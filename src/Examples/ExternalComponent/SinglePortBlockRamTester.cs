using System;
using System.Threading.Tasks;
using SME;
using SME.VHDL;

namespace ExternalComponent
{
    public class SinglePortBlockRamTester<TAddr, TData> : SimulationProcess
    {
        private readonly SME.VHDL.Components.SinglePortMemory<TAddr, TData> m_bram;

        private readonly TData[] m_initial;
        private readonly TData[] m_rnd;

        [OutputBus]
        private readonly SME.VHDL.Components.SinglePortMemory<TAddr, TData>.IInput m_control;
        [InputBus]
        private readonly SME.VHDL.Components.SinglePortMemory<TAddr, TData>.IOutput m_rddata;

        public SinglePortBlockRamTester(bool random = false, int seed = 42)
        {
            var rndbuf = new byte[8];

            var rnd = new Random(seed);
            m_initial = new TData[(int)Math.Pow(2, VHDLHelper.GetBitWidthFromType(typeof(TAddr)))];
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

            m_bram = new SME.VHDL.Components.SinglePortMemory<TAddr, TData>(m_initial);
            m_control = m_bram.Input;
            m_rddata = m_bram.Output;

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

            m_control.Address = VHDLHelper.CreateIntType<TAddr>((ulong)0);

            m_control.Address = VHDLHelper.CreateIntType<TAddr>((ulong)1);
            m_control.IsWriting = false;
            m_control.Enabled = true;

            await ClockAsync();

            for (var i = 1; i < m_initial.Length; i++)
            {
                m_control.Address = VHDLHelper.CreateIntType<TAddr>((ulong)i);
                await ClockAsync();
                if (m_rddata.Data.ToString() != VHDLHelper.CreateIntType<TData>((ulong)(i - 1)).ToString())
                    Console.WriteLine($"Read problem at offset {i}, value is {m_rddata.Data} but should be {VHDLHelper.CreateIntType<TData>((ulong)i)}");
            }

            await ClockAsync();
            m_control.IsWriting = true;

            for (var i = 1; i < m_rnd.Length; i++)
            {
                m_control.Address = VHDLHelper.CreateIntType<TAddr>((ulong)i);
                m_control.Data = m_rnd[i];

                await ClockAsync();
            }

            m_control.IsWriting = false;
            await ClockAsync();

            m_control.Address = VHDLHelper.CreateIntType<TAddr>((ulong)0);
            m_control.Enabled = true;

            for (var i = 1; i < m_rnd.Length; i++)
            {
                m_control.Address = VHDLHelper.CreateIntType<TAddr>((ulong)i);
                await ClockAsync();
                if (m_rddata.Data.ToString() != m_rnd[i - 1].ToString())
                    Console.WriteLine($"Read problem at offset {i}, value is {m_rddata.Data} but should be {VHDLHelper.CreateIntType<TData>((ulong)i)}");
            }


        }
    }
}
