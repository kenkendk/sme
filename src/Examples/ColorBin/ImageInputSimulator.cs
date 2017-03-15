using SME;
using System;
using System.Threading.Tasks;

namespace ColorBin
{
	/// <summary>
	/// Helper process that loads images and writes them into the simulation
	/// </summary>
	public class ImageInputSimulator : SimulationProcess
	{
		[OutputBus]
		private ImageInputLine Data;

		public static string[] IMAGES = new string[] { "image1.png", "image2.jpg", "image3.png" };

		/// <summary>
		/// Run this instance.
		/// </summary>
		public override async Task Run()
		{
			await ClockAsync();

			foreach (var file in IMAGES)
			{
				if (!System.IO.File.Exists(file))
				{
					Console.WriteLine($"File not found: {file}");
				}
				else
				{
					using (var img = System.Drawing.Image.FromFile(file))
					using (var bmp = new System.Drawing.Bitmap(img))
					{
						Console.WriteLine($"Writing {bmp.Width * bmp.Height} pixels from {file}");

						Data.IsValid = true;

						for (var i = 0; i < img.Height; i++)
						{
							for (var j = 0; j < img.Width; j++)
							{
								var pixel = bmp.GetPixel(j, i);
								Data.R = pixel.R;
								Data.G = pixel.G;
								Data.B = pixel.B;
								Data.LastPixel = i == img.Height - 1 && j == img.Width - 1;

								//Console.WriteLine("Input -> pixel {0}x{1}, values: {2},{3},{4}", j, i, pixel.R, pixel.G, pixel.B);

								await ClockAsync();
							}

							Console.WriteLine($"Still need to write {(bmp.Width - i) * bmp.Height} pixels");

						}

						Data.IsValid = false;
						Data.LastPixel = false;
					}
				}
			}

			await ClockAsync();
		}
	}
}
