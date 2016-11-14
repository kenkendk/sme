using System;
using SME;

namespace ColorBin
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Faster test
			ImageInputSimulator.IMAGES = new[] { "image1.png" };

			new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
				.Run(typeof(MainClass).Assembly);
		}
	}
}
