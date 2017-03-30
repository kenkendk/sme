[![Build Status](https://travis-ci.org/kenkendk/sme.svg?branch=master)](https://travis-ci.org/kenkendk/sme)

# Synchronous Message Exchange - SME

Synchronous Message Exchange is a programming model for developing highly concurrent systems. Development is targeted at rapid FPGA development and testing, but the simulation part can be used to describe other kinds of systems, in particular concurrent control logic.

With the C# SME library, it is possible to write control logic entirely within a normal C# environment, including test benches and unittests.

For a subset of the C# language, it is possible to automatically _transpile_ the program into VHDL that can be synthesized for FPGA circuits. With each generated VHDL output is also an automatically generated testbench that can load a trace file with values from a C# test run. With sufficient coverage in the C# source code, this can give a high degree of confidence that the C# and VHDL versions are equivalent.

By leveraging the features of a modern C# IDE, such as Visual Studio, it becomes much faster to develop, experiment and test FPGA designs, especially for a software developer.

# Concurrency as a design feature

Most other VHDL generating tools, attempt to use a sequential programming model, and then extract as much concurrency from this as possible.

With SME, the design is naturally concurrent, making it much simple to compare the source C# model with the resulting VHDL output, and thus also making it simpler to reason about final resource usage and performance.

The concurrency in SME arises from the use of encapsulated processes as well as explict communication with latencies.

# Extensible VHDL

As the generated VHDL follows the original source very closely, it is possible for experienced VHDL developers to augment the generated VHDL with implementation details that are hard to express with C#. With the test bench, it is possible to continue development entirely in VHDL, and leverage the test bench to ensure that the two implementations are still equivalent.

# Integration with existing IP

If the project needs to integrate with existing pre-built components, it is possible to write a simulation component in C# and have the VHDL generated to match the interface. The SME library contains an implementation of this approach, wrapping the Xlinix Block RAM, and allowing the same configurations as the native component.

# Example

In this example, we assume we have an external camera that emits a single pixel (in RGB format) each clock cycle. The objective is to clasify each pixel in one of three different intensities. The results are accumulated, and the count is delivered to the output when the camera has sent the final pixel.

## Communication

The communication has an input and an out, that we define as C# interfaces:

```csharp
[TopLevelInputBus]
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

[TopLevelOutputBus]
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

The attributes are not required, but assists the VHDL generation tool with generating better encapsulated outputs. 

## Processing

The actual processing code is writtin in a simplistic manner that does not require the use of dynamic memory, such that it can be converted to VHDL. Notice that the `Bus` elements are not explicitly instanciated; this is done automatically when loading the SME design:


```csharp
public class ColorBinCollector : SimpleProcess
{
	[InputBus]
	ImageInputLine Input;

	[InputBus, OutputBus]
	BinCountOutput Output;

	const uint HighThreshold = 200;
	const uint MediumThreshold = 100;

	protected override void OnTick()
	{
		var countlow = Output.Low;
		var countmed = Output.Medium;
		var counthigh = Output.High;

		if (Output.IsValid)
			countlow = countmed = counthigh = 0;
		
		if (Input.IsValid)
		{
			//R=0.299, G=0.587, B=0.114
			var color = ((Input.R * 299u) + (Input.G * 587u) + (Input.B * 114u)) / 1000u;
			if (color > HighThreshold)
				counthigh++;
			else if (color > MediumThreshold)
				countmed++;
			else
				countlow++;
		}

		Output.Low = countlow;
		Output.Medium = countmed;
		Output.High = counthigh;
		Output.IsValid = Input.IsValid && Input.LastPixel;
	}
}
```

## Simulation

To simulate a camera, we load an image and outputs the pixels one at a time. Since this is merely for simulation, we can use any .Net library, such as the imaging libraries. Notice again that the `Bus` elements are not explicitly instanciated.

```csharp
public class ImageInputSimulator : SimulationProcess
{
	[OutputBus]
	private ImageInputLine Data;

	public static string[] IMAGES = new string[] { "image1.png", "image2.jpg", "image3.png" };

	public override async Task Run()
	{
		await ClockAsync();

		foreach (var file in IMAGES)
		{
			if (!System.IO.File.Exists(file))
			{
				Console.WriteLine($"File not found: {file}");
			}
			else
			{
				using (var img = System.Drawing.Image.FromFile(file))
				using (var bmp = new System.Drawing.Bitmap(img))
				{
					Console.WriteLine($"Writing {bmp.Width * bmp.Height} pixels from {file}");

					Data.IsValid = true;

					for (var i = 0; i < img.Height; i++)
					{
						for (var j = 0; j < img.Width; j++)
						{
							var pixel = bmp.GetPixel(j, i);
							Data.R = pixel.R;
							Data.G = pixel.G;
							Data.B = pixel.B;
							Data.LastPixel = i == img.Height - 1 && j == img.Width - 1;

							await ClockAsync();
						}
					}

					Data.IsValid = false;
					Data.LastPixel = false;
				}
			}
		}

		await ClockAsync();
	}
}
```

## Setting it up

To load the libraries, run the simulation, generate the trace file, and transpile into VHDL, we set it up like this:

```csharp
class MainClass
{
	public static void Main(string[] args)
	{
		// Faster test
		ImageInputSimulator.IMAGES = new[] { "image1.png" };

		new Simulation()
			.BuildCSVFile()
			.BuildGraph()
			.BuildVHDL()
			.Run(typeof(MainClass).Assembly);
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

More examples can be found in the [Examples folder](https://github.com/kenkendk/sme/tree/master/src/Examples).

# Literature

This SME approach is described in more detail in these academic papers:
  * [BPU Simulator](http://www.wotug.org/papers/CPA-2013/Rehr13/Rehr13.pdf)
  * [Synchronous Message Exchange for Hardware Designs](http://wotug.org/cpa2014/preprints/12-preprint.pdf)
  * [Bus Centric Synchronous Message Exchange for Hardware Designs](https://www.researchgate.net/profile/Kenneth_Skovhede/publication/281278995_Bus_Centric_Synchronous_Message_Exchange_for_Hardware_Designs/links/55deccc808ae45e825d3a681.pdf)

The library is used as a means for simulating and experimenting with designing a vector processor, named the [Bohrium Processing Unit](https://github.com/kenkendk/bpu), capable of running [Bohrium](http://bh107.org) vector byte-code on FPGA hardware.

The packages [SME](https://www.nuget.org/packages/SME/), [SME.GraphViz](https://www.nuget.org/packages/SME.GraphViZ/), and [SME.VHDL](https://www.nuget.org/packages/SME.VHDL/) are all available through [NuGet](https://www.nuget.org).


