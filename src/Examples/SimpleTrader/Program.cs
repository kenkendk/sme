using SME;

namespace SimpleTrader
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                var driver = new SimulationDriver(42);
                var fir = new TraderCoreFIR();
                var ewma = new TraderCoreEWMA();
                var verifier = new Verifier("expected.txt");

                fir.Input = driver.Output;
                ewma.Input = driver.Output;
                verifier.ewma = ewma.Output;
                verifier.fir = fir.Output;

                sim
                    .AddTopLevelInputs(driver.Output)
                    .AddTopLevelOutputs(fir.Output, ewma.Output)
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    //.BuildCPP()
                    .Run();
            }
        }
    }
}
