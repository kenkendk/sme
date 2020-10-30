using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SME.Components
{
    using xip_fpo_prec_t = Int64; // long
    using xip_fpo_ptr = IntPtr; // __xip_fpo_struct*
    using xip_fpo_sign_t = Int32; // int
    using xip_fpo_exp_t = Int64; // long
    using mp_limb_t = IntPtr; // mp_limb_t*
    using xip_fpo_t = IntPtr; // __xip_fpo_struct[1]
    using xip_fpo_exc_t = Int32; // int

    [StructLayout(LayoutKind.Sequential)]
    public struct __xip_fpo_struct
    {
        public xip_fpo_prec_t _xip_fpo_exp_prec;
        public xip_fpo_prec_t _xip_fpo_mant_prec;
        public xip_fpo_sign_t _xip_fpo_sign;
        public xip_fpo_exp_t _xip_fpo_exp;
        public mp_limb_t _xip_fpo_d;
    }

    [ClockedProcess]
    /// <summary>
    /// Component for interfacing / instantiating with the Xilinx floating point IP core.
    /// </summary>
    public class FloatingPoint : SimpleProcess
    {
        // Remember to set LD_LIBRARY_PATH environment variable
        const string floating_point_library = "libIp_floating_point_v7_1_bitacc_cmodel.so";

        // void xip_fpo_inits2(xip_fpo_prec_t, xip_fpo_prec_t, xip_fpo_ptr, ...);
        [DllImport (floating_point_library)]
        private static extern void xip_fpo_inits2(xip_fpo_prec_t exponent, xip_fpo_prec_t mantissa, xip_fpo_ptr value, xip_fpo_ptr null_term);
        // xip_fpo_exc_t xip_fpo_set_ui(xip_fpo_ptr, unsigned long);
        [DllImport (floating_point_library)]
        private static extern xip_fpo_exc_t xip_fpo_set_ui(xip_fpo_ptr arg0, UInt64 arg1);
        // xip_fpo_exc_t xip_fpo_set_flt(xip_fpo_ptr, float);
        [DllImport (floating_point_library)]
        private static extern xip_fpo_exc_t xip_fpo_set_flt(xip_fpo_ptr arg0, float arg1);
        // xip_fpo_exc_t xip_fpo_add(xip_fpo_ptr, xip_fpo_srcptr, xip_fpo_srcptr);
        [DllImport (floating_point_library)]
        private static extern xip_fpo_exc_t xip_fpo_add(xip_fpo_ptr result, xip_fpo_ptr inputa, xip_fpo_ptr inputb);
        // float xip_fpo_get_flt(xip_fpo_srcptr);
        [DllImport (floating_point_library)]
        private static extern float xip_fpo_get_flt(xip_fpo_ptr value);
        // void xip_fpo_clear (xip_fpo_ptr);
        [DllImport (floating_point_library)]
        private static extern void xip_fpo_clear(xip_fpo_ptr value);

        // TODO sørg for at den er simulationonly!
        public static float from_uint(uint input) 
        {
            byte[] tmp = new byte[8];
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = (byte)((input >> (i*8)) & 0xFF);
            }
            return BitConverter.ToSingle(tmp);
        }

        public static uint from_float(float input) 
        {
            byte[] tmp = BitConverter.GetBytes(input);
            uint result = 0;
            for (int i = 0; i < tmp.Length; i++)
            {
                result |= (uint)((uint)tmp[i] << (i*8));
                
            }
            return result;
        }

        private static float add(float ina, float inb)
        {
            xip_fpo_exp_t exp_prec, mant_prec;
            xip_fpo_t a, b, c;

            exp_prec = 16;
            mant_prec = 64;

            a = Marshal.AllocHGlobal(Marshal.SizeOf<__xip_fpo_struct>());
            b = Marshal.AllocHGlobal(Marshal.SizeOf<__xip_fpo_struct>());
            c = Marshal.AllocHGlobal(Marshal.SizeOf<__xip_fpo_struct>());
            
            xip_fpo_inits2(exp_prec, mant_prec, a, (xip_fpo_ptr) 0);
            xip_fpo_inits2(exp_prec, mant_prec, b, (xip_fpo_ptr) 0);
            xip_fpo_inits2(exp_prec, mant_prec, c, (xip_fpo_ptr) 0);
            
            var ex = xip_fpo_set_flt(a, ina);
            ex |= xip_fpo_set_flt(b, inb);
            ex |= xip_fpo_add(c, a, b);
            if (ex != 0)
                throw new Exception($"External function exception: {ex:x8}");
            float fc = xip_fpo_get_flt(c);

            xip_fpo_clear(a);
            xip_fpo_clear(b);
            xip_fpo_clear(c);
            
            Marshal.FreeHGlobal(a);
            Marshal.FreeHGlobal(b);
            Marshal.FreeHGlobal(c);

            return fc;
        }

        public enum Operations
        {
            Add
        }

        [InitializedBus]
        public interface AXIS : IBus
        {
            bool tvalid { get; set; }
            uint tdata  { get; set; }
            bool tready { get; set; }
            bool tlast  { get; set; }
        }

        [InputBus]
        public AXIS s_axis_a = Scope.CreateBus<AXIS>();
        [InputBus]
        public AXIS s_axis_b = Scope.CreateBus<AXIS>();

        [OutputBus]
        public AXIS m_axis_result = Scope.CreateBus<AXIS>();

        private bool was_valid = false;
        private bool was_readya = false;
        private bool was_readyb = false;
        private bool tmptvalida = false;
        private bool tmptvalidb = false;
        private uint tmptdataa;
        private uint tmptdatab;

        public static readonly Dictionary<Operations, (int,int)> latency_dict = new Dictionary<Operations, (int,int)>() {
            { Operations.Add, (3, 4) },
        };

        public readonly int latency;

        private (bool, float, float)[] in_flight;

        public FloatingPoint(Operations op, int latency)
        {
            var (min, max) = latency_dict[op];
            if (latency < min || latency > max)
                throw new Exception($"Error: provided latency ({latency}) is outside the range of the IP core: ({min},{max})");
            this.latency = latency;
            in_flight = new (bool, float, float)[latency].Select(_ => { return (false, .0f, .0f); }).ToArray();
        }

        protected override void OnTick()
        {
            // Handle the inputs
            if (was_readya && s_axis_a.tvalid)
            {
                tmptvalida = true;
                tmptdataa = s_axis_a.tdata;
            }

            if (was_readyb && s_axis_b.tvalid)
            {
                tmptvalidb = true;
                tmptdatab = s_axis_b.tdata;
            }

            if (tmptvalida && tmptvalidb)
            {
                in_flight[0] = (true, from_uint(tmptdataa), from_uint(tmptdatab));
                tmptvalida = false;
                tmptvalidb = false;
            }

            // Handle the outputs
            if (was_valid && m_axis_result.tready || (!was_valid))
            {
                var (valid, a, b) = in_flight[in_flight.Length-1];
                m_axis_result.tvalid = was_valid = valid;
                m_axis_result.tdata = from_float(add(a,b));
                in_flight[in_flight.Length-1] = (false, .0f, .0f);
            }

            // TODO tjek om den staller hele pipelinen, eller kun enkelte stages
            for (int i = in_flight.Length-1; i > 0; i--)
            {
                var (valid, _, _) = in_flight[i];
                if (!valid)
                {
                    in_flight[i] = in_flight[i-1];
                    in_flight[i-1] = (false, .0f, .0f);
                }
            }

            var (free_space, _, _) = in_flight[0];
            s_axis_a.tready = was_readya = !tmptvalida && !free_space;
            s_axis_b.tready = was_readyb = !tmptvalidb && !free_space;
        }
    }

}