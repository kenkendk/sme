using System;
using System.Threading.Tasks;

namespace SME
{
    /// <summary>
    /// Class for defining processes that run a state machine.
    /// </summary>
    public abstract class StateProcess : Process
    {
        /// <summary>
        /// Called on each clock tick.
        /// </summary>
        protected abstract Task OnTickAsync();

        /// <summary>
        /// Run this instance, calling OnTickAsync each clocktick.
        /// </summary>
        public override async Task Run()
        {
            while (true)
            {
                await ClockAsync();
                await OnTickAsync();
            }
        }
    }
}
