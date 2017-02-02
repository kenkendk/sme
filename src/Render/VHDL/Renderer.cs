using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SME.Render.VHDL.ILConvert;
using SME.Render.Transpiler;
using SME.Render.Transpiler.ILConvert;
using Mono.Cecil;
using ICSharpCode.NRefactory.CSharp;

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

		private static readonly ILConvert.VHDLGlobalInformation m_info = new VHDLGlobalInformation();

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

				var s = Path.Combine(targetfolder, m_info.AssemblyNameToFileName(processes));
				if (File.Exists(s))
					File.Copy(s, Path.Combine(backupname, m_info.AssemblyNameToFileName(processes)));


				foreach (var p in processes)
				{
					var source = Path.Combine(targetfolder, m_info.ProcessNameToFileName(p));
					if (File.Exists(source))
						File.Copy(source, Path.Combine(backupname, m_info.ProcessNameToFileName(p)));
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
						n.GetType().GetCustomAttributes(typeof(IgnoreAttribute), true).FirstOrDefault() == null
							&&
						!(n is SimulationProcess)
				);

			BackupExistingTarget(processes, targetfolder, backupfolder);

			var targetTopLevel = Path.Combine(targetfolder, m_info.AssemblyNameToFileName(processes));


			var info = new VHDLGlobalInformation(VHDLConverter.LoadType(processes.First().GetType()).Module);
			var all = new List<VHDLConverter>();

			foreach (var p in processes)
			{
				var c = new VHDLConverter(p, info);
				var targetfile = Path.Combine(targetfolder, m_info.ProcessNameToFileName(p));
				File.WriteAllText(targetfile, MergeUserData(new Entity(c, p).TransformText(), targetfile));
				all.Add(c);
			}

			File.WriteAllText(targetTopLevel, MergeUserData(new TopLevel(info, all).TransformText(), targetTopLevel));

			var targetTestBench = Path.Combine(targetfolder, "TestBench-" + m_info.AssemblyNameToFileName(processes));
			File.WriteAllText(targetTestBench, MergeUserData(new TracefileTester(info, processes, Transpiler.CSVTracer.BuildPropertyMap(), csvtracename).TransformText(), targetTestBench));

			var targetTypeLib =  Path.Combine(targetfolder, "Types-" + m_info.AssemblyNameToFileName(processes));
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
			

			
		public static IEnumerable<IBus> InputOnlyBusses(IProcess process)
		{
			return SME.Render.Transpiler.Renderer.InputOnlyBusses(process);
		}

		public static IEnumerable<IBus> OutputOnlyBusses(IProcess process)
		{
			return SME.Render.Transpiler.Renderer.OutputOnlyBusses(process);
		}

		public static IEnumerable<IBus> InputOutputBusses(IProcess process)
		{
			return SME.Render.Transpiler.Renderer.InputOutputBusses(process);
		}

		public static IEnumerable<IBus> InternalBusses(IProcess process)
		{
			return SME.Render.Transpiler.Renderer.InternalBusses(process);
		}

		public static IEnumerable<IBus> ClockedBusses(IEnumerable<IProcess> processes)
		{
			return SME.Render.Transpiler.Renderer.ClockedBusses(processes);
		}

		public static bool IsClockedBus(IBus bus)
		{
			return SME.Render.Transpiler.Renderer.IsClockedBus(bus);
		}

		public static string ConvertToValidVHDLName(string name)
		{
			return m_info.ToValidName(name);
		}

		public static string ToVHDLName(this MemberItem self, TypeDefinition scope, Expression target)
		{
			return self.Item.ToValidName(m_info, scope, target, self.DeclaringType);
		}


	}
}

