using System;
using System.Linq;
using System.IO;

namespace SimpleTraderTester
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var targetfolder = Path.GetFullPath("output");
			if (!Directory.Exists(targetfolder))
				Directory.CreateDirectory(targetfolder);

			var values = GenerateRandomValueSequence.GetUIntSequence().Take(500).ToArray();
			SimpleTradeImplementation.SimulatedValues.Values = values;
			SimpleTradeImplementation.SimulatedValues.ValueCount = (uint)values.Length;

			var processes = SME.Loader.LoadAssembly(typeof(SimpleTradeImplementation.SimulatedValues).Assembly);
			var tracer = new SME.Render.VHDL.CSVTracer(Path.Combine(targetfolder, "trace.csv"));
			SME.Loader.RunUntilCompletion(processes, () => { tracer.OnClockTick(); });

		}
	}
}
