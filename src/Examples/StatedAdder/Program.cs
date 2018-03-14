using System;
using SME;

namespace StatedAdder
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            // TODO simulate both normal and state machine adders
            using (new Simulation()) 
            {
                var adder = new Adder();
                var tester = new Tester();

                Simulation.Current
                          .BuildCSVFile()
                          .BuildVHDL()
                          .Run();
            }
        }
    }

    [TopLevelInputBus, InitializedBus]
    public interface Inp : IBus 
    {
        uint a { get; set; }
        uint b { get; set; }
        uint c { get; set; }
    }

    [TopLevelOutputBus, InitializedBus]
    public interface Outp : IBus 
    {
        uint sum { get; set; }
    }

    /*[ClockedProcess]
    public class Adder : Process 
    {
        [InputBus]
        Inp input = Scope.CreateOrLoadBus<Inp>();

        [OutputBus]
        Outp output = Scope.CreateOrLoadBus<Outp>();

        uint tmp0 = 0;
        uint tmp1 = 0;

        public async override System.Threading.Tasks.Task Run() 
        {
            while (true) 
            {
                await ClockAsync();
                tmp0 = input.a + input.b;
                tmp1 = input.c;

                await ClockAsync();
                output.sum = tmp0 + tmp1;
            }
        }
    }*/

    [ClockedProcess]
    public class Adder : SimpleProcess 
    {
        [InputBus]
        Inp input = Scope.CreateOrLoadBus<Inp>();

        [OutputBus]
        Outp output = Scope.CreateOrLoadBus<Outp>();

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
        [InputBus]
        Outp output = Scope.CreateOrLoadBus<Outp>();

        [OutputBus]
        Inp input = Scope.CreateOrLoadBus<Inp>();

        public async override System.Threading.Tasks.Task Run()
        {
            await ClockAsync();
            input.a = 1;
            input.b = 2;
            input.c = 3;

            await ClockAsync();
            System.Diagnostics.Debug.Assert(output.sum == 0, string.Format("sum is {0}, expected {1}", output.sum, 0));
            await ClockAsync();
            System.Diagnostics.Debug.Assert(output.sum == 0, string.Format("sum is {0}, expected {1}", output.sum, 0));
            await ClockAsync();
            System.Diagnostics.Debug.Assert(output.sum == 0, string.Format("sum is {0}, expected {1}", output.sum, 0));
            await ClockAsync();
            System.Diagnostics.Debug.Assert(output.sum == 6, string.Format("sum is {0}, expected {1}", output.sum, 6));
        }
    }
}
