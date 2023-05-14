using System;
using System.Linq;
using System.Threading.Tasks;

namespace SME
{
    /// <summary>
    /// Base class for implementing a component.
    /// </summary>
    public abstract class Process : IProcess
    {
        /// <summary>
        /// The clock that drives this process.
        /// </summary>
        protected readonly Clock m_clock = Scope.Current.Clock;

        /// <summary>
        /// The collection of clocked input buses.
        /// </summary>
        private IRuntimeBus[][] m_clockedinputbusses;
        /// <summary>
        /// The collection of input buses.
        /// </summary>
        private IRuntimeBus[][] m_inputbusses;
        /// <summary>
        /// The collection of output buses.
        /// </summary>
        private IRuntimeBus[][] m_outputbusses;
        /// <summary>
        /// The collection of internal buses.
        /// </summary>
        private IRuntimeBus[][] m_internalbusses;

        /// <summary>
        /// Gets the collection of clocked input buses.
        /// </summary>
        /// <value>The collection of input buses.</value>
        IRuntimeBus[][] IProcess.ClockedInputBusses { get { return m_clockedinputbusses; } }
        /// <summary>
        /// Gets the collection of input buses.
        /// </summary>
        /// <value>The collection of input buses.</value>
        IRuntimeBus[][] IProcess.InputBusses { get { return m_inputbusses; } }
        /// <summary>
        /// Gets the collection of output buses.
        /// </summary>
        /// <value>The collection of output buses.</value>
        IRuntimeBus[][] IProcess.OutputBusses { get { return m_outputbusses; } }
        /// <summary>
        /// Gets the collection of output buses.
        /// </summary>
        /// <value>The collection of output buses.</value>
        IRuntimeBus[][] IProcess.InternalBusses { get { return m_internalbusses; } }

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
        /// The finished task.
        /// </summary>
        private TaskCompletionSource<bool> m_finished = new TaskCompletionSource<bool>();

