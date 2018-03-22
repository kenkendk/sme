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
                Simulation.Current
                          .BuildCSVFile()
                          .BuildVHDL()
                          .Run(new Tester());
            }
        }

        [TopLevelInputBus, InitializedBus]
        public interface IControl : IBus
        {
            [InitialValue]
            bool valid { get; set; }
            int count { get; set; }
        }

        [TopLevelOutputBus, InitializedBus]
        public interface IResult : IBus
        {
            [InitialValue]
            bool valid { get; set; }
            int number { get; set; }
        }


        public class StatebasedCounterWithForLoop : StateProcess
        {
            [InputBus]
            public readonly IControl input = Scope.CreateOrLoadBus<IControl>();

            [OutputBus]
            public readonly IResult output = Scope.CreateOrLoadBus<IResult>();

            protected async override Task OnTickAsync()
            {
                while (ShouldContinue)
                {
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
        }

        public class StatebasedCounter : StateProcess
        {
            [InputBus]
            public readonly IControl input = Scope.CreateOrLoadBus<IControl>();

            [OutputBus]
            public readonly IResult output = Scope.CreateOrLoadBus<IResult>();

            protected async override Task OnTickAsync()
            {
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
            readonly StatebasedCounter counter1 = new StatebasedCounter();

            [OutputBus]
            readonly IControl control1;
            [InputBus]
            readonly IResult result1;

            public Tester()
            {
                control1 = counter1.input;
                result1 = counter1.output;
            }

            public async override Task Run()
            {
                await ClockAsync();
                control1.valid = true;
                control1.count = 4;

                await ClockAsync();
                control1.valid = false;

                for (var i = 0; i < 4; i++)
                {
                    if (!result1.valid || i != result1.number)
                        throw new Exception($"Failed in counter with number {i}");
                    await ClockAsync();
                }
            }
        }
    }
}
