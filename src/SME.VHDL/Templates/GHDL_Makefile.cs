using System.Linq;
using System.Text;
using System.Collections.Generic;
using System;

namespace SME.VHDL.Templates
{

    public class GHDL_Makefile : BaseTemplate
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
                foreach (var p in RS.Network.Processes.Select(x => x.SourceType).Distinct())
                    yield return Naming.ProcessNameToValidName(p);

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

        public override string TransformText()
        {
            GenerationEnvironment = null;

            var network = ToStringHelper.ToStringWithCulture( RS.Network.Name );
            var networklower = ToStringHelper.ToStringWithCulture( RS.Network.Name.ToLower() );

            Write($"all: test export\n");
            Write($"testbench: {networklower}_tb\n");
            Write($"export: {network}_export\n");
            Write($"build: export testbench\n");
            Write(@"
# Use a temporary folder for compiled stuff
WORKDIR=work

# All code should be VHDL93 compliant,
# but 93c is a bit easier to work with
STD=93c

# Eveything should compile with clean IEEE,
# but the test-bench and CSV util's require
# std_logic_textio from Synopsys
IEEE=synopsys

# VCD trace file for GTKWave
VCDFILE=trace.vcd

# Disable the 'Warning: redundant others'
FLAGS=--warn-no-others

");

            var cust_tag = CustomFiles == null || CustomFiles.Count() == 0 ? "" : " custom_files";

            if (!string.IsNullOrEmpty(cust_tag))
            {
                Write("custom_files: $(WORKDIR) ");
                foreach(var file in CustomFiles)
                {
                    var filename = ToStringHelper.ToStringWithCulture( file );
                    Write($"$(WORKDIR)/{filename}.o ");
                }
                Write("\n");
            }

            Write($"$(WORKDIR):\n");
            Write($"\tmkdir $(WORKDIR)\n");

            Write($"\n");

            Write($"$(WORKDIR)/system_types.o: system_types.vhdl $(WORKDIR)\n");
            Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) system_types.vhdl\n");

            Write($"\n");

            Write($"$(WORKDIR)/Types_{network}.o: Types_{network}.vhdl $(WORKDIR)\n");
            Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) Types_{network}.vhdl\n");

            Write("\n");

            foreach (var file in Filenames)
            {
                var filename = ToStringHelper.ToStringWithCulture( file );
                var tag = ToStringHelper.ToStringWithCulture( cust_tag );
                Write($"$(WORKDIR)/{filename}.o: {filename}.vhdl $(WORKDIR)/system_types.o $(WORKDIR)/Types_{network}.o $(WORKDIR){tag}\n");
                Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) {filename}.vhdl\n");
                Write("\n");
            }

            if (!string.IsNullOrEmpty(cust_tag))
            {
                foreach (var file in CustomFiles)
                {
                    var filename = ToStringHelper.ToStringWithCulture( file );
                    Write($"$(WORKDIR)/{filename}.o: {filename}.vhdl $(WORKDIR)/system_types.o $(WORKDIR)/Types_{network}.o $(WORKDIR)\n");
                    Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) {filename}.vhdl\n");
                    Write("\n");
                }
                Write("\n");
            }

            var files = Filenames
                .Select(x => {
                    var filename = ToStringHelper.ToStringWithCulture( x );
                    return $"$(WORKDIR)/{filename}.o";
                });
            var filess = string.Join(" ", files);
            var cust = ToStringHelper.ToStringWithCulture( cust_tag );
            Write($"$(WORKDIR)/{network}.o: {network}.vhdl $(WORKDIR)/system_types.o $(WORKDIR)/Types_{network}.o {filess} {cust}\n");
            Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) {network}.vhdl\n");
            Write("\n");

            Write($"$(WORKDIR)/TestBench_{network}.o: TestBench_{network}.vhdl $(WORKDIR)/{network}.o\n");
            Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) TestBench_{network}.vhdl\n");
            Write("\n");

            Write($"{networklower}_tb: $(WORKDIR)/TestBench_{network}.o\n");
            Write($"\tghdl -e --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) {network}_tb\n");
            Write("\n");

            Write($"{network}_export: $(WORKDIR)/{network}.o\n");
            Write($"\tghdl -a --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) Export_{network}.vhdl\n");
            Write("\n");

            var csv = ToStringHelper.ToStringWithCulture( RS.CSVTracename );
            Write($"test: {networklower}_tb\n");
            Write($"\tcp \"{csv}\" .\n");
            Write($"\tghdl -r --std=$(STD) --ieee=$(IEEE) --workdir=$(WORKDIR) $(FLAGS) {network}_tb --vcd=$(VCDFILE)\n");
            Write("\n");

            Write("clean:\n");
            Write($"\trm -rf $(WORKDIR) *.o {networklower}_tb\n");
            Write("\n");

            Write($".PHONY: all clean test export build {cust}\n");

            return GenerationEnvironment.ToString();
        }
    }
}
