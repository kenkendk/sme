using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SME;
using SME.VHDL;

namespace ExternalComponent
{
    public class TrueDualPortBlockRamTester<TData> : SimulationProcess
    {
        private readonly SME.Components.TrueDualPortMemory<TData> m_bram;

        private readonly TData[] m_initial;
        private readonly TData[] m_rnd;

        private readonly bool init_is_random;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<TData>.IControl m_controla;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<TData>.IReadResult m_rda;

        [OutputBus]
        private readonly SME.Components.TrueDualPortMemory<TData>.IControl m_controlb;

        [InputBus]
        private readonly SME.Components.TrueDualPortMemory<TData>.IReadResult m_rdb;

        public TrueDualPortBlockRamTester(int memsize, bool random = false, int seed = 42, bool make_top_level = true)
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

            m_bram = new SME.Components.TrueDualPortMemory<TData>(memsize, m_initial);
            m_controla = m_bram.ControlA;
            m_rda = m_bram.ReadResultA;
            m_controlb = m_bram.ControlB;
            m_rdb = m_bram.ReadResultB;

            if (make_top_level)
            {
                Simulation.Current.AddTopLevelInputs(m_controla, m_controlb);
                Simulation.Current.AddTopLevelOutputs(m_rda, m_rdb);
            }
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            // Wait for initialization to complete
            await ClockAsync();

            m_controla.Address = 0;
            m_controlb.Address = 0;

            m_controla.Enabled = true;
            m_controlb.Enabled = false;

            m_controla.IsWriting = false;
            m_controlb.IsWriting = false;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length+1; i++)
            {
                m_controla.Enabled = i < m_initial.Length;
                m_controla.Address = i;
                await ClockAsync();
                TData expected = init_is_random ? m_initial[i-1] : VHDLHelper.CreateIntType<TData>((ulong)(i - 1));
                Debug.Assert(m_rda.Data.Equals(expected),
                    $"Read problem at offset {i-1}, value is {m_rda.Data} but should be {expected}");
            }

            m_controla.Enabled = false;
            m_controlb.Enabled = true;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length+1; i++)
            {
                m_controlb.Enabled = i < m_initial.Length;
                m_controlb.Address = i;
                await ClockAsync();
                TData expected = init_is_random ? m_initial[i-1] : VHDLHelper.CreateIntType<TData>((ulong)(i - 1));
                Debug.Assert(m_rdb.Data.Equals(expected), $"Read problem at offset {i-1}, value is {m_rdb.Data} but should be {expected}");
            }

            m_controla.Enabled = false;
            m_controlb.Enabled = false;

            await ClockAsync();

            m_controla.Enabled = true;
            m_controla.IsWriting = true;

            for (var i = 0; i < m_rnd.Length; i++)
            {
                m_controla.Address = i;
                m_controla.Data = m_rnd[i];

                await ClockAsync();
            }

            m_controla.IsWriting = false;
            m_controla.Enabled = false;

            await ClockAsync();

            m_controlb.Address = 0;
            m_controlb.IsWriting = false;
            m_controlb.Enabled = true;

            await ClockAsync();

            for (var i = 1; i < m_rnd.Length; i++)
            {
                m_controlb.Address = i;
                await ClockAsync();
                TData expected = m_rnd[i-1];
                Debug.Assert(m_rdb.Data.Equals(expected),
                    $"Read problem at offset {i-1}, value is {m_rdb.Data} but should be {expected}");
            }

            m_controlb.IsWriting = false;
            m_controlb.Enabled = false;

            await ClockAsync();

            m_controlb.IsWriting = true;
            m_controlb.Enabled = true;

            for (var i = 0; i < m_rnd.Length; i++)
            {
                m_controlb.Address = i;
                m_controlb.Data = m_rnd[m_rnd.Length-i-1];

                await ClockAsync();
            }

            m_controlb.IsWriting = false;
            m_controlb.Enabled = false;

            await ClockAsync();

            m_controla.Address = 0;
            m_controla.IsWriting = false;
            m_controla.Enabled = true;

            await ClockAsync();

            for (var i = 1; i < m_rnd.Length+1; i++)
            {
                m_controla.Enabled = i < m_rnd.Length;
                m_controla.Address = i;
                await ClockAsync();
                TData expected = m_rnd[m_rnd.Length-i];
                Debug.Assert(m_rda.Data.Equals(expected),
                    $"Read problem at offset {i-1}, value is {m_rda.Data} but should be {expected}");
            }

            m_controla.IsWriting = false;
            m_controla.Enabled = false;

        }
    }
}
