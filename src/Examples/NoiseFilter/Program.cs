using System;
using System.IO;
using SME;

namespace NoiseFilter
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Faster test
			ImageInputSimulator.IMAGES = new [] { "image1.png" };
			
			new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
				.Run(typeof(MainClass).Assembly);
		}
	}
}
