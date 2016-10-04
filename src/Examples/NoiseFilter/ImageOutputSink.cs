using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
		private ImageInputConfiguration Config;
		
		[InputBus]
		private ImageOutputLine Input;

		[InputBus]
		private PaddedInputLine Padded;

		private class Item : IDisposable
		{
			private readonly Bitmap m_image;
			private int m_index;
			private static int _imageIndex;

			public Item(int width, int height)
			{
				m_image = new Bitmap(width, height);
			}

			public void WritePixel(byte r, byte g, byte b)
			{
				var color = Color.FromArgb(r, g, b);
				m_image.SetPixel(m_index % m_image.Width, m_index / m_image.Width, color);
				m_index++;
			}

			public bool IsComplete { get { return m_index == m_image.Width * m_image.Height; } }

			public void Dispose()
			{
 				var filename = string.Format("output-{0}.png", System.Threading.Interlocked.Increment(ref _imageIndex));
				m_image.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
				m_image.Dispose();
			}

			public int X { get { return m_index % m_image.Width; } }
			public int Y { get { return m_index / m_image.Width; } }
		}

		public override async Task Run()
		{
			var work = new Queue<Item>();
			var workPadded = new Queue<Item>();
			var n = 0;

			while (true)
			{
				await ClockAsync();

				if (Config.IsValid)
				{
					work.Enqueue(new Item(Config.Width, Config.Height));
					//workPadded.Enqueue(new Item(Config.Width + StencilConfig.BORDER_SIZE * 2, Config.Height + StencilConfig.BORDER_SIZE * 2));
				}

					

				if (work.Count > 0 && Input.IsValid)
				{
					n++;
					if (n % 1000 == 0)
						Console.WriteLine("Still need {0} pixels more", (Config.Width * Config.Height) - n);


					var cur = work.Peek();
					//Console.WriteLine("Sink -> pixel {0}x{1}, values: {2},{3},{4}", cur.X, cur.Y, Input.Color[0], Input.Color[1], Input.Color[2]);
					cur.WritePixel(Input.Color[0], Input.Color[1], Input.Color[2]);

					if (cur.IsComplete)
					{
						Console.WriteLine("--------------> Wrote image to disk");
						work.Dequeue().Dispose();
					}
				}

				if (workPadded.Count > 0 && Padded.IsValid)
				{
					var cur = workPadded.Peek();
					cur.WritePixel(Padded.Color[0], Padded.Color[1], Padded.Color[2]);

					if (cur.IsComplete)
					{
						Console.WriteLine("--------------> Wrote padded image to disk");
						workPadded.Dequeue().Dispose();
					}
				}
			}
		}
	}
}
