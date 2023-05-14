using System;
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
            var testfolder = Assembly.GetExecutingAssembly().Location;
            var examplesfolder = Path.Combine(testfolder, "../../../../../Examples");
            var targetfolder = Path.GetFullPath(Path.Combine(examplesfolder, programname));
            SME.Simulation.ProjectPath = Path.Combine(targetfolder, $"{programname}.csproj");

            var vcd_name = "SME_TEST_SKIP_VCD";
            var vcd_env = Environment.GetEnvironmentVariable(vcd_name);
            var vcd_str = vcd_env == null ? "" : $"{vcd_name}={vcd_env}";

            var use_native_ghdl = "1".Equals(Environment.GetEnvironmentVariable("SME_TEST_USE_NATIVE_GHDL"));

            Environment.CurrentDirectory = targetfolder;

            var method = target.Assembly.EntryPoint;
            method.Invoke(null, new object[] { new string[0] });

            if (runGhdl)
            {
                var outputfolder = Path.Combine(targetfolder, "output");
                var vhdlfolder = Path.Combine(outputfolder, "vhdl");
                if (use_native_ghdl)
                {
                    if (RunExternalProgram("make", "", vhdlfolder) != 0)
                        throw new Exception($"Failed to run VHDL for {programname}");
                }
                else
                {
                    if (RunExternalProgram("docker", $"run -t -v {outputfolder}:/mnt/data ghdl/ghdl:ubuntu20-mcode /bin/bash -c \"cd /mnt/data/vhdl; {vcd_str} make; ret=$?; rm -r work; exit $ret\"", targetfolder) != 0)
                        throw new Exception($"Failed to run VHDL for {programname}");
                }
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
            RunTest(typeof(AES256CBC.Tester), true, false);
        }

        [TestMethod]
        public void RunArrayOfBuses()
        {
            RunTest(typeof(ArrayOfBuses.ReduceAddTester), true, false);
        }

        [TestMethod]
        public void RunColorBin()
        {
            RunTest(typeof(ColorBin.ImageInputSimulator), true, false);
        }

        [TestMethod]
        public void RunDependencyCycle()
        {
            RunTest(typeof(DependencyCycle.Dummy), true, false);
        }

        [TestMethod]
        public void RunExternalComponent()
        {
            RunTest(typeof(ExternalComponent.SimpleDualPortBlockRamTester<>), true, false);
        }

        [TestMethod]
        public void RunNoiseFilter()
        {
            RunTest(typeof(NoiseFilter.ImageInputSimulator), true, false);
        }

        [TestMethod]
        public void RunSimpleComponents()
        {
            RunTest(typeof(SimpleComponents.ComponentTester), true, false);
        }

        [TestMethod]
        public void RunSimpleMemoryBus()
        {
            RunTest(typeof(SimpleMemoryBus.MemoryTester), true, false);
        }

        [TestMethod]
        public void RunSimpleMIPS()
        {
            RunTest(typeof(SimpleMIPS.Tester), true, false);
        }

        [TestMethod]
        public void RunSimpleNestedComponent()
        {
            RunTest(typeof(SimpleNestedComponent.TestDriver), true, false);
        }

        [TestMethod]
        public void RunSimpleTrader()
        {
            RunTest(typeof(SimpleTrader.ITraderInput), true, false);
        }

        [TestMethod]
        public void RunStatebasedCounter()
        {
            RunTest(typeof(StatebasedCounter.MainClass), true, false);
        }

        [TestMethod]
        public void RunStatedAdder()
        {
            RunTest(typeof(StatedAdder.Adder), true, false);
        }

        [TestMethod]
        public void RunStateMachineTester()
        {
            RunTest(typeof(StateMachineTester.MainClass), true, false);
        }

        [TestMethod]
        public void RunStopwatch()
        {
            RunTest(typeof(Stopwatch.MainClass), true, false);
        }

        [TestMethod]
        public void RunUnitTester()
        {
            RunTest(typeof(UnitTester.Program), true, false);
        }

    }
}
