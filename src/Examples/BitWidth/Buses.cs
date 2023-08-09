using System;
using SME;
using SME.VHDL;

namespace BitWidth
{
    [InitializedBus]
    public interface ValueBus : IBus
    {
        bool Valid { get; set; }
        SME.VHDL.UInt10 Value { get; set; }
    }
}