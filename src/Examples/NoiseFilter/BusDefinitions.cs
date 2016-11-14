using System;
using SME;
using SME.Render.VHDL;
using static NoiseFilter.StencilConfig;

namespace NoiseFilter
{
	/// <summary>
	/// A bus for sending image dimensions
	/// </summary>
	[TopLevelInputBus]
	public interface ImageInputConfiguration : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		ushort Width { get; set; }
		ushort Height { get; set; }
	}

	/// <summary>
	/// A bus for reading image values from a sensor,
	/// one pixel at a time
	/// </summary>
	[TopLevelInputBus]
	public interface ImageInputLine : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		[FixedArrayLength(COLOR_WIDTH)]
		IFixedArray<byte> Color { get; set; }
	}

	/// <summary>
	/// A bus for sending image dimensions
	/// </summary>
	[TopLevelOutputBus]
	public interface ImageOutputConfiguration : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		ushort Width { get; set; }
		ushort Height { get; set; }
	}


	/// <summary>
	/// A bus for sending filtered image values from a sensor,
	/// one pixel at a time
	/// </summary>
	[TopLevelOutputBus]
	public interface ImageOutputLine : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		[FixedArrayLength(COLOR_WIDTH)]
		IFixedArray<byte> Color { get; set; }
	}

	/// <summary>
	/// A bus for reading image values from a sensor,
	/// one pixel at a time
	/// </summary>
	public interface PaddedInputLine : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		[FixedArrayLength(COLOR_WIDTH)]
		IFixedArray<byte> Color { get; set; }
	}


	/// <summary>
	/// A bus for sending a stencil-size fragment of color values
	/// </summary>
	public interface ImageFragment : IBus
	{
		[InitialValue]
		bool IsValid { get; set; }

		[InitialValue]
		uint Index { get; set; }

		[FixedArrayLength(COLOR_WIDTH * STENCIL_WIDTH * STENCIL_HEIGHT)]
		IFixedArray<byte> Data { get; set; }
	}

	/// <summary>
	/// A bus for signalling the input to delay
	/// </summary>
	public interface BorderDelayUpdate : IBus
	{
		[InitialValue]
		bool IsReady { get; set; }
	}
}
