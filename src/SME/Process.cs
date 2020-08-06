using System;
using System.Linq;
using System.Threading.Tasks;

namespace SME
{
    /// <summary>
    /// Base class for implementing a component
    /// </summary>
    public abstract class Process : IProcess
    {
        /// <summary>
        /// The clock that drives this process
        /// </summary>
        protected readonly Clock m_clock = Scope.Current.Clock;

        /// <summary>
        /// The clocked input busses
        /// </summary>
        private IRuntimeBus[] m_clockedinputbusses;
        /// <summary>
        /// The input busses.
        /// </summary>
        private IRuntimeBus[] m_inputbusses;
        /// <summary>
        /// The output busses.
        /// </summary>
        private IRuntimeBus[] m_outputbusses;
        /// <summary>
        /// The internal busses
        /// </summary>
        private IRuntimeBus[] m_internalbusses;

        /// <summary>
        /// Gets the clocked input busses.
        /// </summary>
        /// <value>The input busses.</value>
        IRuntimeBus[] IProcess.ClockedInputBusses { get { return m_clockedinputbusses; } }
        /// <summary>
        /// Gets the input busses.
        /// </summary>
        /// <value>The input busses.</value>
        IRuntimeBus[] IProcess.InputBusses { get { return m_inputbusses; } }
        /// <summary>
        /// Gets the output busses.
        /// </summary>
        /// <value>The output busses.</value>
        IRuntimeBus[] IProcess.OutputBusses { get { return m_outputbusses; } }
        /// <summary>
        /// Gets the output busses.
        /// </summary>
        /// <value>The output busses.</value>
        IRuntimeBus[] IProcess.InternalBusses { get { return m_internalbusses; } }

        /// <summary>
        /// Gets a value indicating whether this instance is a clocked process.
        /// </summary>
        /// <value><c>true</c> if this instance is a clocked process; otherwise, <c>false</c>.</value>
        bool IProcess.IsClockedProcess { get { return this.GetType().GetCustomAttributes(typeof(ClockedProcessAttribute), true).FirstOrDefault() != null; } }

        /// <summary>
        /// The inputready task.
        /// </summary>
        private TaskCompletionSource<bool> m_inputready = new TaskCompletionSource<bool>();

        /// <summary>
        /// The processready task.
        /// </summary>
        private TaskCompletionSource<bool> m_procready = new TaskCompletionSource<bool>();

        /// <summary>
        /// The finished task
        /// </summary>
        private TaskCompletionSource<bool> m_finished = new TaskCompletionSource<bool>();

        /// <summary>
        /// Gets the name of this <see cref="T:SME.Process"/>.
        /// </summary>
        string IProcess.Name { get { return null; } }

        /// <summary>
        /// Resets the inputready task.
        /// </summary>
        Task IProcess.ResetInputReady()
        {
            var task = System.Threading.Interlocked.Exchange(ref m_inputready, new TaskCompletionSource<bool>());
            var res = task.Task.ContinueWith(x => { });
            return res;
        }

        /// <summary>
        /// Signals the input is ready, allowing all waiters to procceed.
        /// </summary>
        Task IProcess.SignalInputReady()
        {
            m_inputready.SetResult(true);
            return m_inputready.Task.ContinueWith(x => { });
        }

        /// <summary>
        /// Resets the processready task
        /// </summary>
        Task IProcess.ResetProcessReady()
        {
            var task = new TaskCompletionSource<bool>();
            System.Threading.Interlocked.Exchange(ref m_procready, task);
            var res = task.Task.ContinueWith(x => { });
            return res;
        }

        /// <summary>
        /// Gets the processready task
        /// </summary>
        Task IProcess.ProcessReady()
        {
            return m_procready.Task.ContinueWith(x => { });
        }

