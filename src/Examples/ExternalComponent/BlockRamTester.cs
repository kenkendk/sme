using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;

namespace ExternalComponent
{
    public class BlockRamTester<TAddr, TData> : SimulationProcess
    {
        private readonly SME.VHDL.Components.SimpleDualPortMemory<TAddr, TData> m_bram;

        private readonly TData[] m_initial;

        [OutputBus]
        private readonly SME.VHDL.Components.SimpleDualPortMemory<TAddr, TData>.IReadIn m_rdcontrol;
        [InputBus]
        private readonly SME.VHDL.Components.SimpleDualPortMemory<TAddr, TData>.IReadOut m_rddata;
        [OutputBus]
        private readonly SME.VHDL.Components.SimpleDualPortMemory<TAddr, TData>.IWriteIn m_wrcontrol;

        public BlockRamTester(bool random = false, int seed = 42)
        {
            var rndbuf = new byte[8];

            var rnd = new Random(seed);
            m_initial = new TData[(int)Math.Pow(2, VHDLHelper.GetBitWidthFromType(typeof(TAddr)))];
            for (var i = 0; i < m_initial.Length; i++)
            {
                if (random)
                {
                    rnd.NextBytes(rndbuf);
                    m_initial[i] = VHDLHelper.CreateIntType<TData>(BitConverter.ToUInt64(rndbuf, 0));
                }
                else
                {
                    m_initial[i] = VHDLHelper.CreateIntType<TData>((ulong)i);
                }
            }

            m_bram = new SME.VHDL.Components.SimpleDualPortMemory<TAddr, TData>(m_initial);
            m_rdcontrol = m_bram.ReadIn;
            m_rddata = m_bram.ReadOut;
            m_wrcontrol = m_bram.WriteIn;

            Simulation.Current.AddTopLevelInputs(m_rdcontrol, m_wrcontrol);
            Simulation.Current.AddTopLevelOutputs(m_rddata);
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            // Wait for initialization to complete
            await ClockAsync();

            m_rdcontrol.Address = VHDLHelper.CreateIntType<TAddr>((ulong)0);

            m_wrcontrol.Address = VHDLHelper.CreateIntType<TAddr>((ulong)0);
            m_wrcontrol.Enabled = false;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length; i++)
            {
                m_rdcontrol.Address = VHDLHelper.CreateIntType<TAddr>((ulong)i);
                await ClockAsync();
                if (m_rddata.Data.ToString() != VHDLHelper.CreateIntType<TData>((ulong)(i - 1)).ToString())
                    Console.WriteLine($"Read problem at offset {i}, value is {m_rddata.Data} but should be {VHDLHelper.CreateIntType<TData>((ulong)i)}");
            }


        }
    }
}
