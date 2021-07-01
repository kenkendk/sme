﻿using System;
using System.Threading.Tasks;

namespace SME
{
    /// <summary>
    /// Defines the system clock driver.
    /// </summary>
    public sealed class Clock
    {
        /// <summary>
        /// The clock waiting source.
        /// </summary>
        private TaskCompletionSource<bool> m_release = new TaskCompletionSource<bool>();

        /// <summary>
        /// Guard to prevent overlapping ticks of the clock.
        /// </summary>
        private TaskCompletionSource<bool> m_previousRelease = null;

        /// <summary>
        /// Gets the number of ticks issued by this clock.
        /// </summary>
        /// <value>The ticks.</value>
        public long Ticks { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SME.Clock"/> class.
        /// </summary>
        public Clock()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SME.Clock"/> class with a divider.
        /// </summary>
        /// <param name="parent">The clock this clock is based on.</param>
        /// <param name="divider">The number of ticks the parent must run before this clock runs.</param>
        public Clock(Clock parent, int divider)
        {
            RunClockWithDivider(parent, divider);
        }

        /// <summary>
        /// Runs the clock at the specified division.
        /// </summary>
        private async void RunClockWithDivider(Clock parent, int divider)
        {
            while (true)
            {
                for (var i = 0; i < divider; i++)
                    await parent.WaitAsync();

                this.Tick();
            }
        }

        /// <summary>
        /// Waits for the clock to tick.
        /// </summary>
        /// <returns>The async awaitable Task.</returns>
        public Task WaitAsync()
        {
            return m_release.Task;
        }

        /// <summary>
        /// Advances the clock one tick.
        /// </summary>
        private void Release()
        {
            if (m_previousRelease != null)
                m_previousRelease.Task.Wait();

            Ticks++;

            // Set a new waiter blocking entry to the method
            System.Threading.Interlocked.Exchange(ref m_previousRelease, new TaskCompletionSource<bool>());

            //Setup a new list of waiters
            var waiters = System.Threading.Interlocked.Exchange(ref m_release, new TaskCompletionSource<bool>());

            // Register unlock of this method after completion of all tasks
            waiters.Task.ContinueWith(x => {
                if (m_previousRelease != null)
                    m_previousRelease.SetResult(true);
            });

            // Signal clock has ticked
            waiters.SetResult(true);
        }

        /// <summary>
        /// Advances the clock with one tick, and notifies all waiters.
        /// </summary>
        public void Tick()
        {
            Release();
        }

        /// <summary>
        /// Clears all waiters and resets the instance, only supported for internal reset.
        /// </summary>
        internal void Clear()
        {
            // Set a new waiter blocking entry to the method
            System.Threading.Interlocked.Exchange(ref m_previousRelease, null);

            //Setup a new list of waiters
            var waiters = System.Threading.Interlocked.Exchange(ref m_release, new TaskCompletionSource<bool>());

            waiters.SetCanceled();
            Ticks = 0;
        }

    }
}
