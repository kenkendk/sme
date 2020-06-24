using SME;

namespace SimpleMIPS
{
    [InitializedBus]
    public interface MemoryInput : IBus
    {
        bool ena { get; set; }
        uint addr { get; set; }
        uint wrdata { get; set; }
        bool wrena { get; set; }
    }

    [InitializedBus]
    public interface MemoryOutput : IBus
    {
        uint rddata { get; set; }
    }

    [InitializedBus]
    public interface Terminate : IBus
    {
        bool flg { get; set; }
    }
}
