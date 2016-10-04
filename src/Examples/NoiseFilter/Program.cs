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
				//.BuildVHDL()
				.Run(typeof(MainClass).Assembly);
		}
	}
}
