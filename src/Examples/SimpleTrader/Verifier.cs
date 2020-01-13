using SME;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleTrader
{

    public class Verifier : SimulationProcess
    {

        public Verifier()
        {
            make_capture = true;
        }

        public Verifier(string expected_path)
        {
            var lines = File.ReadLines(expected_path).Select(x => x.Split(','));
            ewma_up   = lines.Select(x => bool.Parse(x[0])).ToArray();
            ewma_down = lines.Select(x => bool.Parse(x[1])).ToArray();
            fir_up    = lines.Select(x => bool.Parse(x[2])).ToArray();
            fir_down  = lines.Select(x => bool.Parse(x[3])).ToArray();
        }

        [InputBus]
        public TraderCoreEWMA.ITraderOutput ewma;

        [InputBus]
        public TraderCoreFIR.ITraderOutput fir;

        int i = 0;
        bool[] ewma_up;
        bool[] ewma_down;
        bool[] fir_up;
        bool[] fir_down;

        bool make_capture = false;

        public async override Task Run()
        {
            await ClockAsync();
            while (!fir.Valid)
                await ClockAsync();

            if (make_capture)
            {
                using (var file = File.OpenWrite("tmp.txt"))
                    using (var writer = new StreamWriter(file))
                        while (fir.Valid || SimulationDriver.running){
                            writer.WriteLine($"{ewma.GoingUp},{ewma.GoingDown},{fir.GoingUp},{fir.GoingDown}");
                            await ClockAsync();
                        }
            }
            else
            {
                while (fir.Valid || SimulationDriver.running)
                {
                    Debug.Assert(ewma_up[i] == ewma.GoingUp);
                    Debug.Assert(ewma_down[i] == ewma.GoingDown);
                    Debug.Assert(fir_up[i] == fir.GoingUp);
                    Debug.Assert(fir_down[i] == fir.GoingDown);

                    i++;
                    await ClockAsync();
                }
            }
        }
    }

}