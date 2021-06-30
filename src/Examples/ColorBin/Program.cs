using SME;

namespace ColorBin
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                var tester = new ImageInputSimulator("input/image1.png");
                var collector = new ColorBinCollector();

                collector.Input = tester.Data;
                tester.Result = collector.Output;

                sim
                    .AddTopLevelInputs(collector.Input)
                    .AddTopLevelOutputs(collector.Output)
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    //.BuildCPP()
                    .Run();
            }
        }
    }
}
