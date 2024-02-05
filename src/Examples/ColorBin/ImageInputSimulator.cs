using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SME;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ColorBin
{
    /// <summary>
    /// Helper process that loads images, writes them into the simulation and verifies
    /// that it produces the expected output, if the file exists.
    /// </summary>
    public class ImageInputSimulator : SimulationProcess
    {
        [InputBus]
        public BinCountOutput Result;

        [OutputBus]
        public ImageInputLine Data = Scope.CreateBus<ImageInputLine>();

        /// <summary>
        /// The images to process
        /// </summary>
        private readonly string[] IMAGES;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ColorBin.ImageInputSimulator"/> class.
        /// </summary>
        public ImageInputSimulator()
            : this("input/image1.png", "input/image2.jpg", "input/image3.png")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ColorBin.ImageInputSimulator"/> class.
        /// </summary>
        /// <param name="images">The images to process.</param>
        public ImageInputSimulator(params string[] images)
        {
            if (images == null)
                throw new ArgumentNullException(nameof(images));
            if (images.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(images), "No images to send?");
            IMAGES = images;
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            await ClockAsync();

            foreach (var file in IMAGES)
            {
                Debug.Assert(System.IO.File.Exists(file), $"File not found: {file}");

                using (var img = Image.Load<Rgb24>(file))
                {
                    Console.WriteLine($"Writing {img.Width * img.Height} pixels from {file}");

                    Data.IsValid = true;

                    for (var i = 0; i < img.Height; i++)
                    {
                        for (var j = 0; j < img.Width; j++)
                        {
                            var pixel = img[j, i];
                            Data.R = pixel.R;
                            Data.G = pixel.G;
                            Data.B = pixel.B;
                            Data.LastPixel = i == img.Height - 1 && j == img.Width - 1;

                            await ClockAsync();
                        }
                    }

                    Data.IsValid = false;
                    Data.LastPixel = false;
                }

                // If the collector is clocked, the simulator should wait for
                // it to be ready
                while (!Result.IsValid)
                    await ClockAsync();

                var expected_file = $"{file}.txt";
                Debug.Assert(File.Exists(expected_file), $"Error, expected results file '{expected_file}' does not exist.");
                int[] expected = File.ReadLines(expected_file).Select(x => int.Parse(x)).ToArray();
                Debug.Assert(expected[0] == Result.Low, $"Low: Got {Result.Low}, expected {expected[0]}");
                Debug.Assert(expected[1] == Result.Medium, $"Medium: Got {Result.Medium}, expected {expected[1]}");
                Debug.Assert(expected[2] == Result.High, $"High: Got {Result.High}, expected {expected[2]}");

                await ClockAsync();
            }
        }
    }
}
