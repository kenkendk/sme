using System;
using System.Threading.Tasks;

namespace SME
{
	/// <summary>
	/// Class that performs simulations only
	/// </summary>
	[ClockedProcess]
	public abstract class SimulationProcess : Process
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.SimulationProcess"/> class.
		/// </summary>
		public SimulationProcess()
			: this(Clock.DefaultClock)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.SimulationProcess"/> class.
		/// </summary>
		/// <param name="clock">The clock to use.</param>
		public SimulationProcess(Clock clock)
			: base(clock)
		{
		}
	}
}
