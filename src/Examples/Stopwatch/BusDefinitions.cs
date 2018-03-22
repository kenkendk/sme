using System;
using SME;

namespace Stopwatch
{
    [InitializedBus]
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
}
