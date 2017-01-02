using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SME.Render.VHDL.ILConvert;

namespace SME
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class VHDLExtensionMethods
	{
		/// <summary>
		/// Extension method for building VHDL output after a simulation
		/// </summary>
		/// <returns>The runner.</returns>
		/// <param name="self">The runner.</param>
		/// <param name="backupfolder">The backup folder name.</param>
		/// <param name="csvfile">The CSV file with simulation results</param>
		public static Simulation BuildVHDL(this Simulation self, string backupfolder = null, string csvfile = "trace.csv")
		{
			self.AddPostloader((processes, target) =>
			{
				SME.Render.VHDL.Renderer.Render(processes, target, backupfolder, csvfile);
			});
			return self;
		}
	}
}

namespace SME.Render.VHDL
{
	/// <summary>
	/// Class for rendering the output as VHDL
	/// </summary>
	public static class Renderer
	{
		public static IDictionary<Type, string> TypeMap { get; private set; }

		private static void BackupExistingTarget(IEnumerable<IProcess> processes, string targetfolder, string backupfolder)
		{
			backupfolder = backupfolder ?? targetfolder;

			if (Directory.Exists(targetfolder))
			{
				var backupname = Path.Combine(Path.GetDirectoryName(backupfolder), Path.GetFileName(backupfolder) + "-backup-" + DateTime.Now.ToString("yyyyMMddTHHmmss"));
				while (Directory.Exists(backupname))
				{
					System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1.5));
					backupname = Path.Combine(Path.GetDirectoryName(backupfolder), Path.GetFileName(backupfolder) + "-backup-" + DateTime.Now.ToString("yyyyMMddTHHmmss"));
				}

				Directory.CreateDirectory(backupname);

				var s = Path.Combine(targetfolder, AssemblyNameToFileName(processes));
				if (File.Exists(s))
					File.Copy(s, Path.Combine(backupname, AssemblyNameToFileName(processes)));


