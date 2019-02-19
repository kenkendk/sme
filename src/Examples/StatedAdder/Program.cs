using System;
using System.Threading.Tasks;
using SME;

namespace StatedAdder
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (new Simulation()) 
            {
                Simulation.Current
                          .BuildCSVFile()
                          .BuildVHDL()
                          .Run(new Tester());
            }
        }
    }

    [TopLevelInputBus, InitializedBus]
    public interface IControl : IBus 
    {
        uint a { get; set; }
        uint b { get; set; }
        uint c { get; set; }
    }

    [TopLevelOutputBus, InitializedBus]
    public interface IResult : IBus 
    {
        uint sum { get; set; }
    }


    [ClockedProcess]
    public class Adder2 : StateProcess 
    {
        [InputBus]
        public readonly IControl input = Scope.CreateOrLoadBus<IControl>();

        [OutputBus]
        public readonly IResult output = Scope.CreateOrLoadBus<IResult>();

        protected async override Task OnTickAsync()
        {
            var tmp0 = input.a + input.b;
            var tmp1 = input.c;

            await ClockAsync();
            output.sum = tmp0 + tmp1;
        }
    }

    [ClockedProcess]
    public class Adder : SimpleProcess 
    {
        [InputBus]
        public readonly IControl input = Scope.CreateOrLoadBus<IControl>();

        [OutputBus]
        public readonly IResult output = Scope.CreateOrLoadBus<IResult>();

        uint tmp0 = 0;
        uint tmp1 = 0;
        uint state = 0;

        protected override void OnTick()
        {
            if (state == 0) {
                tmp0 = input.a + input.b;
                tmp1 = input.c;
                state = 1;
            } else if (state == 1) {
                output.sum = tmp0 + tmp1;
                state = 0;
            }
        }
    }

    public class Tester : SimulationProcess 
    {
        //readonly Adder manual_state_adder = new Adder();
        readonly Adder2 auto_state_adder = new Adder2();

        //[OutputBus]
        //readonly IControl control_manual;
        //[InputBus]
        //readonly IResult result_manual;
        [OutputBus]
        readonly IControl control_auto;
        [InputBus]
        readonly IResult result_auto;

        public Tester()
        {
            //control_manual = manual_state_adder.input;
            //result_manual = manual_state_adder.output;
            control_auto = auto_state_adder.input;
            result_auto = auto_state_adder.output;
        }

        public async override Task Run()
        {

            await ClockAsync();
            //control_manual.a = control_auto.a = 1;
            //control_manual.b = control_auto.b = 2;
            //control_manual.c = control_auto.c = 3;

            control_auto.a = 1;
            control_auto.b = 2;
            control_auto.c = 3;

            await ClockAsync();
            //System.Diagnostics.Debug.Assert(result_manual.sum == 0, string.Format("sum is {0}, expected {1}", result_manual.sum, 0));
            System.Diagnostics.Debug.Assert(result_auto.sum == 0, string.Format("sum is {0}, expected {1}", result_auto.sum, 0));
            await ClockAsync();
            //System.Diagnostics.Debug.Assert(result_manual.sum == 0, string.Format("sum is {0}, expected {1}", result_manual.sum, 0));
            System.Diagnostics.Debug.Assert(result_auto.sum == 0, string.Format("sum is {0}, expected {1}", result_auto.sum, 0));
            await ClockAsync();
            //System.Diagnostics.Debug.Assert(result_manual.sum == 0, string.Format("sum is {0}, expected {1}", result_manual.sum, 0));
            System.Diagnostics.Debug.Assert(result_auto.sum == 0, string.Format("sum is {0}, expected {1}", result_auto.sum, 0));
            await ClockAsync();
            //System.Diagnostics.Debug.Assert(result_manual.sum == 6, string.Format("sum is {0}, expected {1}", result_manual.sum, 6));
            System.Diagnostics.Debug.Assert(result_auto.sum == 6, string.Format("sum is {0}, expected {1}", result_auto.sum, 6));
        }
    }
}
