using System;
using System.IO;

namespace SME
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class ExtensionMethodsTracer
	{
		/// <summary>
		/// Extension method for adding CSV output from a simulation
		/// </summary>
		/// <returns>The runner.</returns>
		/// <param name="self">The runner.</param>
		/// <param name="filename">The output filename.</param>
		public static Simulation BuildCSVFile(this Simulation self, string filename = "trace.csv")
		{
			var tracer = new Tracer.CSVTracer(filename, self.TargetFolder);
			self.AddTicker(tracer.OnClockTick);
			self.AddPostloader(_ => { tracer.Dispose(); });
			return self;
		}

		/// <summary>
		/// Extension method for adding CSV output from a simulation
		/// </summary>
		/// <returns>The runner.</returns>
		/// <param name="self">The runner.</param>
		/// <param name="filename">The output filename.</param>
		public static Simulation BuildJsonFile(this Simulation self, string filename = "trace.json")
		{
			var tracer = new Tracer.JsonTracer(filename, self.TargetFolder);
			self.AddTicker(tracer.OnClockTick);
			self.AddPostloader(_ => { tracer.Dispose(); });
			return self;
		}
	}
}