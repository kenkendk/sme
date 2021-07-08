using SME;
using System;

namespace ColorBin
{

    /// <summary>
    /// Counts the values of the given pixels into three bins: low, medium and
    /// high. Once the last pixel have been written, it outputs these bins for
    /// exactly one clock cycle. After this cycle, the internal counters are
    /// reset to 0.
    /// </summary>
    public class ColorBinCollector : SimpleProcess
    {
        [InputBus]
        public ImageInputLine Input;

        [OutputBus]
        public BinCountOutput Output = Scope.CreateBus<BinCountOutput>();

        const uint HighThreshold = 200;
        const uint MediumThreshold = 100;

        bool was_valid = false;
        uint countlow = 0;
        uint countmed = 0;
        uint counthigh = 0;

        protected override void OnTick()
        {
            if (was_valid)
                countlow = countmed = counthigh = 0;

            if (Input.IsValid)
            {
                //R=0.299, G=0.587, B=0.114
                var color = ((Input.R * 299u) + (Input.G * 587u) + (Input.B * 114u)) / 1000u;
                if (color > HighThreshold)
                    counthigh++;
                else if (color > MediumThreshold)
                    countmed++;
                else
                    countlow++;
            }
            was_valid = Input.IsValid && Input.LastPixel;

            Output.Low = countlow;
            Output.Medium = countmed;
            Output.High = counthigh;
            Output.IsValid = was_valid;
        }
    }

}
