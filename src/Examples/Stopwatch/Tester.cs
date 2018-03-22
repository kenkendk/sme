using System;
using System.Diagnostics;
using SME;

namespace Stopwatch
{
    public class Tester : SimulationProcess
    {
        [InputBus]
        WatchOutput watch = Scope.CreateOrLoadBus<WatchOutput>();

        [OutputBus]
        Buttons buttons = Scope.CreateOrLoadBus<Buttons>();

        public async override System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            // Zero
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            await ClockAsync();
            await ClockAsync();

            buttons.startstop = true;
            await ClockAsync();
            await ClockAsync();

            // Start
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();
            await ClockAsync();

            // Running
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();
            await ClockAsync();

            // Stop
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();
            await ClockAsync();

            // Stopped
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();
            await ClockAsync();

            // Start
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();
            await ClockAsync();

            // Running
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();
            await ClockAsync();

            // Stop
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = false;
            await ClockAsync();
            await ClockAsync();

            // Stopped
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.reset = true;
            await ClockAsync();
            await ClockAsync();

            // Reset
            Debug.Assert(!watch.running);
            Debug.Assert(watch.reset);
            buttons.reset = false;
            await ClockAsync();
            await ClockAsync();

            // Zero
            Debug.Assert(!watch.running);
            Debug.Assert(!watch.reset);
            buttons.startstop = true;
            await ClockAsync();
            await ClockAsync();

            // Start
            Debug.Assert(watch.running);
            Debug.Assert(!watch.reset);
        }
    }
}
