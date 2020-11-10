using SME;

namespace UnitTester
{

    [InitializedBus]
    public interface ValueBus : IBus
    {
        bool valid { get; set; }
        int  value { get; set; }
    }

}