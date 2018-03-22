using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestFixture()]
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

        private int RunCPP(string vhdlfolder)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("make", "test")
            {
                WorkingDirectory = vhdlfolder,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var ps = System.Diagnostics.Process.Start(psi);

            var tasks = Task.WhenAny(
                ps.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput()),
                ps.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError())
            );

            ps.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
            if (ps.HasExited)
            {
                tasks.Wait(TimeSpan.FromSeconds(100));
                return ps.ExitCode;
            }
            else
            {
                ps.Kill();
                throw new Exception($"Failed to run process within the time limit");
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

            var tasks = Task.WhenAny(
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

        [Test()]
        public void RunAES256()
        {
            RunTest(typeof(AES256CBC.Tester), true, true);
        }

        [Test()]
        public void RunColorBin()
        {
            RunTest(typeof(ColorBin.ImageInputSimulator), true, false);
        }

        [Test()]
        public void RunExternalComponent()
        {
            RunTest(typeof(ExternalComponent.SimpleDualPortBlockRamTester<>), true, false);
        }

        [Test()]
        public void RunNoiseFilter()
        {
            RunTest(typeof(NoiseFilter.ImageInputSimulator), true, false);
        }


    }
}
