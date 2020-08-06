using System;
using System.Threading.Tasks;

namespace SME
{
    /// <summary>
    /// Class for defining simple processes that repeat on each clock.
    /// </summary>
    public abstract class SimpleProcess : Process
    {
        /// <summary>
        /// Called on each clock tick.
        /// </summary>
        protected abstract void OnTick();

        /// <summary>
        /// Run this instance, calling OnTick each clocktick.
        /// </summary>
        public override async Task Run()
        {
            while (true)
            {
                await ClockAsync();
                OnTick();
            }
        }
    }
}
