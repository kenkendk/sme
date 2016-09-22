using System;
using SME;

namespace SimpleTradeImplementation
{
	[ClockedProcess]
	public class TraderCoreEWMA : SimpleProcess
	{
		[InitializedBus]
		[TopLevelOutputBus]
		public interface ITraderOutput : IBus
		{
			bool Valid { get; set; }
			bool GoingDown { get; set; }
			bool GoingUp { get; set; }

			uint DebugShort { get; set; }
			uint DebugLong { get; set; }
		}

		[InitializedBus]
		public interface IInternal : IBus
		{
			uint ShortValue { get; set; }
			uint LongValue { get; set; }
			uint StartCounter { get; set; }
		}

		[InputBus]
		private ITraderInput Input;

		[OutputBus]
		private ITraderOutput Output;

		[InternalBus]
		private IInternal Internal;

		/// <summary>
		/// The number of values to read before setting outputs
		/// </summary>
		private const uint STARTUP_VALUE_COUNT = 10;

		/// <summary>
		/// The alpha-value for EWMA short window, as an inverted fraction 1/X.
		/// </summary>
		private const uint DECAY_SHORT = 1u << DECAY_SHORT_BITSHIFT;
		/// <summary>
		/// The bit-shift used to obtain DECAY_SHORT, meaning Power(2, x)
		/// </summary>
		private const int DECAY_SHORT_BITSHIFT = 2;

		/// <summary>
		/// The alpha-value for EWMA long window, as an inverted fraction 1/X
		/// </summary>
		private const uint DECAY_LONG = 1u << DECAY_LONG_BITSHIFT;
		/// <summary>
		/// The bit-shift used to obtain DECAY_LONG, meaning Power(2, x)
		/// </summary>
		private const int DECAY_LONG_BITSHIFT = 3;

		protected override void OnTick()
		{
			Output.Valid = false;
			Output.GoingDown = false;
			Output.GoingUp = false;

			if (Input.Restart)
			{
				Internal.StartCounter = 0;
				Internal.ShortValue = 0;
				Internal.LongValue = 0;
			}
			else if (Input.Valid)
			{
				if (Internal.StartCounter == 0)
				{
					Internal.LongValue = Input.Value;
					Internal.ShortValue = Input.Value;
					Internal.StartCounter++;
				}
				else
				{
					// Compute the EWMA
					var newShortValue =
						(Input.Value >> DECAY_SHORT_BITSHIFT)
						+
						(Internal.ShortValue >> DECAY_SHORT_BITSHIFT) * (DECAY_SHORT - 1);

					var newLongValue =
						(Input.Value >> DECAY_LONG_BITSHIFT)
						+
						(Internal.LongValue >> DECAY_LONG_BITSHIFT) * (DECAY_LONG - 1);

					if (Internal.StartCounter < STARTUP_VALUE_COUNT)
					{
						Internal.StartCounter++;
					}
					else
					{
						// Startup delay over, update outputs

						// If the short projection is going down, and the long projection is going up
						Output.GoingDown = newLongValue > newShortValue && Internal.LongValue <= Internal.ShortValue;
						Output.GoingUp = newLongValue < newShortValue && Internal.LongValue >= Internal.ShortValue;
						Output.Valid = true;
					}

					Internal.ShortValue = newShortValue;
					Internal.LongValue = newLongValue;

					Output.DebugShort = newShortValue;
					Output.DebugLong = newLongValue;
				}

			}
		}
	}
}

