using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SME;
using SME.Components;
using SME.VHDL;

namespace ExternalComponent
{
    public class SinglePortBlockRamTester<TData> : SimulationProcess
    {
        private readonly SinglePortMemory<TData> m_bram;

        private readonly TData[] m_initial;
        private readonly TData[] m_rnd;

        private readonly bool init_is_random;

        [OutputBus]
        private readonly SinglePortMemory<TData>.IControl m_control;
        [InputBus]
        private readonly SinglePortMemory<TData>.IReadResult m_rddata;

        public SinglePortBlockRamTester(int memsize, bool random = false, int seed = 42, bool make_top_level = true)
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

            m_bram = new SinglePortMemory<TData>(memsize, m_initial);
            m_control = m_bram.Control;
            m_rddata = m_bram.ReadResult;

            if (make_top_level)
            {
                Simulation.Current.AddTopLevelInputs(m_control);
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

            m_control.Enabled = true;
            m_control.Address = 0;
            m_control.IsWriting = false;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length+1; i++)
            {
                m_control.Enabled = i < m_initial.Length;
                m_control.Address = i;
                await ClockAsync();
                TData expected = init_is_random ? m_initial[i-1] : VHDLHelper.CreateIntType<TData>((ulong)(i-1));
                Debug.Assert(m_rddata.Data.Equals(expected),
                    $"Read problem at offset {i - 1}, value is {m_rddata.Data} but should be {expected}");
            }

            await ClockAsync();

            m_control.Enabled = true;
            m_control.IsWriting = true;

            for (var i = 0; i < m_rnd.Length; i++)
            {
                m_control.Address = i;
                m_control.Data = m_rnd[i];

                await ClockAsync();
            }

            m_control.Enabled = true;
            m_control.Address = 0;
            m_control.IsWriting = false;

            await ClockAsync();

            for (var i = 1; i < m_rnd.Length+1; i++)
            {
                m_control.Enabled = i < m_rnd.Length;
                m_control.Address = i;
                await ClockAsync();
                TData expected = m_rnd[i-1];
                Debug.Assert(m_rddata.Data.Equals(expected),
                    $"Read random problem at offset {i - 1}, value is {m_rddata.Data} but should be {expected}");
            }
        }
    }
}
