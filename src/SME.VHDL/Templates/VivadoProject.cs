﻿using System;
using SME.AST;

namespace SME.VHDL.Templates
{
    /// <summary>
    /// Template for generating a Xilinx Vivado project file.
    /// </summary>
    public partial class VivadoProject : BaseTemplate
    {
        /// <summary>
        /// The network to render.
        /// </summary>
        public readonly Network Network;
        /// <summary>
        /// The current render state.
        /// </summary>
        public readonly RenderState RS;
        /// <summary>
        /// The simulation to render.
        /// </summary>
        public readonly Simulation Simulation;
        /// <summary>
        /// The runtime for the simulator.
        /// </summary>
        public readonly string Runtime;
        /// <summary>
        /// The processes in the network.
        /// </summary>
        public readonly AST.Process[] Processes;

        /// <summary>
        /// Constructs a new instance of the Xilinx Vivado project template.
        /// </summary>
        /// <param name="renderer">The render state to render in.</param>
        /// <param name="processes">The processes to include in the project.</param>
        public VivadoProject(RenderState renderer, AST.Process[] processes)
        {
            RS = renderer;
            Network = renderer.Network;
            Simulation = renderer.Simulation;
            Runtime = ((Simulation.Tick + 2) * 10) + "ns";
            Processes = processes;
        }

