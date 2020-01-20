using SME;

namespace StateMachineTester
{
    [InitializedBus]
    public interface IControlBus : IBus
    {
        bool Go1 { get; set; }
        bool Go2 { get; set; }
        int Value { get; set; }
    }

    [InitializedBus]
    public interface IResultBus : IBus
    {
        int State { get; set; }
    }
}