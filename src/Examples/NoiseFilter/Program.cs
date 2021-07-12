using SME;

namespace NoiseFilter
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var sim = new Simulation())
            {
                // Short test
                var inpu_simu = new ImageInputSimulator("input/image1.png");
                // Long test
                //var inpu_simu = new ImageInputSimulator();
                var bord_emit = new BorderEmitter();
                var sten_emit = new StencilEmitter();
                var sten_appl = new StencilApplier();
                var imag_sink = new ImageOutputSink();

                inpu_simu.Delay = bord_emit.Delay;
                bord_emit.Configuration = inpu_simu.Configuration;
                bord_emit.Input = inpu_simu.Data;
                sten_emit.Configuration = inpu_simu.Configuration;
                sten_emit.Data = bord_emit.Output;
                sten_appl.Input = sten_emit.Output;
                imag_sink.Config = inpu_simu.Configuration;
                imag_sink.Input = sten_appl.Output;
                imag_sink.Padded = bord_emit.Output;

                sim
                    .AddTopLevelInputs(inpu_simu.Configuration, inpu_simu.Data)
                    .AddTopLevelOutputs(imag_sink.Input, imag_sink.Padded)
                    .BuildCSVFile()
                    .BuildGraph()
                    .BuildVHDL()
                    //.BuildCPP()
                    .BuildJsonFile()
                    .Run();
            }
        }
    }
}
