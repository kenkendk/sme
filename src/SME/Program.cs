using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SME
{
	public static class Program
	{

		public static void Main(string[] args)
		{
			if (args == null || args.Length == 0)
			{
				Console.WriteLine("Usage: ");
				Console.WriteLine("{0} <assemblypath>");
				return;
			}

			Run(Assembly.LoadFile(args[0]));
		}


		public static IEnumerable<IProcess> Run(Assembly asm, string outputfolder = "output", string tracefile = "trace.csv", Action<IEnumerable<IProcess>, string> config = null, Action onclock = null)
		{
			var runner = new Simulation(outputfolder);
			if (config != null)
				runner.AddPreloader(config);

			if (!string.IsNullOrWhiteSpace(tracefile))
			{
				var tracetype = Type.GetType("SME.CSVExtensionMethods, SME.Render.VHDL.CSVTracer");
				if (tracetype != null)
				{
					tracetype.GetMethod("BuildCSVFile").Invoke(null, new object[] { runner, tracefile });
				}
				else
				{
					Console.WriteLine("No trace module found, not tracing this run");
				}
			}


			var vhdlrender = Type.GetType("SME.VHDLExtensionMethods, SME.Render.VHDL");
			if (vhdlrender != null)
			{
				vhdlrender.GetMethod("BuildVHDL").Invoke(null, new object[] { runner, null, tracefile });
			}
			else
			{
				Console.WriteLine("No VHDL Renderer found, skipping VHDL rendering");
			}

			var graphrender = Type.GetType("SME.GraphVizExtensionMethods, SME.Render.GraphViz");
			if (graphrender != null)
			{
				graphrender.GetMethod("BuildGraph").Invoke(null, new object[] { runner, "network.dot" });
			}
			else
			{
				Console.WriteLine("No GraphViz Renderer found, skipping VHDL rendering");
			}

			return runner.Run(asm);
		}
	}
}
