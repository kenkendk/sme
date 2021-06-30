using System.Diagnostics;
using System.IO;
using System.Linq;
using SME;

namespace SimpleMIPS
{
    public class Tester : SimulationProcess
    {

        public Tester(uint[] mem, string output_file)
        {
            actual = mem;
            expected = File.ReadLines(output_file).Select(x => uint.Parse(x)).ToArray();
            Debug.Assert(actual.Length >= expected.Length);
        }

        [InputBus]
        public Terminate term;

        uint[] actual;
        uint[] expected;

        public async override System.Threading.Tasks.Task Run()
        {
            while (!term.flg)
                await ClockAsync();

            // Read end of memory, as program uses stack
            int stack_start = actual.Length - expected.Length;
            for (int i = 0; i < expected.Length; i++)
            {
                Debug.Assert(actual[stack_start + i] == expected[i], $"expected {expected[i]}, got {actual[stack_start + i]}");
            }
        }
    }
}
