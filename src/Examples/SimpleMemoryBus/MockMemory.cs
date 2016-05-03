using System;
using SME;
using System.Threading.Tasks;

namespace Tester
{
	public class MockMemory : Process
	{
		[InputBus, OutputBus]
		private IMemoryInterface Interface;

		private ulong[] m_data;
		private int m_cycle = 0;

		public MockMemory()
			: base()
		{
			m_data = new ulong[1024];
		}

		public async override Task Run()
		{
			while (true)
			{
				await ClockAsync();

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
}

