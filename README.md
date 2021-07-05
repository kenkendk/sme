![Build status](https://github.com/kenkendk/sme/actions/workflows/dotnet.yml/badge.svg) ![SME NuGet Package](https://img.shields.io/nuget/vpre/SME.svg?style=flat) ![SME.VHDL NuGet Package](https://img.shields.io/nuget/vpre/SME.VHDL.svg?style=flat) ![SME.Tracer NuGet Package](https://img.shields.io/nuget/vpre/SME.Tracer.svg?style=flat)

# Synchronous Message Exchange - SME

Synchronous Message Exchange is a programming model for developing highly concurrent systems. Development is targeted at rapid FPGA development and testing, but the simulation part can be used to describe other kinds of systems, in particular concurrent control logic.

With the C# SME library, it is possible to write control logic entirely within a normal C# environment, including test benches and unittests.

For a subset of the C# language, it is possible to automatically _transpile_ the program into VHDL that can be synthesized for FPGA circuits. With each generated VHDL output is also an automatically generated testbench that can load a trace file with values from a C# test run. With sufficient coverage in the C# source code, this can give a high degree of confidence that the C# and VHDL versions are equivalent.

By leveraging the features of a modern C# IDE, such as Visual Studio, it becomes much faster to develop, experiment and test FPGA designs, especially for a software developer.

Just want to jump in and see something working? Try the [SME getting started example](https://github.com/kenkendk/sme-gettingstarted).

# Concurrency as a design feature

Most other VHDL generating tools, attempt to use a sequential programming model, and then extract as much concurrency from this as possible.

With SME, the design is naturally concurrent, making it much simpler to compare the source C# model with the resulting VHDL output, and thus also making it simpler to reason about final resource usage and performance.

The concurrency in SME arises from the use of encapsulated processes as well as explict communication with latencies.

# Extensible VHDL

As the generated VHDL follows the original source very closely, it is possible for experienced VHDL developers to augment the generated VHDL with implementation details that are hard to express with C#. With the test bench, it is possible to continue development entirely in VHDL, and leverage the test bench to ensure that the two implementations are still equivalent.

# Integration with existing IP

If the project needs to integrate with existing pre-built components, it is possible to write a simulation component in C# and have the VHDL generated to match the interface. The SME library contains an implementation of this approach, wrapping the Xlinix Block RAM, and allowing the same configurations as the native component.

# Example

In this example, we assume we have an external camera that emits a single pixel (in RGB format) each clock cycle. The objective is to clasify each pixel in one of three different intensities. The results are accumulated, and the count is delivered to the output when the camera has sent the final pixel.

The example here is the same as used in the [SME getting started example](https://github.com/kenkendk/sme-gettingstarted) so you can try it out after reading about it here.

## Communication

The communication has an input and an out, that we define as C# interfaces:

```csharp
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

public interface BinCountOutput : IBus
{
    [InitialValue]
    bool IsValid { get; set; }

    [InitialValue]
    uint Low { get; set; }
    [InitialValue]
    uint Medium { get; set; }
    [InitialValue]
    uint High { get; set; }
}
```

As all signals on the busses are undefined on startup, and thus cannot be read, it can be cumbersome to bootstrap the design. The `[InitialValue]` attribute helps by forcing a value to the signals on startup. If you prefer to have all values initialized, you can use the `[Initialized]` attribute on the bus definition.

*Note*: We never create an implementation of the interface. The SME system will create an automatic implementation that enforces the communication semantics, without requiring the user to worry about anything but the interface.

## Processing

The actual processing code is writtin in a simplistic manner that does not require the use of dynamic memory, such that it can be converted to VHDL. Notice that the `Bus` elements are not explicitly instanciated; this is done automatically when loading the SME design:


```csharp

/// <summary>
/// The bin counter process
/// </summary>
public class ColorBinCollector : SimpleProcess
{
    /// <summary>
    /// The bus that we read input pixels from
    /// </summary>
    [InputBus] private readonly ImageInputLine m_input;

    /// <summary>
    /// The bus that we write results to
    /// </summary>
    [OutputBus] public readonly BinCountOutput Output = Scope.CreateBus<BinCountOutput>();

    /// <summary>
    /// The threshold when a pixel is deemed high intensity
    /// </summary>
    const uint HighThreshold = 200;
    /// <summary>
    /// The threshold when a pixel is deemed medium intensity
    /// </summary>
    const uint MediumThreshold = 100;

    /// <summary>
    /// The current number of low intensity pixels
    /// </summary>
    private uint m_low;
    /// <summary>
    /// The current number of medium intensity pixels
    /// </summary>
    private uint m_med;
    /// <summary>
    /// The current number of high intensity pixels
    /// </summary>
    private uint m_high;

    /// <summary>
    /// Constructs a new bin counter process
    /// </summary>
    /// <param name="input">The camera input bus</param>
    public ColorBinCollector(ImageInputLine input)
    {
        // The constructor is not translated into hardware,
        // so it is possible to have dynamic and initialization
        // When the simulation "run" method is called,
        // the values of all variables are captured and used for
        // initialization
        m_input = input ?? throw new ArgumentNullException(nameof(input));
    }

    /// <summary>
    /// The method invoked when all inputs are ready.
    /// The method is only invoked once pr. clock cycle
    /// </summary>
    protected override void OnTick()
    {
        // If the input pixel is valid, increment the relevant counter
        if (m_input.IsValid)
        {
            //R=0.299, G=0.587, B=0.114
            var color = ((m_input.R * 299u) + (m_input.G * 587u) + (m_input.B * 114u)) / 1000u;
            if (color > HighThreshold)
                m_high++;
            else if (color > MediumThreshold)
                m_med++;
            else
                m_low++;
        }

        // Check if this is the last pixel
        var done = m_input.IsValid && m_input.LastPixel;

        // Send the output
        Output.Low = m_low;
        Output.Medium = m_med;
        Output.High = m_high;
        Output.IsValid = done;

        // Make sure we reset if this was the last pixel
        if (done)
            m_low = m_med = m_high = 0;
    }
}
```

## Simulation

To simulate a camera, we load an image and outputs the pixels one at a time. Since this is merely for simulation, we can use any .Net library, such as the imaging libraries. Notice again that the `Bus` elements are not explicitly instanciated.

```csharp
/// <summary>
/// Helper process that loads images and writes them into the simulation.
/// Since this is a simulation process, it will not be rendered as hardware
/// and we can use any code and dynamic properties we want
/// </summary>
public class ImageInputSimulator : SimulationProcess
{
    /// <summary>
    /// The camera connection bus
    /// </summary>
    [OutputBus]
    public readonly ImageInputLine Data = Scope.CreateBus<ImageInputLine>();

    /// <summary>
    /// The images to process
    /// </summary>
    private readonly string[] IMAGES;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:GettingStarted.ImageInputSimulator"/> class.
    /// </summary>
    /// <param name="images">The images to process.</param>
    public ImageInputSimulator(params string[] images)
    {
        if (images == null)
        throw new ArgumentNullException(nameof(images));
        if (images.Length == 0)
        throw new ArgumentOutOfRangeException(nameof(images), "No images to send?");
        IMAGES = images;
    }

    /// <summary>
    /// Run this instance.
    /// </summary>
    public override async Task Run()
    {
        // Wait for the initial reset to propagate
        await ClockAsync();

        // Run through all images
        foreach (var file in IMAGES)
        {
            // Sanity check
            if (!System.IO.File.Exists(file))
            {
                Console.WriteLine($"File not found: {file}");
            }
            else
            {
                // Load the image as a bitmap
                using (var img = System.Drawing.Image.FromFile(file))
                using (var bmp = new System.Drawing.Bitmap(img))
                {
                    // Write some console progress
                    Console.WriteLine($"Writing {bmp.Width * bmp.Height} pixels from {file}");

                    // We are now transmitting data
                    Data.IsValid = true;

                    // Loop through the image pixels
                    for (var i = 0; i < img.Height; i++)
                    {
                        for (var j = 0; j < img.Width; j++)
                        {
                            // Grab a pixel and send it to the output bus
                            var pixel = bmp.GetPixel(j, i);
                            Data.R = pixel.R;
                            Data.G = pixel.G;
                            Data.B = pixel.B;

                            // Update the LastPixel flag as required
                            Data.LastPixel = i == img.Height - 1 && j == img.Width - 1;

                            await ClockAsync();
                        }

                        // Write progress after each line
                        Console.WriteLine($"Still need to write {bmp.Width * (bmp.Height - i - 1)} pixels");
                    }

                    // We are now done with the image, so signal that
                    Data.IsValid = false;
                    Data.LastPixel = false;
                }
            }
        }

        // Make sure the last pixel has propagated
        await ClockAsync();
    }
}
```

## Setting it up

To load the libraries, run the simulation, generate the trace file, and transpile into VHDL, we set it up like this:

```csharp
public static void Main(string[] args)
{
    using(var sim = new Simulation())
    {
        var simulator = new ImageInputSimulator("image1.png");
        var calculator = new ColorBinCollector(simulator.Data);

        // Use fluent syntax to configure the simulator.
        // The order does not matter, but `Run()` must be
        // the last method called.

        // The top-level input and outputs are exposed
        // for interfacing with other VHDL code or board pins

        sim
            .AddTopLevelOutputs(calculator.Output)
            .AddTopLevelInputs(simulator.Data)
            .BuildCSVFile()
            .BuildVHDL()
            .Run();

        // After `Run()` has been invoked the folder
        // `output/vhdl` contains a Makefile that can
        // be used for testing the generated design
        }
}
```

## The generated VHDL

The VHDL is quite verbose, but a fragment of the generated code is shown here:
```vhdl
num := BinCountOutput_Low;
num2 := BinCountOutput_Medium;
num3 := BinCountOutput_High;
if BinCountOutput_IsValid = '1' then
    tmpvar_1 := STD_LOGIC_VECTOR(TO_UNSIGNED(0, T_SYSTEM_UINT32'length));
    num3 := tmpvar_1;
    tmpvar_0 := tmpvar_1;
    num := tmpvar_0;
    num2 := tmpvar_0;
end if;
if ImageInputLine_IsValid = '1' then
    num4 := STD_LOGIC_VECTOR((((resize(UNSIGNED(STD_LOGIC_VECTOR(resize(UNSIGNED(ImageInputLine_R), T_SYSTEM_UINT32'length))) * TO_UNSIGNED(299, 32), 32)) + (resize(UNSIGNED(STD_LOGIC_VECTOR(resize(UNSIGNED(ImageInputLine_G), T_SYSTEM_UINT32'length))) * TO_UNSIGNED(587, 32), 32))) + UNSIGNED(STD_LOGIC_VECTOR(resize(resize(UNSIGNED(ImageInputLine_B) * TO_UNSIGNED(114, 8), 8), T_SYSTEM_UINT32'length)))) / TO_UNSIGNED(1000, 32));
    if UNSIGNED(num4) > TO_UNSIGNED(200, 32) then
        num3 := STD_LOGIC_VECTOR(UNSIGNED(num3) + TO_UNSIGNED(1, 32));
    else
        if UNSIGNED(num4) > TO_UNSIGNED(100, 32) then
            num2 := STD_LOGIC_VECTOR(UNSIGNED(num2) + TO_UNSIGNED(1, 32));
        else
            num := STD_LOGIC_VECTOR(UNSIGNED(num) + TO_UNSIGNED(1, 32));
        end if;
    end if;
end if;
BinCountOutput_Low <= num;
BinCountOutput_Medium <= num2;
BinCountOutput_High <= num3;
if (ImageInputLine_IsValid = '1') and (ImageInputLine_LastPixel = '1') then
    BinCountOutput_IsValid <= '1';
else
    BinCountOutput_IsValid <= '0';
end if;
```

The above example can be found in the [SME getting started example](https://github.com/kenkendk/sme-gettingstarted).

More examples can be found in the [Examples folder](https://github.com/kenkendk/sme/tree/master/src/Examples).

# Literature

This SME approach is described in more detail in these academic papers:
  * [BPU Simulator](http://www.wotug.org/papers/CPA-2013/Rehr13/Rehr13.pdf)
  * [Synchronous Message Exchange for Hardware Designs](http://wotug.org/cpa2014/preprints/12-preprint.pdf)
  * [Bus Centric Synchronous Message Exchange for Hardware Designs](https://www.researchgate.net/profile/Kenneth_Skovhede/publication/281278995_Bus_Centric_Synchronous_Message_Exchange_for_Hardware_Designs/links/55deccc808ae45e825d3a681.pdf)

The library is used as a means for simulating and experimenting with designing a vector processor, named the [Bohrium Processing Unit](https://github.com/kenkendk/bpu), capable of running [Bohrium](http://bh107.org) vector byte-code on FPGA hardware.

The packages [SME](https://www.nuget.org/packages/SME/), [SME.Tracer](https://www.nuget.org/packages/SME.Tracer/), [SME.GraphViz](https://www.nuget.org/packages/SME.GraphViZ/), and [SME.VHDL](https://www.nuget.org/packages/SME.VHDL/) are all available through [NuGet](https://www.nuget.org).


