using System;
using SME;

namespace SimpleTradeImplementation
{
	[ClockedProcess]
	public class TraderCoreFIR : SimpleProcess
	{
		public interface ITraderInput : IBus
		{
			[InitialValue]
			bool Valid { get; set; }
			[InitialValue]
			bool Restart { get; set; }
			uint Value { get; set; }
		}

		[InitializedBus]
		public interface ITraderOutput : IBus
		{
			bool Valid { get; set; }
			bool GoingDown { get; set; }
			bool GoingUp { get; set; }

			uint DebugShort { get; set; }
			uint DebugLong { get; set; }
		}

		protected override void OnTick()
		{
			
		}
	}
}

