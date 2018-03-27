using System;
using SME;

namespace Stopwatch
{
    //[ClockedProcess]
    public class Stopwatch : StateProcess
    {
        [InputBus]
        Buttons buttons = Scope.CreateOrLoadBus<Buttons>();

        [OutputBus]
        WatchOutput output = Scope.CreateOrLoadBus<WatchOutput>();

        protected async override System.Threading.Tasks.Task OnTickAsync()
        {
            // Zero
            while (!buttons.startstop)
            {
                output.running = false;
                output.reset = false;
                await ClockAsync();
            }

            while (!(!buttons.startstop && buttons.reset))
            {
                // Start
                while (!(!buttons.reset && !buttons.startstop))
                {
                    output.running = true;
                    output.reset = false;
                    await ClockAsync();
                }

                // Running
                while (!buttons.startstop)
                {
                    output.running = true;
                    output.reset = false;
                    await ClockAsync();
                }

                // Stop
                while (!(!buttons.reset && !buttons.startstop))
                {
                    output.running = false;
                    output.reset = false;
                    await ClockAsync();
                }

                // Stopped 
                while (!buttons.reset && !buttons.startstop)
                {
                    output.running = false;
                    output.reset = false;
                    await ClockAsync();
                }
            }

            // Reset
            while (!(!buttons.reset && !buttons.startstop))
            {
                output.running = false;
                output.reset = true;
                await ClockAsync();
            }
        }
    }
}