				foreach (var p in processes)
				{
					var source = Path.Combine(targetfolder, ProcessNameToFileLName(p));
					if (File.Exists(source))
						File.Copy(source, Path.Combine(backupname, ProcessNameToFileLName(p)));
				}
			}
		}

		private static string MergeUserData(string text, string targetfile)
		{
			if (System.IO.File.Exists(targetfile))
			{
				var core = @"[ \t]*--[ \t]+####[ \t]+USER-DATA-{0}-{1}[ \t]*(\r|\r\n|\n)";

				var rxfind = new Regex(string.Format(core, "(?<groupname>[A-Z]+)", "START"));
				var inputgroups = (from n in rxfind.Matches(text).Cast<Match>() select n.Groups["groupname"].Value).ToArray();

				var targettext = File.ReadAllText(targetfile);
				if (string.IsNullOrWhiteSpace(targettext))
					return text;
				
				var targetgroups = (from n in rxfind.Matches(targettext).Cast<Match>() select n.Groups["groupname"].Value).ToArray();

				if (targetgroups.Length != inputgroups.Length)
					throw new Exception(string.Format("The file {0} does not match the template!", targetfile));

				if (targetgroups.Distinct().Count() != targetgroups.Length)
					throw new Exception(string.Format("The file {0} has duplicate user regions!", targetfile));

				if (inputgroups.Distinct().Count() != inputgroups.Length)
					throw new Exception(string.Format("The source template for {0} has duplicate user regions!", targetfile));
				
				if (!targetgroups.All(n => inputgroups.Contains(n)))
					throw new Exception(string.Format("The file {0} does not match the template!", targetfile));

				foreach (var g in inputgroups)
				{
					var rxsource = new Regex(string.Format("(?<startexp>" + core + ")", g, "START") + "(?<content>.*)" + string.Format("(?<endexp>" + core + ")", g, "END"), RegexOptions.Singleline);
					var sourcematch = rxsource.Matches(targettext);
					if (sourcematch == null)
						continue;
					
					if (sourcematch.Count > 1)
						throw new Exception("Multiple regions found in previous file for -- #### USER-DATA-{0}");

					if (sourcematch.Count > 0)
					{
						var userdata = sourcematch[0].Groups["content"].Value;

						text = rxsource.Replace(text, m => m.Groups["startexp"].Value + userdata + m.Groups["endexp"].Value);
					}
				}
			}

			return text;
		}

		public static void Render(IEnumerable<IProcess> processes, string targetfolder, string backupfolder = null, string csvtracename = null)
		{
			processes =
				processes.Where(
					n =>
						n.GetType().GetCustomAttributes(typeof(VHDLIgnoreAttribute), true).FirstOrDefault() == null
							&&
						!(n is SimulationProcess)
				);

			BackupExistingTarget(processes, targetfolder, backupfolder);

			var targetTopLevel = Path.Combine(targetfolder, AssemblyNameToFileName(processes));


			var info = new GlobalInformation(Converter.LoadType(processes.First().GetType()).Module);
			var all = new List<Converter>();

			foreach (var p in processes)
			{
				var c = new Converter(p, info);
				var targetfile = Path.Combine(targetfolder, ProcessNameToFileLName(p));
				File.WriteAllText(targetfile, MergeUserData(new Entity(c, p).TransformText(), targetfile));
				all.Add(c);
			}

			File.WriteAllText(targetTopLevel, MergeUserData(new TopLevel(info, all).TransformText(), targetTopLevel));

			var targetTestBench = Path.Combine(targetfolder, "TestBench-" + AssemblyNameToFileName(processes));
			File.WriteAllText(targetTestBench, MergeUserData(new TracefileTester(info, processes, CSVTracer.BuildPropertyMap(), csvtracename).TransformText(), targetTestBench));

			var targetTypeLib =  Path.Combine(targetfolder, "Types-" + AssemblyNameToFileName(processes));
			File.WriteAllText(targetTypeLib, MergeUserData(new CustomTypes(info).TransformText(), targetTypeLib));

			foreach (
				var vhdlfile in from f in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
				where 
					f.EndsWith(".vhdl", StringComparison.InvariantCultureIgnoreCase) 
					&& 
					f.StartsWith(typeof(Renderer).Namespace + ".", StringComparison.InvariantCultureIgnoreCase)
				select f)
				using (var rs = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(vhdlfile)))
					File.WriteAllText(Path.Combine(targetfolder, vhdlfile.Substring(typeof(Renderer).Namespace.Length + 1)), rs.ReadToEnd());
		}
			
		public static string AssemblyNameToFileName(IEnumerable<IProcess> processes)
		{
			return processes.First().GetType().Assembly.GetName().Name + ".vhdl";
		}

		public static string ProcessNameToFileLName(IProcess process)
		{
			return ProcessNameToVHDLName(process) + ".vhdl";
		}

		public static string ProcessNameToVHDLName(IProcess process)
		{
			var processname = process.GetType().FullName;
			var asmname = process.GetType().Assembly.GetName().Name + '.';
			if (processname.StartsWith(asmname))
				processname = processname.Substring(asmname.Length);

			return ConvertToValidVHDLName(processname);
		}
			
		public static IEnumerable<IBus> InputOnlyBusses(IProcess process)
		{
			return 
				from n in process.InputBusses.Union(process.ClockedInputBusses)
				where !process.OutputBusses.Contains(n)
				select n; 
				
		}

		public static IEnumerable<IBus> OutputOnlyBusses(IProcess process)
		{
			return 
				from n in process.OutputBusses
				where !process.InputBusses.Union(process.ClockedInputBusses).Contains(n)
				select n; 
		}

		public static IEnumerable<IBus> InputOutputBusses(IProcess process)
		{
			return 
				from n in process.InputBusses.Union(process.ClockedInputBusses)
				where process.OutputBusses.Contains(n)
				select n; 
		}

		public static IEnumerable<IBus> InternalBusses(IProcess process)
		{
			return process.InternalBusses;
		}

		public static IEnumerable<IBus> ClockedBusses(IEnumerable<IProcess> processes)
		{
			return processes
				.SelectMany(x => x.InputBusses.Union(x.OutputBusses).Union(x.ClockedInputBusses))
				.Distinct()
				.Where(x => IsClockedBus(x));
		}

		public static bool IsClockedBus(IBus bus)
		{
			return bus.BusType.GetCustomAttributes(typeof(ClockedBusAttribute), true).FirstOrDefault() != null;
		}

		public static string BusSignalNameToVHDLName(IProcess process, System.Reflection.PropertyInfo pi)
		{
			if (process != null && pi.DeclaringType.DeclaringType == process.GetType())
				return ConvertToValidVHDLName(pi.DeclaringType.Name + '_' + pi.Name);

			var busname = pi.DeclaringType.FullName + '_' + pi.Name;
			var asmname = (process == null ? pi.DeclaringType : process.GetType()).Assembly.GetName().Name + '.';
			if (busname.StartsWith(asmname))
				busname = busname.Substring(asmname.Length);
			
			return ConvertToValidVHDLName(busname);
		}
			

		public static string AssemblyNameToVHDLName(IEnumerable<IProcess> processes)
		{
			return ConvertToValidVHDLName(processes.First().GetType().Assembly.GetName().Name);
		}

		private static Regex RX_ALPHANUMERIC = new Regex(@"[^\u0030-\u0039|\u0041-\u005A|\u0061-\u007A]");

		public static string ConvertToValidVHDLName(string name)
		{
			var  r = RX_ALPHANUMERIC.Replace(name, "_");
			if (new string[] {"register", "record", "variable", "process", "if", "then", "else", "begin", "end", "architecture", "of", "is"}.Contains(r.ToLowerInvariant()))
				r = "vhdl_" + r;
			return r;
		}

	}
}

