using SME;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace StateMachineTester
{

    public abstract class StateMachineTest : StateProcess
    {
        [InputBus] public IControlBus control;
        [OutputBus] public IResultBus result;

        [Ignore] public bool[] go1s;
        [Ignore] public bool[] go2s;
        [Ignore] public int[] states;
    }

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

    public class SingleIfStatement : StateMachineTest
    {

        public SingleIfStatement()
        {
            this.go1s = new bool[] { false, true, false };
            this.go2s = new bool[] { };
            this.states = new int[] { 3, 1, 3 };
            this.control = Scope.CreateBus<IControlBus>();
            this.result = Scope.CreateBus<IResultBus>();
        }

        protected async override Task OnTickAsync()
        {
            result.State = 0;
            if (control.Go1)
            {
                result.State = 1;
                await ClockAsync();
                result.State = 2;
            }
            result.State = 3;
        }
    }

    public class Tester : SimulationProcess
    {

        public Tester(StateMachineTest test)
        {
            this.name = test.GetType().Name;
            this.go1s = test.go1s;
            this.go2s = test.go2s;
            this.states = test.states;

            control = test.control;
            result = test.result;

            Simulation.Current
                .AddTopLevelInputs(control)
                .AddTopLevelOutputs(result);
        }

        [OutputBus] public IControlBus control;

        [InputBus] public IResultBus result;

        string name;
        bool[] go1s;
        bool[] go2s;
        int[] states;

        public override async Task Run()
        {
            await ClockAsync();

            int len = Math.Max(go1s.Length, go2s.Length);
            for (int i = 0; i < len; i++)
            {
                if (i < go1s.Length) control.Go1 = go1s[i];
                if (i < go2s.Length) control.Go2 = go2s[i];
                await ClockAsync();
                Debug.Assert(states[i] == result.State, $"{name}: state in step {i} not correct. Expected {states[i]}, got {result.State}");
            }
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
            using (var sim = new Simulation())
            {
                StateMachineTest[] tests = {
                    new SingleIfStatement(),
                    //new SingleIfElseStatement(),
                    //new SingleWhileLoop(),
                    //new SingleForLoop(),
                    //new SingleSwitchStatement(),
        
                    //new NestedIfStatement(),
                    //new NestedIfElseStatement(),
                    //new NestedWhileLoop(),
                    //new NestedForLoop(),
                    //new NestedSwitchStatement(),
                };
                
                //tests.Select(x => new Tester(x));
                foreach (StateMachineTest test in tests)
                    new Tester(test);

                sim
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
