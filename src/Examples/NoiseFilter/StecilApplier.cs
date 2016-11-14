using System;
using SME;
using static NoiseFilter.StencilConfig;

namespace NoiseFilter
{
	/// <summary>
	/// Process that applies the stencil to fragments and outputs the pixels
	/// </summary>
	public class StecilApplier : SimpleProcess
	{
		[InputBus]
		private ImageFragment Input;

		[OutputBus]
		private ImageOutputLine Output;

		public interface IInternal : IBus
		{
			/// <summary>
			/// Gets or sets the index.
			/// </summary>
			[InitialValue]
			uint Index { get; set; }
		}

		private const byte SUM_R = 9;
		private const byte SUM_G = 9;
		private const byte SUM_B = 9;

		private readonly int[] FILTER_SUMS = { 9, 9, 9 };
		private readonly int[] m_buffer = new int[COLOR_WIDTH];

		// TODO: Give warning for this if it is not a signal
		//[SME.Render.VHDL.VHDLSignal]
		//private uint m_index = 0;

		[InternalBus]
		private IInternal Internal;

		private static readonly byte[] FILTER = new byte[] {
			1,1,1, 1,1,1, 1,1,1,
			1,1,1, 1,1,1, 1,1,1,
			1,1,1, 1,1,1, 1,1,1
		};

		protected override void OnTick()
		{
			//DebugOutput = true;
			Output.IsValid = false;
			for (var i = 0; i < COLOR_WIDTH; i++)
				Output.Color[i] = 0;

			for (var i = 0; i < m_buffer.Length; i++)
				m_buffer[i] = 0;

			if (Input.IsValid)
			{
				// Compute the filter
				for (var i = 0; i < Input.Data.Length; i += COLOR_WIDTH)
					for (var j = 0; j < m_buffer.Length; j++)
						m_buffer[j] += FILTER[i + j] * Input.Data[i + j];

				for (var i = 0; i < m_buffer.Length; i++)
					Output.Color[i] = (byte)(m_buffer[i] / FILTER_SUMS[i]);
					//Output.Color[i] = Input.Data[i + (3 * 4)];

				PrintDebug("Apply {3} -> {0},{1},{2}", Input.Data[0 + (3 * 4)], Input.Data[1 + (3 * 4)], Input.Data[2 + (3 * 4)], Internal.Index);

				Internal.Index++;

				Output.IsValid = true;
			}
		}
	}
}
