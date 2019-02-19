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
                var vhdlfolder = Path.Combine(targetfolder, "output", "vhdl");
                if (RunExternalProgram("make", "test", vhdlfolder) != 0)
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

            var tasks = Task.WhenAll(
                CopyStreamAsync(ps.StandardOutput, Console.Out),
                CopyStreamAsync(ps.StandardError, Console.Out)
            );

            ps.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
            if (ps.HasExited)
            {
                tasks.Wait(TimeSpan.FromSeconds(5));
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
        public void RunColorBin()
        {
            RunTest(typeof(ColorBin.ImageInputSimulator), true, false);
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

        //[TestMethod]
        //public void RunSimpleMemoryBus()
        //{
        //    RunTest(typeof(SimpleMemoryBus.MemoryTester), true, false);
        //}

        //[TestMethod]
        //public void RunSimpleNestedComponent()
        //{
        //    RunTest(typeof(SimpleNestedComponent.TestDriver), true, false);
        //}

        [TestMethod]
        public void RunSimpleTrader()
        {
            RunTest(typeof(SimpleTrader.ITraderInput), true, false);
        }

        // [TestMethod]
        // public void RunStatebasedCounter()
        // {
        //     RunTest(typeof(StatebasedCounter.MainClass), true, false);
        // }

        [TestMethod]
        public void RunStatedAdder()
        {
            RunTest(typeof(StatedAdder.Adder), true, false);
        }

    }
}
