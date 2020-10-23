using System;
using SME;
using SME.Components;

namespace FloatingPoint
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                new AddTest();

                sim.Run();
            }
        }
    }

    class AddTest
    {
        public AddTest()
        {
            Func<float, float, float> impl = (a,b) => a + b;
            var op = SME.Components.FloatingPoint.Operations.Add;
            var lat = SME.Components.FloatingPoint.latency_dict[op].Item1;
            var component = new SME.Components.FloatingPoint(op, lat);
            var tester = new TesterFloat(op, impl, lat, 1000);

            tester.component_input_a = component.s_axis_a;
            tester.component_input_b = component.s_axis_b;
            tester.component_result = component.m_axis_result;
        }
    }
}
