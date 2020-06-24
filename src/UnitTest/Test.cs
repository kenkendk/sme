﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TestRunner
    {
        private void RunTest(Type target, bool runGhdl = true, bool runCpp = false)
        {
            var programname = target.Assembly.GetName().Name;
            //var targetfolder = Path.Combine(Path.GetTempPath(), programname);
            var targetfolder = Path.GetDirectoryName(target.Assembly.Location);

            var outputfolder = Path.Combine(targetfolder, "output");

            if (!Directory.Exists(targetfolder))
                Directory.CreateDirectory(targetfolder);

            if (Directory.Exists(outputfolder))
                Directory.Delete(outputfolder, true);

            Environment.CurrentDirectory = targetfolder;

            var method = target.Assembly.EntryPoint;
            method.Invoke(null, new object[] { new string[0] });

            if (runGhdl)
            {
                var vhdlfolder = Path.Combine(targetfolder, "output");
                if (RunExternalProgram("docker", $"run -t -v {vhdlfolder}:/mnt/data ghdl/ghdl:ubuntu18-mcode /bin/bash -c \"cd /mnt/data/vhdl; make; ret=$?; rm -r work; exit $ret\"", targetfolder) != 0)
                    throw new Exception($"Failed to run VHDL for {programname}");
            }

            if (runCpp)
            {
                var cppfolder = Path.Combine(targetfolder, "output", "cpp");
                if (RunExternalProgram("make", "", cppfolder) != 0)
                    throw new Exception($"Failed to run CPP Make for {programname}");
                if (RunExternalProgram(Path.Combine(cppfolder, programname), "", cppfolder) != 0)
                    throw new Exception($"Failed to run CPP for {programname}");
            }
        }


        private static async Task CopyStreamAsync(TextReader source, TextWriter target)
        {
            string read;
            while ((read = await source.ReadLineAsync()) != null)
                await target.WriteLineAsync(read);
        }

        private int RunExternalProgram(string command, string arguments, string workingfolder)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(command, arguments)
            {
                WorkingDirectory = workingfolder,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var ps = System.Diagnostics.Process.Start(psi);
            var errorLine = string.Empty;

            Func<StreamReader, TextWriter, Task> copyAndCheck = async (source, sink) =>
            {
                string line;
                while ((line = await source.ReadLineAsync()) != null)
                {
                    // Check for error markers
                    if (string.IsNullOrWhiteSpace(errorLine) && (line ?? string.Empty).Contains("error"))
                        errorLine = line;

                    await sink.WriteLineAsync(line);
                }
            };


            var tasks = Task.WhenAll(
                copyAndCheck(ps.StandardOutput, Console.Out),
                copyAndCheck(ps.StandardError, Console.Out)
            );

            ps.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
            if (ps.HasExited)
            {
                tasks.Wait(TimeSpan.FromSeconds(5));
                if (!string.IsNullOrWhiteSpace(errorLine))
                    throw new Exception($"Console output indicates error: {errorLine}");

                return ps.ExitCode;
            }
            else
            {
                ps.Kill();
                throw new Exception($"Failed to run process within the time limit");
            }
        }

        [TestMethod]
        public void RunAES256()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/AES256CBC/AES256CBC.csproj";
            RunTest(typeof(AES256CBC.Tester), true, false);
        }

        [TestMethod]
        public void RunColorBin()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/ColorBin/ColorBin.csproj";
            RunTest(typeof(ColorBin.ImageInputSimulator), true, false);
        }

        [TestMethod]
        public void RunExternalComponent()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/ExternalComponent/ExternalComponent.csproj";
            RunTest(typeof(ExternalComponent.SimpleDualPortBlockRamTester<>), true, false);
        }

        [TestMethod]
        public void RunNoiseFilter()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/NoiseFilter/NoiseFilter.csproj";
            RunTest(typeof(NoiseFilter.ImageInputSimulator), true, false);
        }

        [TestMethod]
        public void RunSimpleComponents()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/SimpleComponents/SimpleComponents.csproj";
            RunTest(typeof(SimpleComponents.ComponentTester), true, false);
        }

        [TestMethod]
        public void RunSimpleMemoryBus()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/SimpleMemoryBus/SimpleMemoryBus.csproj";
            RunTest(typeof(SimpleMemoryBus.MemoryTester), true, false);
        }

        [TestMethod]
        public void RunSimpleMIPS()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/SimpleMIPS/SimpleMIPS.csproj";
            RunTest(typeof(SimpleMIPS.Tester), true, false);
        }

        [TestMethod]
        public void RunSimpleNestedComponent()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/SimpleNestedComponent/SimpleNestedComponent.csproj";
            RunTest(typeof(SimpleNestedComponent.TestDriver), true, false);
        }

        [TestMethod]
        public void RunSimpleTrader()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/SimpleTrader/SimpleTrader.csproj";
            RunTest(typeof(SimpleTrader.ITraderInput), true, false);
        }

        [TestMethod]
        public void RunStatebasedCounter()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/StatebasedCounter/StatebasedCounter.csproj";
            RunTest(typeof(StatebasedCounter.MainClass), true, false);
        }

        [TestMethod]
        public void RunStatedAdder()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/StatedAdder/StatedAdder.csproj";
            RunTest(typeof(StatedAdder.Adder), true, false);
        }

        [TestMethod]
        public void RunStateMachineTester()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/StateMachineTester/StateMachineTester.csproj";
            RunTest(typeof(StateMachineTester.MainClass), true, false);
        }

        [TestMethod]
        public void RunStopwatch()
        {
            SME.Simulation.ProjectPath = "../../../../Examples/Stopwatch/Stopwatch.csproj";
            RunTest(typeof(Stopwatch.MainClass), true, false);
        }

    }
}
