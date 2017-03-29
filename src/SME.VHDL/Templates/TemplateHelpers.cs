using System;
using System.Collections.Generic;
using System.Linq;
using SME.AST;

namespace SME.VHDL.Templates
{
	public partial class TopLevel
	{
		public readonly Network Network;
		public readonly RenderState RS;

		public TopLevel(RenderState renderer)
		{
			RS = renderer;
			Network = renderer.Network;
		}
	}

	public partial class ExportTopLevel
	{
		public readonly Network Network;
		public readonly RenderState RS;

		public ExportTopLevel(RenderState renderer)
		{
			RS = renderer;
			Network = renderer.Network;
		}
	}

	public partial class CustomTypes
	{
		public readonly Network Network;
		public readonly RenderState RS;

		public CustomTypes(RenderState renderer)
		{
			RS = renderer;
			Network = renderer.Network;
		}
	}

	public partial class TracefileTester
	{
		public readonly Network Network;
		public readonly RenderState RS;

		public TracefileTester(RenderState renderer)
		{
			RS = renderer;
			Network = renderer.Network;
		}
	}

	public partial class Entity
	{
		public readonly RenderState RS;
		public readonly RenderStateProcess RSP;

		public readonly Network Network;
		public readonly AST.Process Process;

		public Entity(RenderState renderer, RenderStateProcess renderproc)
		{
			RS = renderer;
			RSP = renderproc;
			Network = renderer.Network;
			Process = renderproc.Process;
		}
	}

	public partial class GHDL_Makefile
	{
		public readonly RenderState RS;
		public GHDL_Makefile(RenderState renderer)
		{
			RS = renderer;
		}

		public IEnumerable<string> CustomFiles
		{
			get
			{
				return RS.CustomFiles;
			}
		}

		public IEnumerable<string> Filenames
		{
			get
			{
				foreach (var p in RS.Network.Processes)
					yield return Naming.ProcessNameToValidName(p.SourceInstance);

				foreach (var p in RawVHDL)
					yield return p;
			}
		}

		public IEnumerable<string> RawVHDL
		{
			get
			{
				var prefix = typeof(Templates.TopLevel).Namespace + ".";
				return
					System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
						  .Where(x => x.EndsWith(".vhdl", StringComparison.InvariantCultureIgnoreCase))
						  .Where(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
						  .Select(x => x.Substring(prefix.Length))
						  .Where(x => x != "system_types.vhdl")
					      .Select(x => x.Substring(0, x.Length - ".vhdl".Length));
			}
		}
	}
}
