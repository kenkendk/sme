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
        int skips = 100; // Set to 5000000 (5 MHz) for 1 Hz blinking (if clocked at 5 MHz)
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
