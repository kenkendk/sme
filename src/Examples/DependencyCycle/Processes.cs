using System;
using SME;

namespace DependencyCycle
{

    public abstract class Id : SimpleProcess
    {
        [InputBus]
        public IntBus input;

        [OutputBus]
        public IntBus output = Scope.CreateBus<IntBus>();

        protected override void OnTick()
        {
            output.value = input.value;
        }
    }

    public class UnclockedId : Id { }

    [ClockedProcess]
    public class ClockedId : Id { }

    public class Dummy : SimulationProcess
    {
        public override async System.Threading.Tasks.Task Run()
        {
            await ClockAsync();
        }
    }

}