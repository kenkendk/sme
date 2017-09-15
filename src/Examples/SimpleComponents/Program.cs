using System;
using SME;

namespace SimpleComponents
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            SimpleFifoBuffer<byte> buffer1;
			SimpleFifoBuffer<byte> buffer2;
			SimpleFifoBuffer<byte> buffer3;
			SimpleFifoBuffer<byte> buffer4;

			ComponentTester tester1;
			ComponentTester tester2;
			ComponentTester tester3;
			ComponentTester tester4;

			// This is using the root scope
			buffer1 = new SimpleFifoBuffer<byte>(2);
            using (new Scope())
            {
                // This is using the scope, and will allocate new busses
                buffer2 = new SimpleFifoBuffer<byte>(2);

                // Create the tester while we have the scope set up
				tester2 = new ComponentTester(2);

				// This is using the same scope and will overwrite the previous
				buffer3 = new SimpleFifoBuffer<byte>(2);

                // This will use the newly created busses
				tester3 = new ComponentTester(3);
			}

            using (new Scope())
            {
                // This is using the new scope, and will not interfere with the others
                buffer4 = new SimpleFifoBuffer<byte>(2);

                // Create the tester while we have the scope set up
                tester4 = new ComponentTester(4);
            }

            // This will use the root scope
	    	tester1 = new ComponentTester(1);

            // Fire it up
            new Simulation()
                .AddTicker(ticks => Console.WriteLine("Ticked {0}", ticks))
                .Run(
                    buffer1, buffer2, buffer3, buffer4,
                    tester1, tester2, tester3, tester4
                );

		}
    }
}
