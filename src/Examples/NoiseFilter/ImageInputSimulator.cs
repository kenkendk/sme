using SME;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace NoiseFilter
{
	/// <summary>
	/// Helper process that loads images and writes them into the simulation
	/// </summary>
	[Ignore]
	[ClockedProcess]
	public class ImageInputSimulator : SimulationProcess
	{
		[OutputBus]
        private readonly ImageInputConfiguration Configuration = Scope.CreateOrLoadBus<ImageInputConfiguration>();

		[OutputBus]
        private readonly ImageInputLine Data = Scope.CreateOrLoadBus<ImageInputLine>();

		[InputBus]
        private readonly BorderDelayUpdate Delay = Scope.CreateOrLoadBus<BorderDelayUpdate>();

		/// <summary>
		/// The images to process
		/// </summary>
		private readonly string[] IMAGES;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NoiseFilter.ImageInputSimulator"/> class.
		/// </summary>
		public ImageInputSimulator()
			: this("image1.png", "image2.jpg", "image3.png")
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
					while (!Delay.IsReady)
						await ClockAsync();

					using (var img = System.Drawing.Image.FromFile(file))
					using (var bmp = new System.Drawing.Bitmap(img))
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

								//Console.WriteLine("Input -> pixel {0}x{1}, values: {2},{3},{4}", j, i, pixel.R, pixel.G, pixel.B);

								await ClockAsync();
							}

							Console.WriteLine($"Still need to write {(bmp.Width - i) * bmp.Height} pixels");

						}

						Data.IsValid = false;
					}
				}
			}

			await ClockAsync();

			while (!Delay.IsReady)
				await ClockAsync();

			await Task.WhenAll(Enumerable.Range(0, 10).Select(x => ClockAsync()));
			//await ClockAsync();

		}
	}
}
