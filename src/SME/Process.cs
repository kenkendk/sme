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
		protected readonly Clock m_clock;

		/// <summary>
		/// The clocked input busses
		/// </summary>
		private IBus[] m_clockedinputbusses;
		/// <summary>
		/// The input busses.
		/// </summary>
		private IBus[] m_inputbusses;
		/// <summary>
		/// The output busses.
		/// </summary>
		private IBus[] m_outputbusses;
		/// <summary>
		/// The internal busses
		/// </summary>
		private IBus[] m_internalbusses;

		/// <summary>
		/// Gets the clocked input busses.
		/// </summary>
		/// <value>The input busses.</value>
		IBus[] IProcess.ClockedInputBusses { get { return m_clockedinputbusses; } }
		/// <summary>
		/// Gets the input busses.
		/// </summary>
		/// <value>The input busses.</value>
		IBus[] IProcess.InputBusses { get { return m_inputbusses; } }
		/// <summary>
		/// Gets the output busses.
		/// </summary>
		/// <value>The output busses.</value>
		IBus[] IProcess.OutputBusses { get { return m_outputbusses; } }
		/// <summary>
		/// Gets the output busses.
		/// </summary>
		/// <value>The output busses.</value>
		IBus[] IProcess.InternalBusses { get { return m_internalbusses; } }

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
		/// Flag indicating if the process produces debug output
		/// </summary>
		protected bool DebugOutput;

		/// <summary>
		/// Signals the input is ready, allowing all waiters to procceed.
		/// </summary>
		Task IProcess.SignalInputReady()
		{
			var task = System.Threading.Interlocked.Exchange(ref m_inputready, new TaskCompletionSource<bool>());
			var res = task.Task.ContinueWith(x => { });
			task.SetResult(true);
			return res;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SME.Component"/> class with the default clock.
		/// </summary>
		public Process()
			: this(Clock.DefaultClock)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SME.Component"/> class.
		/// </summary>
		/// <param name="clock">The clock that drives the process.</param>
		public Process(Clock clock)
		{
			Loader.AutoloadBusses(this, clock);
			m_clock = clock;
			ReloadBusMaps();
		}

		/// <summary>
		/// Helper method to refresh the internal list of input and output busses
		/// </summary>
		protected void ReloadBusMaps()
		{
			m_internalbusses = 
				(from n in Loader.GetBusFields(this.GetType())
					let attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault()
					where attrInternal != null
					select (IBus)n.GetValue(this)).ToArray();

			m_outputbusses = 
				(from n in Loader.GetBusFields(this.GetType())
					let attrIn = n.GetCustomAttributes(typeof(InputBusAttribute), true).FirstOrDefault()
					let attrOut = n.GetCustomAttributes(typeof(OutputBusAttribute), true).FirstOrDefault()
					let attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault()
					where attrInternal == null && (attrOut != null || ((attrIn == null) == (attrOut == null)))
					select (IBus)n.GetValue(this)).ToArray();

			var inputList = 
				(from n in Loader.GetBusFields(this.GetType())
					let attrIn = n.GetCustomAttributes(typeof(InputBusAttribute), true).FirstOrDefault()
					let attrOut = n.GetCustomAttributes(typeof(OutputBusAttribute), true).FirstOrDefault()
					let attrInternal = n.GetCustomAttributes(typeof(InternalBusAttribute), true).FirstOrDefault()
				    where attrInternal == null && (attrOut == null || ((attrIn == null)  == (attrOut == null)))
					select (IBus)n.GetValue(this)).ToArray();


			var violator = m_internalbusses.FirstOrDefault(x => x.BusType.GetCustomAttributes(typeof(TopLevelInputBusAttribute), true).FirstOrDefault() != null);
			if (violator != null)
				throw new NotSupportedException($"The bus {violator.BusType.FullName} is marked with [{nameof(InternalBusAttribute)}], but is also marked with [{nameof(TopLevelInputBusAttribute)}] ");

			violator = m_internalbusses.FirstOrDefault(x => x.BusType.GetCustomAttributes(typeof(TopLevelOutputBusAttribute), true).FirstOrDefault() != null);
			if (violator != null)
				throw new NotSupportedException($"The bus {violator.BusType.FullName} is marked with [{nameof(InternalBusAttribute)}], but is also marked with [{nameof(TopLevelOutputBusAttribute)}] ");

			if (((IProcess)this).IsClockedProcess)
			{
				m_inputbusses = new IBus[0];
				m_clockedinputbusses = inputList;
			}
			else
			{
				m_clockedinputbusses = new IBus[0];
				m_inputbusses = inputList;
			}
		}

		/// <summary>
		/// Returns an awaitable task that is signaled when the process can run
		/// </summary>
		/// <returns>The async awaitable task.</returns>
		public async Task ClockAsync()
		{			
			await m_clock.WaitAsync();
			await m_inputready.Task;
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
		/// Prints debug output if the process has debug enabled
		/// </summary>
		/// <param name="msg">The format string</param>
		/// <param name="arg">The arguments</param>
		protected void PrintDebug(string msg, params object[] arg)
		{
			if (DebugOutput)
				Console.WriteLine(msg, arg);
		}


		/// <summary>
		/// Helper method for performing an operation that only occurs during simulation
		/// </summary>
		/// <returns>The input value</returns>
		/// <param name="value">The value to return</param>
		/// <typeparam name="T">The data type parameter.</typeparam>
		protected T SimulationOnly<T>(T value)
		{
			return value;
		}
	}
}

