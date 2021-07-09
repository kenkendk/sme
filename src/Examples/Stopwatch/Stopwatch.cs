using System;
using SME;

namespace Stopwatch
{
    //[ClockedProcess]
    public class Stopwatch : StateProcess
    {
        [InputBus]
        public Buttons buttons;

        [OutputBus]
        public WatchOutput output = Scope.CreateBus<WatchOutput>();

        protected async override System.Threading.Tasks.Task OnTickAsync()
        {
            output.running = false;
            output.reset = false;

            // Wait for a trigger event
            while (!buttons.startstop && !buttons.reset)
                await ClockAsync();

            // Check if we are in the reset or start
            if (buttons.reset)
            {
                // Keep resetting until the button is released
                output.reset = true;
                while (buttons.reset)
                    await ClockAsync();

                output.reset = false;
            }
            else
            {
                // Now running, wait for button to be pressed
                output.running = true;
                while (buttons.startstop)
                    await ClockAsync();
                // Keep running until the button is pressed
                while (!buttons.startstop)
                    await ClockAsync();

                // Stop running, and wait for the button to be released
                output.running = false;
                while (buttons.startstop)
                    await ClockAsync();
            }
        }
    }
}
