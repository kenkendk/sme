using System;
using SME;

namespace SimpleTradeImplementation
{
	[ClockedProcess]
	public class TraderCoreFIR : SimpleProcess
	{
		[InitializedBus]
		[TopLevelOutputBus]
		public interface ITraderOutput : IBus
		{
			bool Valid { get; set; }
			bool GoingDown { get; set; }
			bool GoingUp { get; set; }

			ulong DebugShort { get; set; }
			ulong DebugLong { get; set; }
		}

		[InitializedBus]
		public interface IInternal : IBus
		{
			ulong ShortValue { get; set; }
			ulong LongValue { get; set; }

			uint StartCounter { get; set; }
		}

		[InputBus]
		private ITraderInput Input;

		[OutputBus]
		private ITraderOutput Output;

		[InternalBus]
		private IInternal Internal;


		// Weights for the short tap
		private readonly byte[] WeightsShort = new byte[] { 1, 1, 1, 1 };
		// Weights for the long tap
		private readonly byte[] WeightsLong = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };

		// Storage for short sample list
		private uint[] SamplesShort = new uint[4];
		// Storage for long sample list
		private uint[] SamplesLong = new uint[8];

		private readonly uint TESTVAL = 4;

		/// <summary>
		/// The number of values to read before setting outputs
		/// </summary>
		private const uint STARTUP_VALUE_COUNT = 10;

		protected override void OnTick()
		{
			Output.Valid = false;
			Output.GoingDown = false;
			Output.GoingUp = false;

			if (Input.Restart)
			{
				Internal.StartCounter = 0;
			}
			else if (Input.Valid)
			{
				// Shift values right to make room for new sample
				for (var i = 0; i < WeightsShort.Length - 1; i++)
					SamplesShort[i + 1] = SamplesShort[i];
				
				for (var i = 0; i < WeightsLong.Length - 1; i++)
					SamplesLong[i + 1] = SamplesLong[i];

				SamplesLong[0] = SamplesShort[0] = Input.Value;

				// Compute new gradients
				var newShortValue = 0uL;
				var newLongValue = 0uL;

				for (var i = 0; i < SamplesShort.Length; i++)
					newShortValue += SamplesShort[i] * WeightsShort[i];

				for (var i = 0; i < SamplesLong.Length; i++)
					newLongValue += SamplesLong[i] * WeightsLong[i];

				newShortValue /= 4;
				newLongValue /= 8;

				// Startup delay over, update outputs
				if (Internal.StartCounter >= STARTUP_VALUE_COUNT)
				{
					// If the short projection is going down, and the long projection is going up
					Output.GoingDown = newLongValue > newShortValue && Internal.LongValue <= Internal.ShortValue;
					Output.GoingUp = newLongValue < newShortValue && Internal.LongValue >= Internal.ShortValue;
					Output.Valid = true;
				}

				Internal.StartCounter++;

				Internal.ShortValue = newShortValue;
				Internal.LongValue = newLongValue;

				Output.DebugShort = newShortValue;
				Output.DebugLong = newLongValue;

			}

		}
	}
}

