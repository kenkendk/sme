using System;
using SME;
using SME.Components;

namespace FloatingPoint
{
    public class TesterFloat : Tester<float>
    {
        public TesterFloat(SME.Components.FloatingPoint.Operations operation, Func<float,float,float> operation_impl, int latency, int count) : base(operation, operation_impl, latency, count)
        { 
            base.threshold = .00001f;
            base.compare = (a,b) => (a - b) < threshold;
            base.get_random = () => (float)rng.NextDouble();
            base.init();
        }
    }

    /// <summary>
    /// Class for testing floating point operations, which take 2 inputs of type T.
    /// </summary>
    public class Tester<T> : SimulationProcess
    {
        //public abstract bool compare(T a, T b);
        //public abstract T get_random();
        protected Func<T,T,bool> compare;
        protected Func<T> get_random;

        [InputBus]
        public SME.Components.FloatingPoint.AXIS component_result;

        [OutputBus]
        public SME.Components.FloatingPoint.AXIS component_input_a;
        [OutputBus]
        public SME.Components.FloatingPoint.AXIS component_input_b;

        SME.Components.FloatingPoint.Operations operation;
        Func<T, T, T> operation_impl;
        int latency;
        int i = 0, j = 0, k = 0;
        bool was_valid_i = false, was_valid_j = false, was_ready_k = false;
        protected T threshold;

        public Random rng = new Random();

        T[] input_a, input_b, result, expected_result;

        public Tester(SME.Components.FloatingPoint.Operations operation, Func<T,T,T> operation_impl, int latency, int count)
        {
            this.operation = operation;
            this.operation_impl = operation_impl;
            this.latency = latency;
            
            input_a = new T[count];
            input_b = new T[count];
            result = new T[count];
            expected_result = new T[count];
        }

        protected void init()
        {
            for (int i = 0; i < input_a.Length; i++)
            {
                input_a[i] = get_random();
                input_b[i] = get_random();
                expected_result[i] = operation_impl(input_a[i], input_b[i]);
            }
        }

        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();

            while (k < result.Length)
            {
                if (was_valid_i && component_input_a.tready)
                    i++;
                if (i < input_a.Length)
                    component_input_a.tdata = SME.Components.FloatingPoint.from_float((float)(object)input_a[i]);
                component_input_a.tvalid = was_valid_i = i < input_a.Length;

                if (was_valid_j && component_input_a.tready)
                    j++;
                if (j < input_b.Length)
                    component_input_a.tdata = SME.Components.FloatingPoint.from_float((float)(object)input_b[j]);
                component_input_a.tvalid = was_valid_j = j < input_b.Length;

                component_result.tready = true;
                if (component_result.tvalid)
                {
                    result[k] = (T)(object)SME.Components.FloatingPoint.from_uint(component_result.tdata);
                    k++;
                }

                await ClockAsync();
            }

            bool matched = true;
            for (int i = 0; i < result.Length; i++)
                matched &= compare(expected_result[i], result[i]);
            System.Diagnostics.Debug.Assert(matched);
        }

    } 

}