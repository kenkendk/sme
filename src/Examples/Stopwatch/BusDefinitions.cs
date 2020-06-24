using System;
using SME;
using SME.VHDL;

namespace Stopwatch
{
    [InitializedBus, TopLevelInputBus]
    public interface Buttons : IBus
    {
        bool reset { get; set; }
        bool startstop { get; set; }
    }

    [InitializedBus]
    public interface WatchOutput : IBus
    {
        bool running { get; set; }
        bool reset { get; set; }
    }

    [InitializedBus, TopLevelOutputBus]
    public interface NumberOutput : IBus
    {
        UInt6 val { get; set; }
    }
}
