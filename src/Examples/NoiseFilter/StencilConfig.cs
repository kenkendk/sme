using System;
namespace NoiseFilter
{
	public static class StencilConfig
	{
		/// <summary>
		/// The number of byte components in each color sample (RGB)
		/// </summary>
		public const int COLOR_WIDTH = 3;
		/// <summary>
		/// The width of the stencil
		/// </summary>
		public const int STENCIL_WIDTH = 3;
		/// <summary>
		/// The height of the stencil
		/// </summary>
		public const int STENCIL_HEIGHT = 3;
		/// <summary>
		/// The length of the stencil (width * height)
		/// </summary>
		public const int STENCIL_LENGTH = STENCIL_WIDTH * STENCIL_HEIGHT;
		/// <summary>
		/// The size of the border, must be 0 or 1
		/// </summary>
		public const int BORDER_SIZE = 1;

		/// <summary>
		/// The maximum width of an image
		/// </summary>
		public const int MAX_IMAGE_WIDTH = 1024;
		/// <summary>
		/// The maximum height of an image
		/// </summary>
		public const int MAX_IMAGE_HEIGHT = 1024;

	}
}
