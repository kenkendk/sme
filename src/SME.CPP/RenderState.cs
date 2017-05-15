using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using SME.AST;

namespace SME.CPP
{
	public class RenderState
	{

		/// <summary>
		/// A type reference comparer, used to compare type references loaded from different contexts
		/// </summary>
		private class TypeRefComp : IEqualityComparer<TypeReference>
		{
			/// <summary>
			/// Returns a value indicating if x is equal to y.
			/// </summary>
			/// <returns><c>True</c> if x is equal to y, <c>false</c> otherwise.</returns>
			/// <param name="x">The x value.</param>
			/// <param name="y">The y value.</param>
			public bool Equals(TypeReference x, TypeReference y)
			{ return x.FullName == y.FullName; }

			/// <summary>
			/// Gets the hash code of an object.
			/// </summary>
			/// <returns>The hash code.</returns>
			/// <param name="obj">The item to get the hash code for.</param>
			public int GetHashCode(TypeReference obj)
			{ return obj.FullName.GetHashCode(); }
		}

		/// <summary>
		/// The network being rendered
		/// </summary>
		public readonly Network Network;

		/// <summary>
		/// The processes forming the basis of the network
		/// </summary>
		public readonly IEnumerable<IProcess> Processes;

		/// <summary>
		/// The folder where data is place
		/// </summary>
		public readonly string TargetFolder;
		/// <summary>
		/// The folder where backups are stored
		/// </summary>
		public readonly string BackupFolder;
		/// <summary>
		/// The name of the file where a CSV trace is stored
		/// </summary>
		public readonly string CSVTracename;

		/// <summary>
		/// Sequence of custom CPP files to include in the compilation
		/// </summary>
		public readonly IEnumerable<string> CustomFiles;

		/// <summary>
		/// The unique types found in the network
		/// </summary>
		public readonly TypeReference[] Types;

		/// <summary>
		/// The type scope
		/// </summary>
		public readonly CppTypeScope TypeScope;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.VHDL.RenderState"/> class.
		/// </summary>
		/// <param name="processes">The processes to parse.</param>
		/// <param name="targetfolder">The folder where the output is stored.</param>
		/// <param name="backupfolder">The folder where backups are stored.</param>
		/// <param name="csvtracename">The name of the CSV trace file.</param>
		/// <param name="customfiles">A list of VHDL files to include in the Makefile, without the VHDL extension</param>
		public RenderState(IEnumerable<IProcess> processes, string targetfolder, string backupfolder = null, string csvtracename = null, IEnumerable<string> customfiles = null)
		{
			Processes = processes;
			TargetFolder = targetfolder;
			BackupFolder = backupfolder;
			CSVTracename = csvtracename;
			CustomFiles = customfiles;

			Network = ParseProcesses.BuildNetwork(processes, true);

			ValidateNetwork(Network);

			TypeScope = new CppTypeScope(Network.Processes.First(x => x.MainMethod != null).MainMethod.SourceMethod.Module);

			Types = Network
				.All()
				.OfType<DataElement>()
				.Select(x => x.CecilType)
				.Distinct(new TypeRefComp())
				.ToArray();

			Network.Name = Naming.ToValidName(Processes.First().GetType().Assembly.GetName().Name);

            SME.AST.Transform.Apply.Transform(
                Network,
                new SME.AST.Transform.IASTTransform[] {
                    new Transformations.AssignNames(),
                },
                m => new SME.AST.Transform.IASTTransform[] {

                },
				m => new SME.AST.Transform.IASTTransform[] {
					new SME.AST.Transform.WrapIfComposite()
			    },
				m => new SME.AST.Transform.IASTTransform[] {
                    new SME.AST.Transform.RemoveExtraParenthesis()
				}
            );
		}

		/// <summary>
		/// Render this instance.
		/// </summary>
		public void Render()
		{
            BackupExistingTarget(TargetFolder, BackupFolder);

			var targetTopLevel = Path.Combine(TargetFolder, Naming.AssemblyNameToFileName(Network));
			File.WriteAllText(targetTopLevel, MergeUserData(new Templates.TopLevel(this).TransformText(), targetTopLevel));

            var targetDefinitions = Path.Combine(TargetFolder, Naming.DefinitionsFileName(Network));
            File.WriteAllText(targetDefinitions, MergeUserData(new Templates.SharedDefinitions(this).TransformText(), targetDefinitions));

			var makeFileTarget = Path.Combine(TargetFolder, "Makefile");
            File.WriteAllText(makeFileTarget, MergeUserData(new Templates.Makefile(this).TransformText(), makeFileTarget));

			foreach (var p in Network.Processes)
			{
				var rsp = new RenderStateProcess(this, p);
				var targetheaderfile = Path.Combine(TargetFolder, p.Name + ".hpp");
                File.WriteAllText(targetheaderfile, MergeUserData(new Templates.ProcessHeader(this, rsp).TransformText(), targetheaderfile));

				var targetfile = Path.Combine(TargetFolder, Naming.ProcessNameToFileName(p));
				File.WriteAllText(targetfile, MergeUserData(new Templates.ProcessItem(this, rsp).TransformText(), targetfile));
			}

            var names = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

			foreach (
				var cppfile in from f in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
								where
								(
									f.EndsWith(".cpp", StringComparison.InvariantCultureIgnoreCase) 
									||
								 	f.EndsWith(".hpp", StringComparison.InvariantCultureIgnoreCase) 
								)
								&&
								f.StartsWith(typeof(Templates.TopLevel).Namespace + ".", StringComparison.InvariantCultureIgnoreCase)
								select f)
				using (var rs = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(cppfile)))
					File.WriteAllText(Path.Combine(TargetFolder, cppfile.Substring(typeof(Templates.TopLevel).Namespace.Length + 1)), rs.ReadToEnd());
			
		}
			

