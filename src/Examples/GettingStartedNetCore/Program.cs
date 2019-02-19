using System;
using System.Linq;
using SME;

namespace GettingStarted
{
	class MainClass
	{
		public static void Main(string[] args)
		{

            using(var sim = new Simulation())
            {
                var simulator = new ImageInputSimulator("image1.png");
                var calculator = new ColorBinCollector(simulator.Data);

                // Use fluent syntax to configure the simulator.
                // The order does not matter, but `Run()` must be 
                // the last method called.

                // The top-level input and outputs are exposed
                // for interfacing with other VHDL code or board pins

                sim
                    .AddTopLevelOutputs(calculator.Output)
                    .AddTopLevelInputs(simulator.Data)
                    .BuildCSVFile()
                    .BuildVHDL()
    			    .Run();

                // After `Run()` has been invoked the folder
                // `output/vhdl` contains a Makefile that can
                // be used for testing the generated design
            }
		}
	}
}