        /// <summary>
        /// Gets the name of this process.
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
        /// Signals the process is ready, allowing all waiters to procceed.
        /// </summary>
        void SignalProcessReady()
        {
            m_procready.SetResult(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SME.Process"/> class
        /// </summary>
        public Process()
        {
            if (Simulation.Current == null)
                throw new InvalidOperationException($"Cannot create a {nameof(Process)} element when there is no active simulation");
            Simulation.Current.RegisterProcess(this);
            Loader.AutoloadBusses(this);
        }

        /// <summary>
        /// Helper method to refresh the internal collections of input and output buses.
        /// </summary>
        public void LoadBusMapsIfRequired()
        {
            if (m_internalbusses == null || m_outputbusses == null || m_inputbusses == null || m_clockedinputbusses == null)
                ReloadBusMaps();
        }

        /// <summary>
        /// Helper method to refresh the internal collections of input and output buses.
        /// </summary>
        protected void ReloadBusMaps()
        {
            m_internalbusses =
                Loader.GetBusFields(this.GetType())
                        .Where(n => n.GetCustomAttributes(typeof(InternalBusAttribute), true).Any())
                        .Select(n => Loader.GetBusInstances(this, n))
                        .Select(n =>
                            n.Where(m => m != null)
                                .Distinct()
                                .Cast<IRuntimeBus>())
                        .Where(n => n != null)
                        .Distinct()
                        .Select(n => n.ToArray())
                        .ToArray();

            m_outputbusses =
                Loader.GetBusFields(this.GetType())
                        .Where(n => {
                            var attrIn = n.GetCustomAttributes(typeof(InputBusAttribute), true).FirstOrDefault();
                            var attrOut = n.GetCustomAttributes(typeof(OutputBusAttribute), true).FirstOrDefault();
                            var attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault();
                            return attrInternal == null && (attrOut != null || ((attrIn == null) == (attrOut == null)));
                        })
                        .Select(n => Loader.GetBusInstances(this, n))
                        .Select(n =>
                            n.Where(m => m != null)
                                .Distinct()
                                .Cast<IRuntimeBus>())
                        .Where(n => n != null)
                        .Distinct()
                        .Select(n => n.ToArray())
                        .ToArray();

            var inputList =
                Loader.GetBusFields(this.GetType())
                        .Where(n => {
                            var attrIn = n.GetCustomAttributes(typeof(InputBusAttribute), true).FirstOrDefault();
                            var attrOut = n.GetCustomAttributes(typeof(OutputBusAttribute), true).FirstOrDefault();
                            var attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault();
                            return attrInternal == null && (attrOut == null || ((attrIn == null) == (attrOut == null)));
                        })
                        .Select(n => Loader.GetBusInstances(this, n))
                        .Select(n =>
                            n.Where(m => m != null)
                            .Distinct()
                            .Cast<IRuntimeBus>())
                        .Where(n => n != null)
                        .Distinct()
                        .Select(n => n.ToArray())
                        .ToArray();

            var violator = m_internalbusses
                .FirstOrDefault(x =>
                    x.FirstOrDefault().BusType
                        .GetCustomAttributes(typeof(TopLevelInputBusAttribute), true)
                        .FirstOrDefault() != null
                )?.FirstOrDefault();
            if (violator != null)
                throw new NotSupportedException($"The bus {violator.BusType.FullName} is marked with [{nameof(InternalBusAttribute)}], but is also marked with [{nameof(TopLevelInputBusAttribute)}] ");

            violator = m_internalbusses
                .FirstOrDefault(x =>
                    x.FirstOrDefault().BusType
                        .GetCustomAttributes(typeof(TopLevelOutputBusAttribute), true)
                        .FirstOrDefault() != null
                )?.FirstOrDefault();
            if (violator != null)
                throw new NotSupportedException($"The bus {violator.BusType.FullName} is marked with [{nameof(InternalBusAttribute)}], but is also marked with [{nameof(TopLevelOutputBusAttribute)}] ");

            if (((IProcess)this).IsClockedProcess)
            {
                m_inputbusses = new IRuntimeBus[0][];
                m_clockedinputbusses = inputList;
            }
            else
            {
                m_clockedinputbusses = new IRuntimeBus[0][];
                m_inputbusses = inputList;
            }
        }

        /// <summary>
        /// Manually register the collections of buses in this instance, overrides the automatic bus detection system
        /// </summary>
        /// <param name="inputBusses">The collection of input buses.</param>
        /// <param name="outputBusses">The collection of output buses.</param>
        /// <param name="internalBusses">The collection of internal buses.</param>
        protected void RegisterBusses(IBus[][] inputBusses, IBus[][] outputBusses, IBus[][] internalBusses)
        {
            m_internalbusses = (internalBusses ?? new IBus[0][])
                .Select(x => x.Cast<IRuntimeBus>().ToArray())
                .ToArray();
            m_outputbusses = (outputBusses ?? new IBus[0][])
                .Select(x => x.Cast<IRuntimeBus>().ToArray())
                .ToArray();
            if (((IProcess)this).IsClockedProcess)
            {
                m_inputbusses = new IRuntimeBus[0][];
                m_clockedinputbusses = (inputBusses ?? new IBus[0][])
                    .Select(n => n.Cast<IRuntimeBus>().ToArray())
                    .ToArray();
            }
            else
            {
                m_clockedinputbusses = new IRuntimeBus[0][];
                m_inputbusses = (inputBusses ?? new IBus[0][])
                    .Select(n => n.Cast<IRuntimeBus>().ToArray())
                    .ToArray();
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
        /// Returns an awaitable task indicating that the process has finished.
        /// </summary>
        Task IProcess.Finished()
        {
            return m_finished.Task;
        }

        /// <summary>
        /// Method to be called after Run().
        /// </summary>
        void IProcess.SignalFinished()
        {
            m_finished.SetResult(true);
        }

        /// <summary>
        /// Yields until a specific condition is met.
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
        /// Run this instance.
        /// </summary>
        public abstract Task Run();

        /// <summary>
        /// Helper method for performing an operation that only occurs during simulation.
        /// </summary>
        /// <returns>The input value.</returns>
        protected void SimulationOnly(Action f)
        {
            f();
        }

        /// <summary>
        /// Field for storing an instance of SME.VHDL.ICustomRenderer
        /// TODO it is an object, since SME should not depend on SME.VHDL,
        /// but the type should be ICustomRenderer
        /// </summary>
        public virtual object CustomRenderer { get { return null; } }
    }
}
