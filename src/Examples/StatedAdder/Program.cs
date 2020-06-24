using SME;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StatedAdder
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (new Simulation())
            {
                var manual_adder = new Adder();
                var stated_adder = new StatedAdder();
                var tester = new Tester();

                manual_adder.input = tester.control;
                stated_adder.input = tester.control;
                tester.result_manual = manual_adder.output;
                tester.result_stated = stated_adder.output;

                Simulation.Current
                    .AddTopLevelInputs(manual_adder.input)
                    .AddTopLevelOutputs(manual_adder.output, stated_adder.output)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }

    [InitializedBus]
    public interface IControl : IBus
    {
        uint a { get; set; }
        uint b { get; set; }
        uint c { get; set; }
    }

    [InitializedBus]
    public interface IResult : IBus
    {
        uint sum { get; set; }
    }


    [ClockedProcess]
    public class StatedAdder : StateProcess
    {
        [InputBus]
        public IControl input;

        [OutputBus]
        public IResult output = Scope.CreateBus<IResult>();

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
        public IControl input;

        [OutputBus]
        public IResult output = Scope.CreateBus<IResult>();

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

        [OutputBus]
        public IControl control = Scope.CreateBus<IControl>();

        [InputBus]
        public IResult result_manual;

        [InputBus]
        public IResult result_stated;

        public async override Task Run()
        {
            await ClockAsync();

            control.a = 1;
            control.b = 2;
            control.c = 3;

            await ClockAsync();

            for (int i = 0; i < 4; i++)
            {
                uint expected = i == 3 ? 6u : 0u;
                Debug.Assert(result_manual.sum == result_stated.sum, 
                    $"Manual and Stated aren't equal: {result_manual.sum} != {result_stated.sum}");
                Debug.Assert(result_manual.sum == expected,
                    $"sum is {result_manual.sum}, expected {expected}");
                await ClockAsync();
            }
        }
    }
}
