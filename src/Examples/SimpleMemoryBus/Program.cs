using SME;
using System;
using System.IO;
using System.Linq;

namespace Tester
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			new Simulation()
				.BuildCSVFile()
				.Run(typeof(MainClass).Assembly);


		}
	}
}