        /// <summary>
        /// Signals the process is ready, allowing all waiters to procceed.
        /// </summary>
        void SignalProcessReady()
        {
            m_procready.SetResult(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SME.Component"/> class
        /// </summary>
        public Process()
        {
            if (Simulation.Current == null)
                throw new InvalidOperationException($"Cannot create a {nameof(Process)} element when there is no active simulation");
            Simulation.Current.RegisterProcess(this);
            Loader.AutoloadBusses(this);
        }

        /// <summary>
        /// Helper method to refresh the internal list of input and output busses
        /// </summary>
        public void LoadBusMapsIfRequired()
        {
            if (m_internalbusses == null || m_outputbusses == null || m_inputbusses == null || m_clockedinputbusses == null)
                ReloadBusMaps();
        }

        /// <summary>
        /// Helper method to refresh the internal list of input and output busses
        /// </summary>
        protected void ReloadBusMaps()
        {
            m_internalbusses =
                Loader.GetBusFields(this.GetType())
                      .Where(n => n.GetCustomAttributes(typeof(InternalBusAttribute), true).Any())
                      .SelectMany(n => Loader.GetBusInstances(this, n))
                      .Where(n => n != null)
                      .Distinct()
                      .Cast<IRuntimeBus>()
                      .ToArray();

            m_outputbusses =
                Loader.GetBusFields(this.GetType())
                      .Where(n => {
                          var attrIn = n.GetCustomAttributes(typeof(InputBusAttribute), true).FirstOrDefault();
                          var attrOut = n.GetCustomAttributes(typeof(OutputBusAttribute), true).FirstOrDefault();
                          var attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault();
                          return attrInternal == null && (attrOut != null || ((attrIn == null) == (attrOut == null)));
                      })
                      .SelectMany(n => Loader.GetBusInstances(this, n))
                      .Where(n => n != null)
                      .Distinct()
                      .Cast<IRuntimeBus>()
                      .ToArray();

            var inputList =
                Loader.GetBusFields(this.GetType())
                      .Where(n => {
                          var attrIn = n.GetCustomAttributes(typeof(InputBusAttribute), true).FirstOrDefault();
                          var attrOut = n.GetCustomAttributes(typeof(OutputBusAttribute), true).FirstOrDefault();
                          var attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault();
                          return attrInternal == null && (attrOut == null || ((attrIn == null) == (attrOut == null)));
                      })
                      .SelectMany(n => Loader.GetBusInstances(this, n))
                      .Where(n => n != null)
                      .Distinct()
                      .Cast<IRuntimeBus>()
                      .ToArray();

            var violator = m_internalbusses.FirstOrDefault(x => x.BusType.GetCustomAttributes(typeof(TopLevelInputBusAttribute), true).FirstOrDefault() != null);
            if (violator != null)
                throw new NotSupportedException($"The bus {violator.BusType.FullName} is marked with [{nameof(InternalBusAttribute)}], but is also marked with [{nameof(TopLevelInputBusAttribute)}] ");

            violator = m_internalbusses.FirstOrDefault(x => x.BusType.GetCustomAttributes(typeof(TopLevelOutputBusAttribute), true).FirstOrDefault() != null);
            if (violator != null)
                throw new NotSupportedException($"The bus {violator.BusType.FullName} is marked with [{nameof(InternalBusAttribute)}], but is also marked with [{nameof(TopLevelOutputBusAttribute)}] ");

            if (((IProcess)this).IsClockedProcess)
            {
                m_inputbusses = new IRuntimeBus[0];
                m_clockedinputbusses = inputList;
            }
            else
            {
                m_clockedinputbusses = new IRuntimeBus[0];
                m_inputbusses = inputList;
            }
        }

        /// <summary>
        /// Manually register the busses in this instance, overrides the automatic bus detection system
        /// </summary>
        /// <param name="inputBusses">The input busses.</param>
        /// <param name="outputBusses">The output busses.</param>
        /// <param name="internalBusses">The internal busses.</param>
        protected void RegisterBusses(IBus[] inputBusses, IBus[] outputBusses, IBus[] internalBusses)
        {
            m_internalbusses = (internalBusses ?? new IBus[0]).Cast<IRuntimeBus>().ToArray();
            m_outputbusses = (outputBusses ?? new IBus[0]).Cast<IRuntimeBus>().ToArray();
            if (((IProcess)this).IsClockedProcess)
            {
                m_inputbusses = new IRuntimeBus[0];
                m_clockedinputbusses = (inputBusses ?? new IBus[0]).Cast<IRuntimeBus>().ToArray();
            }
            else
            {
                m_clockedinputbusses = new IRuntimeBus[0];
                m_inputbusses = (inputBusses ?? new IBus[0]).Cast<IRuntimeBus>().ToArray();
            }
        }

        /// <summary>
        /// Returns an awaitable task that is signaled when the process can run
        /// </summary>
        /// <returns>The async awaitable task.</returns>
        public async Task ClockAsync()
        {
            SignalProcessReady();
            await m_clock.WaitAsync();
            await m_inputready.Task;
        }

        /// <summary>
        /// Returns an awaitable task indicating that the process has finished
        /// </summary>
        Task IProcess.Finished()
        {
            return m_finished.Task;
        }

        /// <summary>
        /// Method to be called after Run()
        /// </summary>
        void IProcess.SignalFinished()
        {
            m_finished.SetResult(true);
        }

        /// <summary>
        /// Yields until a specific condition is met
        /// </summary>
        /// <returns>The async awaitable task.</returns>
        /// <param name="condition">The condition to wait for.</param>
        public async Task WaitUntilAsync(Func<bool> condition)
        {
            do
            {
                await ClockAsync();
            } while(!condition());
        }

        /// <summary>
        /// Repeat the specified piece of code each clock cycle.
        /// </summary>
        /// <param name="code">The code to run.</param>
        public async Task RepeatAsync(Action code)
        {
            while (true)
            {
                await ClockAsync();

                code();
            }
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public abstract Task Run();

        /// <summary>
        /// Helper method for performing an operation that only occurs during simulation
        /// </summary>
        /// <returns>The input value</returns>
        protected void SimulationOnly(Action f)
        {
            f();
        }
    }
}

