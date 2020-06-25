using SME;

namespace AES256CBC
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                var core = new AESCore();
                var test = new Tester();

                core.Input = test.Input;
                test.Output = core.Output;

                sim
                    .AddTopLevelInputs(core.Input)
                    .AddTopLevelOutputs(core.Output)
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    //.BuildCPP()
                    .Run();
            }
        }
    }
}
