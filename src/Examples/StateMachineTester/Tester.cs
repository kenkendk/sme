using SME;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StateMachineTester
{
    public class Tester : SimulationProcess
    {
        public Tester(StateMachineTest test)
        {
            name = test.GetType().Name;
            go1s = test.go1s;
            go2s = test.go2s;
            values = test.values;
            states = test.states;

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
        int[] values;
        int[] states;

        public override async Task Run()
        {
            await ClockAsync();

            for (int i = 0; i < states.Length; i++)
            {
                if (i < go1s.Length) control.Go1 = go1s[i];
                if (i < go2s.Length) control.Go2 = go2s[i];
                if (i < values.Length) control.Value = values[i];
                await ClockAsync();
                Debug.Assert(states[i] == result.State, $"{name}: state in step {i} not correct. Expected {states[i]}, got {result.State}");
            }
        }
    }
}