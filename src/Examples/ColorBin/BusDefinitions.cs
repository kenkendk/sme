using System;
using SME;

namespace ColorBin
{
	/// <summary>
	/// A bus for reading image values from a sensor,
	/// one pixel at a time
	/// </summary>
	[TopLevelInputBus]
	public interface ImageInputLine : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }
		[InitialValue]
		bool LastPixel { get; set; }

		byte R { get; set; }
		byte G { get; set; }
		byte B { get; set; }
	}

	/// <summary>
	/// Bus for reporting counts in the bins
	/// </summary>
	[TopLevelOutputBus]
	public interface BinCountOutput : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		[InitialValue]
		uint Low { get; set; }
		[InitialValue]
		uint Medium { get; set; }
		[InitialValue]
		uint High { get; set; }
	}
}
