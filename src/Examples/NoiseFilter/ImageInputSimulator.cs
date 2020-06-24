﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SME;


namespace NoiseFilter
{
    /// <summary>
    /// Helper process that loads images and writes them into the simulation
    /// </summary>
    public class ImageInputSimulator : SimulationProcess
    {
        [OutputBus]
        public ImageInputConfiguration Configuration = Scope.CreateBus<ImageInputConfiguration>();

        [OutputBus]
        public ImageInputLine Data = Scope.CreateBus<ImageInputLine>();

        [InputBus]
        public BorderDelayUpdate Delay;

        /// <summary>
        /// The images to process
        /// </summary>
        private readonly string[] IMAGES;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NoiseFilter.ImageInputSimulator"/> class.
        /// </summary>
        public ImageInputSimulator()
            : this("input/image1.png", "input/image2.jpg", "input/image3.png")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NoiseFilter.ImageInputSimulator"/> class.
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

        public static string current;
        public static bool running = true;

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            await ClockAsync();

            foreach (var file in IMAGES)
            {
                Debug.Assert(File.Exists(file), $"File not found: {file}");
                current = file;
                while (!Delay.IsReady)
                    await ClockAsync();

                using (var img = Image.FromFile(file))
                    using (var bmp = new Bitmap(img))
                    {
                        Console.WriteLine($"Writing {bmp.Width * bmp.Height} pixels from {file}");

                        Configuration.IsValid = true;
                        Configuration.Width = (ushort)bmp.Width;
                        Configuration.Height = (ushort)bmp.Height;

                        await ClockAsync();

                        Configuration.IsValid = false;
                        Data.IsValid = true;

                        for (var i = 0; i < img.Height; i++)
                        {
                            for (var j = 0; j < img.Width; j++)
                            {
                                var pixel = bmp.GetPixel(j, i);
                                Data.Color[0] = pixel.R;
                                Data.Color[1] = pixel.G;
                                Data.Color[2] = pixel.B;

                                await ClockAsync();
                            }

                            Console.WriteLine($"Still need to write {bmp.Width * (bmp.Height - i)} pixels");

                        }

                        Data.IsValid = false;
                    }
            }

            running = false;
        }
    }
}
