using System;
using System.Diagnostics;
using SME;

namespace Stopwatch
{
    public class Tester : SimulationProcess
    {
        [InputBus]
        public WatchOutput watch;
        [InputBus]
        public NumberOutput number;

        [OutputBus]
        public Buttons buttons = Scope.CreateBus<Buttons>();

        int numbers_to_count = 10;
        int skip_cycles = 100;

        public async override System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            // Zero
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            await ClockAsync();

            buttons.startstop = true;
            await ClockAsync();

            // Start
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();

            // Running
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();

            // Stop
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();

            // Stopped
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();

            // Start
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();

            // Running
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();

            // Stop
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();

            // Stopped
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.reset = true;
            await ClockAsync();

            // Reset
            Debug.Assert(!watch.running);
            Debug.Assert(watch.reset);
            buttons.reset = false;
            await ClockAsync();
            // Wait for process wrap around
            await ClockAsync();

            // Zero
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();

            // Start
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);

            // Reset and run for a number of number changes
            buttons.startstop = false;
            await ClockAsync();
            buttons.startstop = true;
            await ClockAsync();
            buttons.startstop = false;
            await ClockAsync();
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.reset = true;
            await ClockAsync();
            buttons.reset = false;
            await ClockAsync();
            await ClockAsync();
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();
            buttons.startstop = false;
            int last = 0;
            for (int i = 0; i < numbers_to_count; i++)
            {
                for (int j = 0; j < skip_cycles; j++)
                {
                    Debug.Assert(number.val == last, $"expected {last}, got {number.val}");
                    await ClockAsync();
                }
                last++;
            }
        }
    }
}
