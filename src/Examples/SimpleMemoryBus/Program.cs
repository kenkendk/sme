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
			var targetfolder = Path.GetFullPath("output");
			if (!Directory.Exists(targetfolder))
				Directory.CreateDirectory(targetfolder);

			var tracer = new SME.Render.VHDL.CSVTracer(Path.Combine(targetfolder, "trace.csv"));
			var processes = SME.Loader.LoadAssemblies(typeof(IMemoryInterface).Assembly);
			Loader.RunUntilCompletion(tickcallback: () =>
			{
				Console.WriteLine("Ticked {0}", Clock.DefaultClock.Ticks);
				tracer.OnClockTick();
			});


			Console.WriteLine("Execution complete after {0} ticks", Clock.DefaultClock.Ticks);
		}
	}
}
