using System;
using SME;
using System.Threading.Tasks;

namespace SimpleMemoryBus
{
    public class MockMemory : Process
    {
        [InputBus, OutputBus]
        public IMemoryInterface Interface = Scope.CreateBus<IMemoryInterface>();

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

