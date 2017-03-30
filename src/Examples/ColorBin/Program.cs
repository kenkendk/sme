using System;
using System.Linq;
using SME;

namespace ColorBin
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Faster test
			ImageInputSimulator.IMAGES = new[] { "image1.png" };

			var sim = new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
				;

			sim.Run(typeof(MainClass).Assembly);
		}
	}
}
