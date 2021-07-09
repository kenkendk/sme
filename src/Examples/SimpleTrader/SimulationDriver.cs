using System;
using System.Linq;
using System.Threading.Tasks;
using SME;

namespace SimpleTrader
{
    class SimulationDriver : SimulationProcess
    {

        public SimulationDriver(int seed, int runs = 10, int values_per_run = 50)
        {
            this.seed = seed;
            this.runs = runs;
            this.values_per_run = values_per_run;
        }

        [OutputBus]
        public ITraderInput Output = Scope.CreateBus<ITraderInput>();

        public static bool running = true;
        int seed;
        int runs;
        int values_per_run;

        public override async Task Run()
        {
            var rn = seed == 0 ? new Random() : new Random(seed);
            for (int i = 0; i < runs; i++)
            {
                Output.Restart = true;
                Output.Valid = false;
                await ClockAsync();

                Output.Restart = false;

                foreach (var v in GenerateRandomValueSequence.GetUIntSequence(seed).Take(50))
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

            running = false;
        }
    }
}
