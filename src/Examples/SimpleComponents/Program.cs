using System;
using SME;

namespace SimpleComponents
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (new Simulation())
            {
                // This is using the root scope
                var buffer1 = new SimpleFifoBuffer<byte>(depth: 2);

                using (new Scope())
                {
                    // This is using the scope, and will allocate new busses
                    var buffer2 = new SimpleFifoBuffer<byte>(depth: 2);

                    // Create the tester while we have the scope set up
                    var tester2 = new ComponentTester(offset: 2);

                    // This is using the same scope and will overwrite the previous
                    var buffer3 = new SimpleFifoBuffer<byte>(depth: 2);

                    // This will use the newly created busses
                    var tester3 = new ComponentTester(offset: 3);
                }

                using (new Scope())
                {
                    // This is using the new scope, and will not interfere with the others
                    var buffer4 = new SimpleFifoBuffer<byte>(depth: 2);

                    // Create the tester while we have the scope set up
                    var tester4 = new ComponentTester(offset: 4);
                }

                // This will use the root scope
                var tester1 = new ComponentTester(offset: 1);

                // Fire it up
                Simulation
                    .Current
                    .AddTicker(s => Console.WriteLine("Ticked {0}", Scope.Current.Clock.Ticks))
                    .BuildCSVFile()
                    .BuildVHDL()
                    .BuildCPP()
                    .Run();
            }

        }
    }
}
