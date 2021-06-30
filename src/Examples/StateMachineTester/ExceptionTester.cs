using SME;
using System.Threading.Tasks;

namespace StateMachineTester
{

    public class ExceptionTester : SimulationProcess
    {
        public ExceptionTester(ExceptionTest test)
        {
            control = test.control;
            result = test.result;
        }

        [OutputBus] public IControlBus control;

        [InputBus] public IResultBus result;

        public async override Task Run()
        {
            await ClockAsync();
        }
    }

}