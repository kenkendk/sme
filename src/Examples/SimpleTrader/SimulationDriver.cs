using System;
using System.Linq;
using System.Threading.Tasks;
using SME;

namespace SimpleTrader
{
	class SimulationDriver : SimulationProcess
	{

		public SimulationDriver(int seed)
		{
			this.seed = seed;
		}

		[OutputBus]
        public ITraderInput Output = Scope.CreateBus<ITraderInput>();

		public static bool running = true;
		int seed;

		public override async Task Run()
		{
			var rn = seed == 0 ? new Random() : new Random(seed);
			Output.Valid = false;

			foreach (var v in GenerateRandomValueSequence.GetUIntSequence(seed).Take(500))
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
			
			running = false;
		}
	}
}
