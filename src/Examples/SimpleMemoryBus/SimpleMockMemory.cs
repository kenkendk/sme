using System;
using SME;

namespace Tester
{
	public class SimpleMockMemory : SimpleProcess
	{
		[InputBus, OutputBus]
		private IMemoryInterface Interface;

		private ulong[] m_data;
		private int m_cycle = 0;

		public SimpleMockMemory()
			: base()
		{
			m_data = new ulong[1024];
		}

		protected override void OnTick()
		{
			Console.WriteLine("Phase: {0}", ++m_cycle);

			if (Interface.ReadEnabled)
			{
				Console.WriteLine("Setting readvalue to {0}", m_data[Interface.ReadAddr]);
				Interface.ReadValue = m_data[Interface.ReadAddr];
			}

			if (Interface.WriteEnabled)
				m_data[Interface.WriteAddr] = Interface.WriteValue;
		}
	}
}

