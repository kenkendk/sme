SME tutorial
============


# About Synchronous Message Exchange

Synchronous Message Exchange, SME, is a modelling tool for bulding massively concurrent systems, especially tailored at designing hardware circuits.

SME leverages many years of educational results teaching concurrent programming and uses these to provide a way to model concurrent systems that is understandable and does not deal with the pitfalls commonly found in concurrent programs, such as race conditions, deadlocks and others.

# Basic concepts - A first processs

There are only two basic components in SME: processes and busses.

The processes are encapsulated pieces of a program. You can think of them as a function or a thread.

The busses define and manange the data that is exchanged between processes. You can think of them as a pipe or a channel.

With those two concepts, lets define a simple SME program that adds two numbers:

```csharp
public class Adder : SimpleProcess
{
  public interface IAddOperands : IBus
  {
    [InitialValue(0)]
    public int A { get; set; }
    [InitialValue(0)]
    public int B { get; set; }
  }

  public interface IAddResult : IBus
  {
    public int C { get; set; }
  }

  [InputBus]
  private readonly IAddOperands Input = Scope.CreateBus<IAddOperands>();
  [OutputBus]
  private readonly IAddResult Output = Scope.CreateBus<IAddResult>();

  public override void OnTick()
  {
    Output.C = Input.A + Input.B;
  }
}
```

There are a number of things to note in this simple example. First we define two interfaces that are derived from `IBus`, and will serve as the communication between the process and the outside world. These do not need to be defined inside the process, but for a re-usable process such as this, it makes sense to define the busses inside the class such that they are grouped inside the `Adder` namespace.

Inside each bus definition, we simply add the properties that we want to read and write outside the process. In this example we have added an `InitialValue` attribute to the inputs, because by default busses in SME does not carry a default value. If you attempt to read from a property that has not previously been written, you will get an exception. By setting the `InitialValue(0)` on the field, we can override this so we do not have to deal with uninitialized values.

You can think of busses as the input and output arguments to a function. When naming these busses, it is very tempting to call them `Input` and `Output` but I recommend that you resist that urge. While it makes perfect sense while writing the process, it is easy to get confused when _using_ the process later, as the process that *outputs* data to the `Adder` will then send *output* to `IInput`.

When declaring the busses for use inside the process, it is perfectly fine to use the names `Input` and `Output`, as these names are not visible outside the process. In the example, we use the `Scope.CreateBus<T>()` function to get an instance of a bus that fits the description we made. It is important to note that we do not provide an implementation for the busses, just the definition, and the SME library takes care of providing an implementation for us.

Finally we have the method that does all the work: `OnTick()`. This method is invoked whenever the process needs to perform its actions. Exactly _when_ the process is invoked is up to the SME library. The library looks at all the potential inputs to the process and once they are ready, it will invoke the process, but only one invocation is performed for each process in each iteration.

This is a different approach compared to normal programs, where methods are invoked when you call them. You can think of this as programming in an environment with discrete timesteps, where all processes run in each timestep. If you have looked at hardware design before, this is something in between _combinatorial logic_ and a _clocked process_.

# Testing the process

One great benefit from writing a process, is that you can easily test it in isolation. This is particularly easy using the expressive power of the C# programming language and runtime system. To test the `Added` example, lets write a new process that checks that a few numbers are working:

```csharp
public class AdderTester : SimulationProcess
{
  [InputBus, OutputBus]
  private readonly Adder.IAddOperands Operands = Scope.CreateOrLoadBus<Adder.IAddOperands>();
  [InputBus]
  private readonly Adder.IAddResult Result = Scope.CreateOrLoadBus<Adder.IAddResult>();

  public async Task RunAsync()
  {
    await ClockAsync();

    Operands.A = 1;
    Operands.B = 2;

    await ClockAsync();

    Debug.Assert(Result.C == Operands.A + Operands.B);
    Operands.A = Operands.B = 2;

    await ClockAsync();

    Debug.Assert(Result.C == Operands.A + Operands.B);
    Operands.A = Operands.B = 1;

    // Still holds, A and B not updated immediately
    Debug.Assert(Result.C == Operands.A + Operands.B);

    await ClockAsync();
  }
}
```

