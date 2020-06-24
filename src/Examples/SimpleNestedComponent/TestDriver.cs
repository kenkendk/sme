using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SME;

namespace SimpleNestedComponent
{
    public class TestDriver : SimulationProcess
    {
        [OutputBus]
        public CounterInput Input = Scope.CreateBus<CounterInput>();

        [InputBus]
        public CounterOutput Output;

        public async override Task Run()
        {
            await ClockAsync();

            Input.InputEnabled = true;
            Input.StartRegister = 4;
            Input.RepeatCount = 4;

            await ClockAsync();

            await WaitUntilAsync(() => { Input.InputEnabled = false; return Output.OutputEnabled; });

            for (int i = 4; i < 8; i++)
            {
                Debug.Assert(Output.RegisterNumber == i, $"Output is {Output.RegisterNumber}, expected {i}");
                if (i < 7)
                    await WaitUntilAsync(() => Output.OutputEnabled);
            }

            await ClockAsync();

            Debug.Assert(!Output.OutputEnabled, $"Output should not be enabled");
        }
    }
}

