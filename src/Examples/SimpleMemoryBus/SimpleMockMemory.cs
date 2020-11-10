using SME;
using System;

namespace SimpleMemoryBus
{
    public class SimpleMockMemory : SimpleProcess
    {
        [InputBus, OutputBus]
        public IMemoryInterface Interface = Scope.CreateBus<IMemoryInterface>();

        private ulong[] m_data = new ulong[1024];
        private int m_cycle = 0;

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

