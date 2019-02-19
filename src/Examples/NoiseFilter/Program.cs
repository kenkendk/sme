using System;
using System.IO;
using SME;

namespace NoiseFilter
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
                .BuildCPP()
                .BuildJsonFile()
				.Run(
                    new ImageInputSimulator("image1.png"),
                    new BorderEmitter(),
                    new StencilEmitter(),
                    new StencilApplier(),
                    new ImageOutputSink()
                );
		}
	}
}
