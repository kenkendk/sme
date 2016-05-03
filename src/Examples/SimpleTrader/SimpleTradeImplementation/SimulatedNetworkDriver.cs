using System;
using SME;
using System.Threading.Tasks;

namespace SimpleTradeImplementation
{
	public static class SimulatedValues
	{
		public static uint[] Values;
		public static uint ValueCount;
	}

	public class Stopper : Process
	{
		public interface IStopper : IBus
		{
			[InitialValue]
			bool Stopped { get; set; }
		}

		[InputBus]
		private IStopper Input;

		public override async Task Run()
		{
			while(!Input.Stopped)
				await ClockAsync();
		}
	}

	[ClockedProcess]
	public class SimulatedNetworkDriver : SimpleProcess
	{
		private readonly uint SIMULATED_VALUE_COUNT = SimulatedValues.ValueCount;
		private readonly uint[] VALUES = SimulatedValues.Values;

		[OutputBus]
		private TraderCoreEWMA.ITraderInput Output;

		[OutputBus]
		private Stopper.IStopper Stop;

		private int m_index = -1;

		protected override void OnTick()
		{
			Output.Valid = false;
			Stop.Stopped = m_index > SIMULATED_VALUE_COUNT;

			if (m_index >= 0 && m_index < SIMULATED_VALUE_COUNT)
			{
				Output.Value = VALUES[m_index];
				Output.Valid = true;
			}
			m_index++;
		}
	}
}