        /// <summary>
        /// Writes the template to the output file.
        /// </summary>
        public override string TransformText()
        {
            GenerationEnvironment = null;

            var networkname = ToStringHelper.ToStringWithCulture( Network.Name );

            Write($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            Write($"<Project Version=\"7\" Minor=\"35\" Path=\"./{networkname}.xpr\">\n");
            Write(
@"    <DefaultLaunch Dir=""$PRUNDIR""/>
    <Configuration>
        <Option Name=""Id"" Val=""da04b7443593460ab7943c9e399803cf""/>
        <Option Name=""Part"" Val=""xc7z020clg484-1""/>
        <Option Name=""CompiledLibDir"" Val=""$PCACHEDIR/compile_simlib""/>
        <Option Name=""CompiledLibDirXSim"" Val=""""/>
        <Option Name=""CompiledLibDirModelSim"" Val=""$PCACHEDIR/compile_simlib/modelsim""/>
        <Option Name=""CompiledLibDirQuesta"" Val=""$PCACHEDIR/compile_simlib/questa""/>
        <Option Name=""CompiledLibDirIES"" Val=""$PCACHEDIR/compile_simlib/ies""/>
        <Option Name=""CompiledLibDirXcelium"" Val=""$PCACHEDIR/compile_simlib/xcelium""/>
        <Option Name=""CompiledLibDirVCS"" Val=""$PCACHEDIR/compile_simlib/vcs""/>
        <Option Name=""CompiledLibDirRiviera"" Val=""$PCACHEDIR/compile_simlib/riviera""/>
        <Option Name=""CompiledLibDirActivehdl"" Val=""$PCACHEDIR/compile_simlib/activehdl""/>
        <Option Name=""TargetLanguage"" Val=""VHDL""/>
        <Option Name=""SimulatorLanguage"" Val=""VHDL""/>
        <Option Name=""BoardPart"" Val=""em.avnet.com:zed:part0:1.3""/>
        <Option Name=""ActiveSimSet"" Val=""sim_1""/>
        <Option Name=""DefaultLib"" Val=""xil_defaultlib""/>
        <Option Name=""ProjectType"" Val=""Default""/>
        <Option Name=""IPOutputRepo"" Val=""$PCACHEDIR/ip""/>
        <Option Name=""IPCachePermission"" Val=""read""/>
        <Option Name=""IPCachePermission"" Val=""write""/>
        <Option Name=""EnableCoreContainer"" Val=""FALSE""/>
        <Option Name=""CreateRefXciForCoreContainers"" Val=""FALSE""/>
        <Option Name=""IPUserFilesDir"" Val=""$PIPUSERFILESDIR""/>
        <Option Name=""IPStaticSourceDir"" Val=""$PIPUSERFILESDIR/ipstatic""/>
        <Option Name=""EnableBDX"" Val=""FALSE""/>
        <Option Name=""DSAVendor"" Val=""xilinx""/>
        <Option Name=""DSABoardId"" Val=""zed""/>
        <Option Name=""DSANumComputeUnits"" Val=""16""/>
        <Option Name=""WTXSimLaunchSim"" Val=""84""/>
        <Option Name=""WTModelSimLaunchSim"" Val=""0""/>
        <Option Name=""WTQuestaLaunchSim"" Val=""0""/>
        <Option Name=""WTIesLaunchSim"" Val=""0""/>
        <Option Name=""WTVcsLaunchSim"" Val=""0""/>
        <Option Name=""WTRivieraLaunchSim"" Val=""0""/>
        <Option Name=""WTActivehdlLaunchSim"" Val=""0""/>
        <Option Name=""WTXSimExportSim"" Val=""0""/>
        <Option Name=""WTModelSimExportSim"" Val=""0""/>
        <Option Name=""WTQuestaExportSim"" Val=""0""/>
        <Option Name=""WTIesExportSim"" Val=""0""/>
        <Option Name=""WTVcsExportSim"" Val=""0""/>
        <Option Name=""WTRivieraExportSim"" Val=""0""/>
        <Option Name=""WTActivehdlExportSim"" Val=""0""/>
        <Option Name=""GenerateIPUpgradeLog"" Val=""TRUE""/>
        <Option Name=""XSimRadix"" Val=""hex""/>
        <Option Name=""XSimTimeUnit"" Val=""ns""/>
        <Option Name=""XSimArrayDisplayLimit"" Val=""1024""/>
        <Option Name=""XSimTraceLimit"" Val=""65536""/>
        <Option Name=""SimTypes"" Val=""rtl""/>
    </Configuration>
    <FileSets Version=""1"" Minor=""31"">
        <FileSet Name=""sources_1"" Type=""DesignSrcs"" RelSrcDir=""$PSRCDIR/sources_1"">
            <Filter Type=""Srcs""/>
            <File Path=""$PPRDIR/system_types.vhdl"">
                <FileInfo>
                    <Attr Name=""Library"" Val=""xil_defaultlib""/>
                    <Attr Name=""IsGlobalInclude"" Val=""1""/>
                    <Attr Name=""UsedIn"" Val=""synthesis""/>
                    <Attr Name=""UsedIn"" Val=""simulation""/>
                </FileInfo>
            </File>
");
            var simname = ToStringHelper.ToStringWithCulture( Naming.AssemblyNameToFileName() );
            Write($"            <File Path=\"$PPRDIR/Types_{simname}\">\n");
            Write(
@"                <FileInfo>
                    <Attr Name=""Library"" Val=""xil_defaultlib""/>
                    <Attr Name=""IsGlobalInclude"" Val=""1""/>
                    <Attr Name=""UsedIn"" Val=""synthesis""/>
                    <Attr Name=""UsedIn"" Val=""simulation""/>
                </FileInfo>
            </File>
");

            foreach (var p in Processes)
            {
                var procname = ToStringHelper.ToStringWithCulture( Naming.ProcessNameToFileName(p.SourceInstance.Instance) );
                Write($"            <File Path=\"$PPRDIR/{procname}\">\n");
                Write(
@"                <FileInfo>
                    <Attr Name=""Library"" Val=""xil_defaultlib""/>
                    <Attr Name=""UsedIn"" Val=""synthesis""/>
                    <Attr Name=""UsedIn"" Val=""simulation""/>
                </FileInfo>
            </File>
");
            }

            Write($"            <File Path=\"$PPRDIR/{simname}\">\n");
            Write(
@"                <FileInfo>
                    <Attr Name=""Library"" Val=""xil_defaultlib""/>
                    <Attr Name=""UsedIn"" Val=""synthesis""/>
                    <Attr Name=""UsedIn"" Val=""simulation""/>
                </FileInfo>
            </File>
            <Config>
                <Option Name=""DesignMode"" Val=""RTL""/>
");
            Write($"                <Option Name=\"TopModule\" Val=\"{networkname}\"/>\n");
            Write(
@"                <Option Name=""TopAutoSet"" Val=""TRUE""/>
            </Config>
        </FileSet>
        <FileSet Name=""constrs_1"" Type=""Constrs"" RelSrcDir=""$PSRCDIR/constrs_1"">
            <Filter Type=""Constrs""/>
            <Config>
                <Option Name=""ConstrsType"" Val=""XDC""/>
            </Config>
        </FileSet>
        <FileSet Name=""sim_1"" Type=""SimulationSrcs"" RelSrcDir=""$PSRCDIR/sim_1"">
            <Filter Type=""Srcs""/>
            <File Path=""$PPRDIR/csv_util.vhdl"">
                <FileInfo>
                    <Attr Name=""Library"" Val=""xil_defaultlib""/>
                    <Attr Name=""UsedIn"" Val=""synthesis""/>
                    <Attr Name=""UsedIn"" Val=""simulation""/>
                </FileInfo>
            </File>
");
            Write($"            <File Path=\"$PPRDIR/TestBench_{simname}\">\n");
            Write(
@"                <FileInfo>
                    <Attr Name=""Library"" Val=""xil_defaultlib""/>
                    <Attr Name=""UsedIn"" Val=""synthesis""/>
                    <Attr Name=""UsedIn"" Val=""simulation""/>
                </FileInfo>
            </File>
");
            var csvfile = ToStringHelper.ToStringWithCulture( RS.CSVTracename );
            Write($"            <File Path=\"$PPRDIR/{csvfile}\">\n");

            Write(
@"                <Attr Name=""UsedIn"" Val=""simulation""/>
            </File>
            <Config>
                <Option Name=""DesignMode"" Val=""RTL""/>
");
            Write($"                <Option Name=\"TopModule\" Val=\"{networkname}_tb\"/>\n");
            Write(
@"                <Option Name=""TopLib"" Val=""xil_defaultlib""/>
                <Option Name=""TransportPathDelay"" Val=""0""/>
                <Option Name=""TransportIntDelay"" Val=""0""/>
                <Option Name=""SrcSet"" Val=""sources_1""/>
");
            var runtime = ToStringHelper.ToStringWithCulture( Runtime );
            Write($"                <Option Name=\"xsim.simulate.runtime\" Val=\"{runtime}\"/>\n");
            Write(
@"            </Config>
        </FileSet>
    </FileSets>
    <Simulators>
        <Simulator Name=""XSim"">
            <Option Name=""Description"" Val=""Vivado Simulator""/>
            <Option Name=""CompiledLib"" Val=""0""/>
        </Simulator>
        <Simulator Name=""ModelSim"">
            <Option Name=""Description"" Val=""ModelSim Simulator""/>
        </Simulator>
        <Simulator Name=""Questa"">
            <Option Name=""Description"" Val=""Questa Advanced Simulator""/>
        </Simulator>
        <Simulator Name=""Riviera"">
            <Option Name=""Description"" Val=""Riviera-PRO Simulator""/>
        </Simulator>
        <Simulator Name=""ActiveHDL"">
            <Option Name=""Description"" Val=""Active-HDL Simulator""/>
        </Simulator>
    </Simulators>
    <Runs Version=""1"" Minor=""10"">
        <Run Id=""synth_1"" Type=""Ft3:Synth"" SrcSet=""sources_1"" Part=""xc7z020clg484-1"" ConstrsSet=""constrs_1"" Description=""Vivado Synthesis Defaults"" WriteIncrSynthDcp=""false"" State=""current"" IncludeInArchive=""true"">
            <Strategy Version=""1"" Minor=""2"">
                <StratHandle Name=""Vivado Synthesis Defaults"" Flow=""Vivado Synthesis 2017""/>
                <Step Id=""synth_design""/>
            </Strategy>
            <ReportStrategy Name=""Vivado Synthesis Default Reports"" Flow=""Vivado Synthesis 2017""/>
            <Report Name=""ROUTE_DESIGN.REPORT_METHODOLOGY"" Enabled=""1""/>
        </Run>
        <Run Id=""impl_1"" Type=""Ft2:EntireDesign"" Part=""xc7z020clg484-1"" ConstrsSet=""constrs_1"" Description=""Default settings for Implementation."" WriteIncrSynthDcp=""false"" State=""current"" SynthRun=""synth_1"" IncludeInArchive=""true"">
            <Strategy Version=""1"" Minor=""2"">
                <StratHandle Name=""Vivado Implementation Defaults"" Flow=""Vivado Implementation 2017""/>
                <Step Id=""init_design""/>
                <Step Id=""opt_design""/>
                <Step Id=""power_opt_design""/>
                <Step Id=""place_design""/>
                <Step Id=""post_place_power_opt_design""/>
                <Step Id=""phys_opt_design""/>
                <Step Id=""route_design""/>
                <Step Id=""post_route_phys_opt_design""/>
                <Step Id=""write_bitstream""/>
            </Strategy>
            <ReportStrategy Name=""Vivado Implementation Default Reports"" Flow=""Vivado Implementation 2017""/>
            <Report Name=""ROUTE_DESIGN.REPORT_METHODOLOGY"" Enabled=""1""/>
        </Run>
    </Runs>
    <Board>
        <Jumpers/>
    </Board>
</Project>");

            return GenerationEnvironment.ToString();
        }

    }

}
