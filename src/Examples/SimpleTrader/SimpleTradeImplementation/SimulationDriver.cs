using System;
using System.Linq;
using System.Threading.Tasks;
using SME;
using SME.Render.VHDL;

namespace SimpleTraderTester
{
	[ClockedProcess]
	[VHDLIgnore]
	class SimulationDriver : SimulationProcess
	{
		[OutputBus]
		private SimpleTradeImplementation.ITraderInput Output;

		public override async Task Run()
		{
			var rn = new Random();
			Output.Valid = false;

			foreach (var v in GenerateRandomValueSequence.GetUIntSequence().Take(500))
			{
				await ClockAsync();

				// Simulate bubbles in the input
				if (rn.NextDouble() > 0.85)
				{
					Output.Valid = false;
					await ClockAsync();
				}

				Output.Valid = true;
				Output.Value = v;
			}

			await ClockAsync();
			Output.Valid = false;
			await ClockAsync();
		}
	}
}