In the tester we can se that we reference the same busses, and use the `Scope.CreateOrLoadBus<T>()` method to load the busses from the adder. This way we can load the busses by depending only on the bus type, and do not have to deal with connecting bus ends. If you prefer, you can also just pass around the actual bus references or use naming, which is illustrated in a later example.

Since we are simulating, this process derives from `SimulationProcess` marking that this process is only used for testing. When implementing a simulation process, we override the `RunAsync()` method instead of the `OnTick()` method. This gives us greater flexibility, in that we can explicitly wait for the simultation timestep to finish with `await ClockAsync();`. In the tester code we start by waiting for the tester process to be reset, and then we set the operands. If we attempt to read the `Result.C` variable at this point, we would get `ReadViolationException` as the value is not yet ready. After waiting for the clock, the values have now propagated and we can read both the values we wrote to `Operands` but also the value from `Result`.

In the last test case we can see that this propagation delay is also applied to the inputs. Even though we set `Operands.A = Operands.B = 1`, the values are not yet updated, so when we test the condition from before, it still holds, even though we have written the values in the previous step. This "timed logic" or "delayed propagation" is handled entirely by the bus implementations, which is why you inly have to supply the interface for the bus, and not the actual implementation.

# Running a simulation

To glue the tester and the implementation together, we need to set up a simulation environment and run it:

```csharp
static void Main(string[] args)
{
  using(var simulation = new Simulation())
  {
    simulation
      .BuildCSVFile()
      .BuildGraph()
      .Run(
        new Adder(),
        new AdderTester()
      );
  }
}
```

The simulation provides a scope in which all execution can occur. It is important that you only create processes within this scope, because SME will keep track of processes that are created within the scope. For this reason, it is not required that you add the processes that you create to the `Run()` method, you can simply construct them (calling `new`) inside the scope, and they will be part of the simulation.

You do not need to dispose the simulation scope, unless you want to run multiple simulations in the same process, but for using a common notation we use that setup here.

As the example shows, it is possible to add functionality to the simulation. In this example, we build a traces file via the `BuildCSVFile()` call and a dependency graph via `BuildGraph()`.

# Making hardware-ready circuits

As SME is designed to model hardware circuits, it is a matter of requesting this as part of the simulation. Simply call `BuildVHDL()` on the `simulation` instance, and it will produce a full set of [VHDL](https://en.wikipedia.org/wiki/VHDL) files that can be loaded in an FPGA vendor tool. For testing the generated circuit, SME provides a `Makefile` that relies on the open source [GHDL](http://ghdl.free.fr/) tool to compile and run the design.

The generated VHDL code also contains a testbench that can be used to verify the generated design in a vendor tool, or GHDL. The testbench loads a CSV file generated by `BuildCSVFile()` and uses this to write test data into the circuit and verify that all components generate the correct responses. This automated testing is also performed with GHDL, which can be used to quickly get a validation on the generated code before loading the design into a vendor tool. Should you want to debug or check the generated code, the GHDL run will generate a waveform for the simulation that can be viewed with [GTKWave](http://gtkwave.sourceforge.net/).

As the testbench is written in standard compliant VHDL-93 (using the common Synopsys package for the testbench) it can also be loaded in all proprietary simulators, and tested for correctness.

The steps for loading the VHDL depends on the vendor tool, but generally it is a matter of choosing all the files in the `vhdl` folder for input into a "project" and then following each step (simulation, synthesis, implementation, bitstream).

In the current version of SME we focus mostly on the modelling and code-generation capabilities, so you need to use the vendor tools to set timing constraints and pin assignments for your circuit. When you have done that, the design can be written to an FPGA.
