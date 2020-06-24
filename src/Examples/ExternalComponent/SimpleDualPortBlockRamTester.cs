using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SME;
using SME.Components;
using SME.VHDL;

namespace ExternalComponent
{
    public class SimpleDualPortBlockRamTester<TData> : SimulationProcess
    {
        private readonly SimpleDualPortMemory<TData> m_bram;

        private readonly TData[] m_initial;
        private readonly TData[] m_rnd;

        private readonly bool init_is_random;

        [OutputBus]
        private readonly SimpleDualPortMemory<TData>.IReadControl m_rdcontrol;
        [InputBus]
        private readonly SimpleDualPortMemory<TData>.IReadResult m_rddata;
        [OutputBus]
        private readonly SimpleDualPortMemory<TData>.IWriteControl m_wrcontrol;

        public SimpleDualPortBlockRamTester(int memsize, bool random = false, int seed = 42, bool make_top_level = true)
        {
            var rndbuf = new byte[8];

            init_is_random = random;
            var rnd = new Random(seed);
            m_initial = new TData[memsize];

            m_rnd = new TData[m_initial.Length];
            for (var i = 0; i < m_initial.Length; i++)
            {
                rnd.NextBytes(rndbuf);
                m_rnd[i] = VHDLHelper.CreateIntType<TData>(BitConverter.ToUInt64(rndbuf, 0));

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

            m_bram = new SimpleDualPortMemory<TData>(memsize, m_initial);
            m_rdcontrol = m_bram.ReadControl;
            m_rddata = m_bram.ReadResult;
            m_wrcontrol = m_bram.WriteControl;

            if (make_top_level)
            {
                Simulation.Current.AddTopLevelInputs(m_rdcontrol, m_wrcontrol);
                Simulation.Current.AddTopLevelOutputs(m_rddata);
            }
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            // Wait for initialization to complete
            await ClockAsync();

            m_rdcontrol.Enabled = true;
            m_rdcontrol.Address = 0;

            m_wrcontrol.Enabled = false;
            m_wrcontrol.Address = 1;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length+1; i++)
            {
                m_rdcontrol.Enabled = i < m_initial.Length;
                m_rdcontrol.Address = i;
                await ClockAsync();
                TData expected = init_is_random ? m_initial[i-1] : VHDLHelper.CreateIntType<TData>((ulong)(i - 1));
                Debug.Assert(m_rddata.Data.Equals(expected),
                    $"Sequential test: Read problem at offset {i - 1}, value is {m_rddata.Data} but should be {expected}");
            }

            m_rdcontrol.Enabled = false;

            await ClockAsync();

            m_wrcontrol.Enabled = true;

            for (var i = 0; i < m_rnd.Length; i++)
            {
                m_wrcontrol.Address = i;
                m_wrcontrol.Data = m_rnd[i];

                await ClockAsync();
            }

            m_wrcontrol.Enabled = false;
            await ClockAsync();

            m_rdcontrol.Address = 0;
            m_rdcontrol.Enabled = true;
            await ClockAsync();

            for (var i = 1; i < m_rnd.Length+1; i++)
            {
                m_rdcontrol.Enabled = i < m_rnd.Length;
                m_rdcontrol.Address = i;
                await ClockAsync();
                Debug.Assert(m_rddata.Data.Equals(m_rnd[i - 1]), $"Random test: Read problem at offset {i-1}, value is {m_rddata.Data} but should be {m_rnd[i - 1]}");
            }
        }
    }
}
