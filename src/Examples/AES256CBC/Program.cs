using System;
using System.IO;
using SME;

namespace AES256CBC
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            //Tester.NUMBER_OF_RUNS = 10000;

			new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
                .BuildCPP()
				.Run(typeof(MainClass).Assembly);
		}
	}
}
