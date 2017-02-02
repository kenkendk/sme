using System;
using System.IO;

namespace SME
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class CSVExtensionMethods
	{
		public static Simulation BuildCSVFile(this Simulation self, string filename = "trace.csv")
		{
			var tracer = new SME.Render.Transpiler.CSVTracer(new SME.Render.VHDL.ILConvert.VHDLGlobalInformation(), Path.Combine(self.TargetFolder, filename));

			self.AddTicker(x => tracer.OnClockTick());

			return self;
		}
	}
}