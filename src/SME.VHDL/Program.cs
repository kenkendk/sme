using System;
using System.IO;
using CommandLine;

namespace SME.VHDL
{
	class MainClass
	{
		private class Options
		{
			[ValueOption(0)]
			[Option(/*0, MetaName="assemblypath",*/ MetaValue="DLLPATH", HelpText="The path to the assembly to generate VHDL for")]
			public string AssemblyPath { get; set; }

			[ValueOption(1)]
			[Option(/*1, MetaName="targetfolder",*/ MetaValue="FOLDERPATH", HelpText="The path to the folder where VHDL output is generated")]
			public string TargetFolder { get; set; }

			[Option(HelpText="Path to a folder where backups of the VHDL files are stored, to avoid loosing information with overwrites")]
			public string BackupFolder { get; set; }

			[Option(DefaultValue="TestData.csv", HelpText="The string to insert into the testbench, indicating what trace file to load")]
			public string CSVPath { get; set; }
		}

		public static int Main(string[] args)
		{
			var options = new Options();

			if (!CommandLine.Parser.Default.ParseArguments(args, options))
				return 1;

			if (!File.Exists(options.AssemblyPath))
			{
				Console.WriteLine("File not found: {0}", options.AssemblyPath);
				return 1;
			}

			var asm = System.Reflection.Assembly.UnsafeLoadFrom(options.AssemblyPath);
			new RenderState(Loader.LoadAssemblies(asm), options.TargetFolder, options.BackupFolder, options.CSVPath).Render();			
			return 0;
		}
	}
}
