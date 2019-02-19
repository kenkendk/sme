using SME;
using System;
using System.Threading.Tasks;

namespace GettingStarted
{
	/// <summary>
	/// Helper process that loads images and writes them into the simulation.
	/// Since this is a simulation process, it will not be rendered as hardware
	/// and we can use any code and dynamic properties we want
	/// </summary>
	public class ImageInputSimulator : SimulationProcess
	{
		/// <summary>
		/// The camera connection bus
		/// </summary>
		[OutputBus]
		public readonly ImageInputLine Data = Scope.CreateBus<ImageInputLine>();

        /// <summary>
        /// The images to process
        /// </summary>
        private readonly string[] IMAGES;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:GettingStarted.ImageInputSimulator"/> class.
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
			// Wait for the initial reset to propagate
			await ClockAsync();

			// Run through all images
			foreach (var file in IMAGES)
			{
				// Sanity check
				if (!System.IO.File.Exists(file))
				{
					Console.WriteLine($"File not found: {file}");
				}
				else
				{
					// Load the image as a bitmap
					using (var img = System.Drawing.Image.FromFile(file))
					using (var bmp = new System.Drawing.Bitmap(img))
					{
						// Write some console progress
						Console.WriteLine($"Writing {bmp.Width * bmp.Height} pixels from {file}");

						// We are now transmitting data
						Data.IsValid = true;

						// Loop through the image pixels
						for (var i = 0; i < img.Height; i++)
						{
							for (var j = 0; j < img.Width; j++)
							{
								// Grab a pixel and send it to the output bus
								var pixel = bmp.GetPixel(j, i);
								Data.R = pixel.R;
								Data.G = pixel.G;
								Data.B = pixel.B;

								// Update the LastPixel flag as required
								Data.LastPixel = i == img.Height - 1 && j == img.Width - 1;

								//Console.WriteLine("Input -> pixel {0}x{1}, values: {2},{3},{4}", j, i, pixel.R, pixel.G, pixel.B);

								await ClockAsync();
							}

							// Write progress after each line
							Console.WriteLine($"Still need to write {bmp.Width * (bmp.Height - i - 1)} pixels");
						}

						// We are now done with the image, so signal that
						Data.IsValid = false;
						Data.LastPixel = false;
					}
				}
			}

			// Make sure the last pixel has propagated
			await ClockAsync();
		}
	}
}
