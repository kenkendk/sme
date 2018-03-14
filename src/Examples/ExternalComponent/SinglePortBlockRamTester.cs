using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;

namespace ExternalComponent
{
    public class SinglePortBlockRamTester<TData> : SimulationProcess
    {
        private readonly SME.Components.SinglePortMemory<TData> m_bram;

        private readonly TData[] m_initial;
        private readonly TData[] m_rnd;

        [OutputBus]
        private readonly SME.Components.SinglePortMemory<TData>.IControl m_control;
        [InputBus]
        private readonly SME.Components.SinglePortMemory<TData>.IReadResult m_rddata;

        public SinglePortBlockRamTester(int memsize, bool random = false, int seed = 42)
        {
            var rndbuf = new byte[8];

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

            m_bram = new SME.Components.SinglePortMemory<TData>(memsize, m_initial);
            m_control = m_bram.Control;
            m_rddata = m_bram.ReadResult;

            Simulation.Current.AddTopLevelInputs(m_control);
            Simulation.Current.AddTopLevelOutputs(m_rddata);
        }

        /// <summary>
        /// Run this instance.
        /// </summary>
        public override async Task Run()
        {
            // Wait for initialization to complete
            await ClockAsync();

            m_control.Address = 0;

            m_control.IsWriting = false;
            m_control.Enabled = true;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length; i++)
            {
                m_control.Address = i;
                await ClockAsync();
                if (m_rddata.Data.ToString() != VHDLHelper.CreateIntType<TData>((ulong)(i - 1)).ToString())
                    Console.WriteLine($"Read problem at offset {i - 1}, value is {m_rddata.Data} but should be {i - 1}");
            }

            await ClockAsync();
            m_control.IsWriting = true;

            for (var i = 0; i < m_rnd.Length; i++)
            {
                m_control.Address = i;
                m_control.Data = m_rnd[i];

                await ClockAsync();
            }

            m_control.IsWriting = false;
            m_control.Address = 0;
            m_control.Enabled = true;

            await ClockAsync();

            for (var i = 1; i < m_rnd.Length; i++)
            {
                m_control.Address = i;
                await ClockAsync();
                if (m_rddata.Data.ToString() != m_rnd[i - 1].ToString())
                    Console.WriteLine($"Read random problem at offset {i - 1}, value is {m_rddata.Data} but should be {m_rnd[i - 1]}");
            }
        }
    }
}
