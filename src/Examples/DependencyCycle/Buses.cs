using System;
using SME;

namespace DependencyCycle
{

    public interface IntBus : IBus
    {
        int value { get; set; }
    }

}