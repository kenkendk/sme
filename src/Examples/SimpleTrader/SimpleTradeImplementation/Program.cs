using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace SimpleTraderTester
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			new Simulation()
				.BuildCSVFile()
				.BuildGraph()
				.BuildVHDL()
				.Run(typeof(MainClass).Assembly);
		}
	}
}
