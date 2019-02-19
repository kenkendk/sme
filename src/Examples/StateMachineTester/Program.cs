using System;
using System.Threading.Tasks;
using SME;

namespace StateMachineTester
{
    [TopLevelInputBus]
    public interface IControlBus : IBus
    {
        [InitialValue]
        bool Go1 { get; set; }
        [InitialValue]
        bool Go2 { get; set; }
        [InitialValue]
        int Value { get; set; }
    }

    [TopLevelOutputBus]
    public interface IResultBus : IBus
    {
        [InitialValue]
        int State { get; set; }
    }

    public class SingleIfStatement : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            if (controlBus.Go1)
            {
                resultBus.State = 1;
                await ClockAsync();
                //resultBus.State = 2;
            }
            resultBus.State = 3;
        }
    }

    public class NestedIfStatement : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            if (controlBus.Go1)
            {
                resultBus.State = 1;
                await ClockAsync();
                resultBus.State = 2;
                if (controlBus.Go2)
                {
                    resultBus.State = 3;
                    await ClockAsync();
                    resultBus.State = 4;
                }
                resultBus.State = 5;
            }
            resultBus.State = 6;
        }
    }

    public class SingleIfElseStatement : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            if (controlBus.Go1)
            {
                resultBus.State = 1;
                await ClockAsync();
                resultBus.State = 2;
            }
            else
            {
                resultBus.State = 3;
                await ClockAsync();
                resultBus.State = 4;
            }
            resultBus.State = 5;
        }
    }

    public class SingleWhileLoop : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            while (controlBus.Go1)
            {
                resultBus.State = 1;
                await ClockAsync();
                resultBus.State = 2;
            }
            resultBus.State = 3;
        }
    }

    public class NestedWhileLoop : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            while (controlBus.Go1)
            {
                resultBus.State = 1;
                while (controlBus.Go2)
                {
                    resultBus.State = 2;
                    await ClockAsync();
                    //resultBus.State = 3;
                }
                while (!controlBus.Go2)
                {
                    resultBus.State = 4;
                    await ClockAsync();
                    //resultBus.State = 5;
                }
                //resultBus.State = 6;
            }
            resultBus.State = 7;
        }
    }

    public class SingleForLoop : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            for (var i = 0; i < 5; i++)
            {
                resultBus.State = 1;
                await ClockAsync();
                resultBus.State = 2;
            }
            resultBus.State = 3;
        }
    }

    public class SingleSwitchStatement : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            switch (controlBus.Value)
            {
                case 0:
                    resultBus.State = 1;
                    await ClockAsync();
                    resultBus.State = 2;
                    break;
                case 1:
                    resultBus.State = 3;
                    await ClockAsync();
                    resultBus.State = 4;
                    break;
                default:
                    break;
            }
        }
    }

    public class NestedSwitchStatement : StateProcess
    {
        [InputBus]
        private readonly IControlBus controlBus = Scope.CreateBus<IControlBus>();
        [OutputBus]
        private readonly IResultBus resultBus = Scope.CreateBus<IResultBus>();

        protected async override Task OnTickAsync()
        {
            resultBus.State = 0;
            switch (controlBus.Value)
            {
                case 0:
                    switch (controlBus.Value)
                    {
                        case 0:
                            resultBus.State = 1;
                            await ClockAsync();
                            resultBus.State = 2;
                            break;
                        case 1:
                            resultBus.State = 3;
                            await ClockAsync();
                            resultBus.State = 4;
                            break;
                    }
                    break;
                case 1:
                    resultBus.State = 5;
                    await ClockAsync();
                    resultBus.State = 6;
                    break;
                default:
                    break;
            }
        }
    }


    public class DummySimulationTester : SimulationProcess
    {
        private readonly IControlBus controlBus = Scope.LoadBus<IControlBus>();
        private readonly IResultBus resultBus = Scope.LoadBus<IResultBus>();

        public async override Task Run()
        {
            await ClockAsync();
            controlBus.Go1 = true;
            await ClockAsync();
            controlBus.Go1 = false;
            controlBus.Go2 = true;
            await ClockAsync();
            controlBus.Go2 = false;
            await ClockAsync();
            for (var i = 0; i < 10; i++)
            {
                controlBus.Value = i;
                await ClockAsync();
            }
            await ClockAsync();
        }
    }

    public class NestedWhileLoopTester : SimulationProcess
    {
        private readonly IControlBus controlBus = Scope.LoadBus<IControlBus>();
        private readonly IResultBus resultBus = Scope.LoadBus<IResultBus>();

        public async override Task Run()
        {
            await ClockAsync();
            if (resultBus.State != 0)
                throw new Exception("Bad state");
            
            controlBus.Go1 = true;
            controlBus.Go2 = false;

            await ClockAsync();
            if (resultBus.State != 4)
                throw new Exception("Bad state");
            
            controlBus.Go1 = false;
            controlBus.Go2 = true;

            await ClockAsync();
            if (resultBus.State != 7)
                throw new Exception("Bad state");
            controlBus.Go1 = false;
            controlBus.Go2 = false;

            await ClockAsync();
            if (resultBus.State != 7)
                throw new Exception("Bad state");

        }
    }

    public class MainClass
    {
        public static void Main(string[] args)
        {
            new Simulation()
                .BuildCSVFile()
                .BuildVHDL()
                .Run(
                    //new SingleIfStatement(),
                    //new SingleIfElseStatement(),
                    //new NestedIfStatement(),
                    //new SingleWhileLoop(),
                    //new NestedWhileLoop(),
                    //new SingleForLoop(),
                    //new SingleSwitchStatement(),
                    new NestedSwitchStatement(),

                    new DummySimulationTester()
                );
        }
    }
}
