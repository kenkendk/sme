using System;
using SME;

namespace Stopwatch
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                var watch = new Stopwatch();
                var counter = new Counter();
                var tester = new Tester();

                watch.buttons = tester.buttons;
                counter.watch = watch.output;
                tester.number = counter.output;
                tester.watch = watch.output;

                sim
                    .AddTopLevelInputs(watch.buttons)
                    .AddTopLevelOutputs(counter.output)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
