using System;
using SME;

namespace SimpleMIPS
{
    public class Tester : SimulationProcess
    {
        [InputBus]
        Terminate term = Scope.CreateOrLoadBus<Terminate>();

        public async override System.Threading.Tasks.Task Run()
        {
            while (!term.flg)
                await ClockAsync();
        }
    }
}
