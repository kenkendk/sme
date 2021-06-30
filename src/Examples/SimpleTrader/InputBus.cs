using SME;

namespace SimpleTrader
{
    /// <summary>
    /// The top-level bus that communicates input values
    /// </summary>
    public interface ITraderInput : IBus
    {
        [InitialValue]
        bool Valid { get; set; }
        [InitialValue]
        bool Restart { get; set; }
        uint Value { get; set; }
    }
}
