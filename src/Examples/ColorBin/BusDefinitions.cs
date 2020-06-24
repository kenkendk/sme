using SME;

namespace ColorBin
{
    /// <summary>
    /// A bus for reading image values from a sensor,
    /// one pixel at a time
    /// </summary>
    public interface ImageInputLine : IBus
    {
        [InitialValue]
        bool IsValid { get; set; }
        [InitialValue]
        bool LastPixel { get; set; }

        byte R { get; set; }
        byte G { get; set; }
        byte B { get; set; }
    }

    /// <summary>
    /// Bus for reporting counts in the bins
    /// </summary>
    [InitializedBus]
    public interface BinCountOutput : IBus
    {
        bool IsValid { get; set; }

        uint Low { get; set; }
        uint Medium { get; set; }
        uint High { get; set; }
    }
}
