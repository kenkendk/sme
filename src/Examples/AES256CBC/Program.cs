using System;
using System.IO;

namespace AES256CBCTester
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var targetfolder = Path.GetFullPath("output");
			if (!Directory.Exists(targetfolder))
				Directory.CreateDirectory(targetfolder);

			var items = SME.Loader.LoadAssemblies(typeof(AES256CBC.AESCore).Assembly);

			var tracer = new SME.Render.VHDL.CSVTracer(Path.Combine(targetfolder, "trace.csv"));
			SME.Loader.RunUntilCompletion(items, () => { tracer.OnClockTick(); });
			SME.Render.VHDL.Renderer.Render(items, "output", null, "trace.csv");
		}
	}
}