		/// <summary>
		/// Performs some checks to see if the network uses features that are not supported by the CPP render
		/// </summary>
		/// <param name="network">The network to validate.</param>
		private static void ValidateNetwork(AST.Network network)
		{
			var sp = network.Processes.SelectMany(x => x.InternalBusses).FirstOrDefault(x => x.IsTopLevelInput || x.IsTopLevelOutput);
			if (sp != null)
				throw new Exception($"Cannot have an internal bus that is also toplevel input or output: {sp.Name}");
		}

		/// <summary>
		/// Makes a backup of all target files
		/// </summary>
		/// <param name="targetfolder">The folder where output is stored.</param>
		/// <param name="backupfolder">The folder where backups are stored.</param>
		private void BackupExistingTarget(string targetfolder, string backupfolder)
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

				var s = Path.Combine(targetfolder, Naming.AssemblyNameToFileName(Network));
				if (File.Exists(s))
					File.Copy(s, Path.Combine(backupname, Naming.AssemblyNameToFileName(Network)));

				s = Path.Combine(targetfolder, Naming.DefinitionsFileName(Network));
				if (File.Exists(s))
					File.Copy(s, Path.Combine(backupname, Naming.DefinitionsFileName(Network)));

				foreach (var p in Network.Processes)
				{
					var source = Path.Combine(targetfolder, Naming.ProcessNameToFileName(p));
					if (File.Exists(source))
						File.Copy(source, Path.Combine(backupname, Naming.ProcessNameToFileName(p)));
				}
			}
			else
			{
				Directory.CreateDirectory(targetfolder);
			}
		}

		/// <summary>
		/// Returns the name written in the trace file for a given signal
		/// </summary>
		/// <returns>The signal name as written by the tracer.</returns>
		/// <param name="s">The signal to find the name for.</param>
		public string TestBenchSignalName(BusSignal s)
		{
			var bus = (AST.Bus)s.Parent;
			var st = bus.SourceType;
			var name = st.Name + "." + s.Name;
			if (st.DeclaringType != null)
				name = st.DeclaringType.Name + "." + name;

			return name;
		}

		/// <summary>
		/// Returns all signals from top-level input busses
		/// </summary>
		public IEnumerable<BusSignal> DriverSignals
		{
			get
			{
				return Network.Busses
							  .Where(x => x.IsTopLevelInput)
							  .SelectMany(x => x.Signals)
							  .OrderBy(x => TestBenchSignalName(x))
							  .SelectMany(x => SplitArray(x));
			}
		}

		/// <summary>
		/// Returns all signals from non-top-level input busses
		/// </summary>
		public IEnumerable<BusSignal> VerifySignals
		{
			get
			{
				return Network.Busses
							  .Where(x => !x.IsTopLevelInput && !x.IsInternal)
							  .SelectMany(x => x.Signals)
							  .OrderBy(x => TestBenchSignalName(x))
							  .SelectMany(x => SplitArray(x));
			}
		}

		/// <summary>
		/// Splits a signal into a signal for each array element.
		/// </summary>
		/// <returns>The array signals.</returns>
		/// <param name="signal">The signal to split.</param>
		private IEnumerable<BusSignal> SplitArray(BusSignal signal)
		{
			if (!signal.CecilType.IsArrayType())
			{
				yield return signal;
			}
			else
			{
				var attr = (signal.Source as System.Reflection.MemberInfo).GetCustomAttributes(typeof(FixedArrayLengthAttribute), true).FirstOrDefault() as FixedArrayLengthAttribute;
				if (attr == null)
					throw new Exception($"Expected an array length on {signal.Name} ({signal.Source})");

				for (var i = 0; i < attr.Length; i++)
					yield return new BusSignal()
					{
						CecilType = signal.CecilType.GetArrayElementType(),
						Parent = signal.Parent,
						Name = string.Format("{0}({1})", signal.Name, i)
					};
			}
		}

		/// <summary>
		/// Merges a newly rendered file with user supplied data from an existing file
		/// </summary>
		/// <returns>The user data.</returns>
		/// <param name="text">The newly rendered content.</param>
		/// <param name="targetfile">The file to write.</param>
		private string MergeUserData(string text, string targetfile)
		{
			if (File.Exists(targetfile))
			{
				var core = @"[ \t]*//[ \t]+####[ \t]+USER-DATA-{0}-{1}[ \t]*(\r|\r\n|\n)";

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

	}
}
