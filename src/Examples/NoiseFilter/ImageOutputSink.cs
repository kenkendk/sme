using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using SME;

namespace NoiseFilter
{
    /// <summary>
    /// Helper process that dumps image pixels into an image
    /// </summary>
    public class ImageOutputSink : SimulationProcess
    {
        [InputBus]
        public ImageInputConfiguration Config;

        [InputBus]
        public ImageOutputLine Input;

        [InputBus]
        public PaddedInputLine Padded;

        private class Item : IDisposable
        {
            private readonly Bitmap m_image;
            private Bitmap m_image_expected;
            private int m_index;
            private static int _imageIndex;

            public Item(int width, int height, string filename)
            {
                m_image = new Bitmap(width, height);
                m_image_expected = new Bitmap(Image.FromFile(filename));
            }

            public void WritePixel(byte r, byte g, byte b)
            {
                var color = Color.FromArgb(r, g, b);
                Debug.Assert(m_image_expected.GetPixel(X, Y).Equals(color), $"Error when comparing pixels, expected {m_image_expected.GetPixel(X,Y)}, got {color}");
                m_image.SetPixel(X, Y, color);
                m_index++;
            }

            public bool IsComplete { get { return m_index == m_image.Width * m_image.Height; } }

            public void Dispose()
            {
                System.IO.Directory.CreateDirectory("output");
                var filename = string.Format("output/output-{0}.png", System.Threading.Interlocked.Increment(ref _imageIndex));
                m_image.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
                m_image.Dispose();
            }

            public int X { get { return m_index % m_image.Width; } }
            public int Y { get { return m_index / m_image.Width; } }
            public int idx { get { return _imageIndex; } }
        }

        public override async Task Run()
        {
            var work = new Queue<Item>();
            var workPadded = new Queue<Item>();

            while (true)
            {
                await ClockAsync();

                if (Config.IsValid)
                {
                    string filename = ImageInputSimulator.current;
                    work.Enqueue(new Item(Config.Width, Config.Height, $"{filename}.expected.png"));
                    workPadded.Enqueue(new Item(Config.Width + StencilConfig.BORDER_SIZE * 2, Config.Height + StencilConfig.BORDER_SIZE * 2, $"{filename}.expectedpad.png"));
                }

                if (work.Count > 0 && Input.IsValid)
                {
                    var cur = work.Peek();
                    cur.WritePixel(Input.Color[0], Input.Color[1], Input.Color[2]);

                    if (cur.IsComplete)
                    {
                        Console.WriteLine($"--------------> Wrote image ({cur.idx}) to disk in output/ folder");
                        work.Dequeue().Dispose();
                    }
                }

                if (workPadded.Count > 0 && Padded.IsValid)
                {
                    var cur = workPadded.Peek();
                    cur.WritePixel(Padded.Color[0], Padded.Color[1], Padded.Color[2]);

                    if (cur.IsComplete)
                    {
                        Console.WriteLine($"--------------> Wrote padded image ({cur.idx}) to disk in output/ folder");
                        workPadded.Dequeue().Dispose();
                    }
                }

                if (work.Count == 0 && workPadded.Count == 0 && !ImageInputSimulator.running)
                    Simulation.Current.RequestStop();
            }
        }
    }
}
