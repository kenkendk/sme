using SME;

namespace GettingStarted
{
	/// <summary>
	/// A bus for reading image values from a sensor,
	/// one pixel at a time
	/// </summary>
	public interface ImageInputLine : IBus
	{
        // The [InitialValue] attribute makes sure we can read the value without writing.
		// It is also possible to set [InitializedBus] to force all fields to be initialized
		
		/// <summary>
		/// A value sent from the camera indicating if the RGB value is valid
		/// </summary>
		[InitialValue]
		bool IsValid { get; set; }

		/// <summary>
		/// A value sent from the camera indicating if the current pixel is the last in the image
		/// </summary>
		[InitialValue]
		bool LastPixel { get; set; }

		/// <summary>
		/// The red component of the pixel
		/// </summary>
		byte R { get; set; }
        /// <summary>
        /// The gree component of the pixel
        /// </summary>
        byte G { get; set; }
        /// <summary>
        /// The blue component of the pixel
        /// </summary>
        byte B { get; set; }
	}

	/// <summary>
	/// Bus for reporting counts in the bins
	/// </summary>
	public interface BinCountOutput : IBus
	{
		/// <summary>
		/// A value send by bin counter indicating if the output represents a whole image
		/// </summary>
		[InitialValue]
		bool IsValid { get; set; }

		/// <summary>
		/// The number of low intensity pixels
		/// </summary>
		[InitialValue]
		uint Low { get; set; }

		/// <summary>
		/// The number of medium intensity pixels
		/// </summary>
		[InitialValue]
		uint Medium { get; set; }

		/// <summary>
		/// The number of high intensity pixels
		/// </summary>
		[InitialValue]
		uint High { get; set; }
	}
}
