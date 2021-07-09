using System;
using SME;
using SME.VHDL;

namespace Stopwatch
{
    [ClockedProcess]
    public class Counter : SimpleProcess
    {
        [InputBus]
        public WatchOutput watch;

        [OutputBus]
        public NumberOutput output = Scope.CreateBus<NumberOutput>();

        UInt6 num = 0;
        int skips = 5000000; // 5 MHz to 1 Hz
        int current = 0;

        protected override void OnTick()
        {
            if (watch.running)
            {
                current++;
                if (current == skips)
                {
                    num++;
                    current = 0;
                }
            }
            else if (watch.reset)
            {
                num = 0;
                current = 0;
            }
            output.val = num;
        }
    }
}
