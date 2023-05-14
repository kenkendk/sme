using SME;

[InitializedBus]
public interface ValueBus : IBus
{
    bool valid { get; set; }
    int Value { get; set; }
}