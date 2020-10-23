using System;
using SME;

namespace FloatingPoint
{

    public interface ValBus : IBus
    {
        uint val { get; set; }
    }

}