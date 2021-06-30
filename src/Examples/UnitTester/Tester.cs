using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SME;

namespace UnitTester
{
    public class Tester : SimulationProcess
    {
        public Tester(Test test)
        {
            name = test.GetType().Name;
            test_input   = test.input;
            test_inputs  = test.inputs;
            test_output  = test.output;
            test_outputs = test.outputs;

            Simulation.Current
                .AddTopLevelInputs(test_input)
                .AddTopLevelOutputs(test_output);
        }

        [OutputBus] public ValueBus test_input;

        [InputBus]  public ValueBus test_output;

        string name;
        int[] test_inputs;
        int[] test_outputs;
        int i, j;

        public override async Task Run()
        {
            await ClockAsync();

            i = j = 0;

            while (j < test_outputs.Length)
            {
                test_input.valid = i < test_inputs.Length;
                test_input.value = i < test_inputs.Length ? test_inputs[i] : 0;
                i++;

                if (test_output.valid)
                {
                    Debug.Assert(
                        test_output.value == test_outputs[j],
                        $"Error with {name}: Expected {test_outputs[j]}, got {test_output.value}"
                    );
                    j++;
                }
                await ClockAsync();
            }
        }
    }

}