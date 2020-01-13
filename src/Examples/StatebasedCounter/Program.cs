using System;
using System.Threading.Tasks;
using SME;

namespace StatebasedCounter
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            using (new Simulation())
            {
                var counter1 = new StatebasedCounter();
                var counter2 = new StatebasedCounterWithForLoop();
                var tester1 = new Tester();
                var tester2 = new Tester();

                tester1.result = counter1.output;
                tester2.result = counter2.output;
                counter1.input = tester1.control;
                counter2.input = tester2.control;

                Simulation.Current
                    .AddTopLevelInputs(counter1.input, counter2.input)
                    .AddTopLevelOutputs(counter1.output, counter2.output)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }

        [InitializedBus]
        public interface IControl : IBus
        {
            [InitialValue]
            bool valid { get; set; }
            int count { get; set; }
        }

        [InitializedBus]
        public interface IResult : IBus
        {
            [InitialValue]
            bool valid { get; set; }
            int number { get; set; }
        }


        public class StatebasedCounterWithForLoop : StateProcess
        {
            [InputBus]
            public IControl input;

            [OutputBus]
            public IResult output = Scope.CreateBus<IResult>();

            protected async override Task OnTickAsync()
            {
                output.valid = false;
                while (!input.valid)
                    await ClockAsync();

                var count = input.count;
                for (var i = 0; i < count; i++)
                {
                    output.number = i;
                    output.valid = true;
                    await ClockAsync();
                }
            }
        }

        public class StatebasedCounter : StateProcess
        {
            [InputBus]
            public IControl input;

            [OutputBus]
            public IResult output = Scope.CreateBus<IResult>();

            protected async override Task OnTickAsync()
            {
                output.valid = false;
                while (!input.valid)
                    await ClockAsync();

                var count = input.count;
                var i = 0;
                while (i < count)
                {
                    output.number = i;
                    output.valid = true;
                    await ClockAsync();
                    i++;
                }
            }
        }

        public class Tester : SimulationProcess
        {
            [OutputBus]
            public IControl control = Scope.CreateBus<IControl>();

            [InputBus]
            public IResult result;

            public async override Task Run()
            {
                await ClockAsync();
                control.valid = true;
                control.count = 4;

                await ClockAsync();
                control.valid = false;

                for (var i = 0; i < 4; i++)
                {
                    if (!result.valid && i != result.number)
                        throw new Exception($"Failed in counter with number {i}");
                    await ClockAsync();
                }
            }
        }
    }
}
