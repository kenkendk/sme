using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace SimpleTrader
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
