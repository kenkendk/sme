using SME;
using System;
using System.Linq;

namespace Tester
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Loader.RunUntilCompletion(tickcallback: () => Console.WriteLine("Ticked {0}", Clock.DefaultClock.Ticks));

			Console.WriteLine("Execution complete after {0} ticks", Clock.DefaultClock.Ticks);
		}
	}
}
