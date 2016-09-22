using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using SME;

namespace SimpleTraderTester
{
	[ClockedProcess]
	class SimulationDriver : Process
	{
		[OutputBus]
		private SimpleTradeImplementation.ITraderInput Output;

		public override async Task Run()
		{
			var rn = new Random();
			Output.Valid = false;

			foreach(var v in GenerateRandomValueSequence.GetUIntSequence().Take(500))
			{
				await ClockAsync();

				// Simulate bubbles in the input
				if (rn.NextDouble() > 0.85)
				{
					Output.Valid = false;
					await ClockAsync();
				}

				Output.Valid = true;
				Output.Value = v;
			}

			await ClockAsync();
			Output.Valid = false;
			await ClockAsync();
		}
	}


	class MainClass
	{
		public static void Main(string[] args)
		{
			var targetfolder = Path.GetFullPath("output");
			if (!Directory.Exists(targetfolder))
				Directory.CreateDirectory(targetfolder);

			var processes = Loader.LoadAssemblies(typeof(SimpleTradeImplementation.TraderCoreEWMA).Assembly).ToList();
			processes.AddRange(Loader.LoadItems(new SimulationDriver()));

			var tracer = new SME.Render.VHDL.CSVTracer(Path.Combine(targetfolder, "trace.csv"));
			SME.Loader.RunUntilCompletion(processes, () => { tracer.OnClockTick(); });

		}
	}
}
