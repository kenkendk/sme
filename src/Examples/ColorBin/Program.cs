using System;
using System.Linq;
using SME;

namespace ColorBin
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var sim = new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
				.BuildCPP()
				;

			sim.Run(
                new ImageInputSimulator("image1.png"),
                new ColorBinCollector()
            );
		}
	}
}
