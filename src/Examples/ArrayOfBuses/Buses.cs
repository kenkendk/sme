using SME;

namespace ArrayOfBuses
{
    [InitializedBus]
    public interface ValueBus : IBus
    {
        bool valid { get; set; }
        int Value { get; set; }
    }
}