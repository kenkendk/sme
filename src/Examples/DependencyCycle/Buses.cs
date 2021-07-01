using System;
using SME;

namespace DependencyCycle
{

    [InitializedBus]
    public interface IntBus : IBus
    {
        int value { get; set; }
    }

}