using System;
using System.Linq;
using SME;

namespace UnitTester
{

    // TODO Currently doesn't work, since this isn't captured properly
    /*public class WriteViolationExceptionTestA : ExceptionTest
    {
        public WriteViolationExceptionTestA()
        {
            exception_to_catch = new WriteViolationException("");
            var bus = Scope.CreateBus<ValueBus>();
            var proc_a = new WritingProc();
            var proc_b = new WritingProc();
            proc_a.output = bus;
            proc_b.output = bus;
        }

        [ClockedProcess]
        public class WritingProc : SimpleProcess
        {
            [OutputBus] public ValueBus output;

            protected override void OnTick()
            {
                output.value = 42;
            }
        }
    }*/

    public class WriteViolationExceptionTestB : ExceptionTest
    {
        public WriteViolationExceptionTestB()
        {
            exception_to_catch = new WriteViolationException("");

            var proc_a = new ClockedProc();
            var proc_b = new UnclockedProc();

            var bus_a = Scope.CreateBus<ValueBus>();
            var bus_b = Scope.CreateBus<ClockedValueBus>();

            proc_a.outputa = bus_a;
            proc_a.outputb = bus_b;
            proc_b.input = bus_a;
            proc_b.output = bus_b;
        }

        public class UnclockedProc : SimpleProcess
        {
            [InputBus] public ValueBus input;
            [OutputBus] public ClockedValueBus output;

            protected override void OnTick()
            {
                output.value = 33;
            }
        }

        [ClockedProcess]
        public class ClockedProc : SimpleProcess
        {
            [InputBus] public ValueBus input;
            [OutputBus] public ValueBus outputa;
            [OutputBus] public ClockedValueBus outputb;

            protected override void OnTick()
            {
                outputa.value = 42;
                outputb.value = 42;
            }
        }
    }

    // Missing exception tests
    // TODO Non-constant static variable, which is not allowed in SME, as it implies multiple instances share a variable.
    // TODO Create a bus from a non interface type (Exception)
    // TODO Create a bus with a native array rather than an IFixedArray (Actually, can't this type be removed, now that we're running a compiler instead of a decompiler? The biggest challenge is whether or not the array is fixed in size, and we should be able to check this through the compiler now.) (Exception)
    // TODO Create a bus signal, which is not a value type (Exception)
    // TODO Create a bus without any signals. (Exception)
    // TODO Create a bus signal, which has multiple default values (Exception)
    // TODO Create a bus signal with IFixedArray without the length attribute (Exception)
    // TODO Write to a non-existant signal on a bus (Exception) (Wouldn't this be handled by the C# type system?)
    // TODO Read from a signal, which has not been written yet (Exception)
    // TODO Read from a non-existant signal on a bus (Exception) (Wouldn't this be handled by the C# type system?)
    // TODO Create a bus, whose interface definition contains a method definition. (Exception)
    // TODO Create a bus, which has an index parameter on a property ??? (Exception)
    // TODO Non-clocked process without any parents (Exception)
    // TODO Non-clocked process without any parents, which also writes to some output bus? (Exception)
    // TODO A case where all processes don't trigger. (Exception) (Actually this shouldn't even be able to be provoked?)
    // TODO Multiple processes writing to the same index in an IFixedArray (Exception)
    // TODO Read a value on an IFixedArray before it has been written. (Exception)
    // TODO Create a process without an active simulation (Exception)
    // TODO Create a bus with both InternalBus and TopLevelBus attributes (NotSupportedException)
    // TODO Set the name of an internal bus (ArgumentException)
    // TODO Set the name of a singletonbus ?? (ArgumentException)
    // TODO CreateOrLoadBus cannot create a bus of type IBus or SingletonBus ?? (ArgumentException)
    // TODO Calling CreateOrLoadBus with a bus that shares the name of another bus, but not the same type?? (InvalidOperationException)
    // TODO Setting a scope without a key ? (InvalidOperationException)
    // TODO Starting multiple simulations (InvalidOperationException)
    // TODO Adding a null preloader to a simulation (ArgumentNullException)
    // TODO Adding a null postloader to a simulation (ArgumentNullException)
    // TODO Adding a null prerunner to a simulation (ArgumentNullException)
    // TODO Adding a null postrunner to a simulation (ArgumentNullException)
    // TODO Adding a null postclockrunner to a simulation (ArgumentNullException)
    // TODO Simulation.Run(IProcess[] processes) with an empty processes collection
    // TODO Start a new scope from an ASTItem, where the item is null (ArgumentNullException)
    // TODO Finish a scope with an ASTItem, where the item is null
    // TODO Try to finish a scope, which isn't the last scope in a set of nested scopes
    // TODO Have a network with no processes. (Exception)
    // TODO Have the decompile flag set on an unsupported process type (Exception)
    // TODO Waiting on something other than ClockAsync()
    // TODO IndexerExpression with multiple index arguments (Exception)
    // TODO For loop which increments more than 1 (Exception)
    // TODO For loop with multiple iteration variables (Exception)
    // TODO For loop with no incrementor expression (Exception)
    // TODO State process with a for loop without await statement, which is also not a static for loop. (Exception)
    // TODO TrueDualPortMemory where both ports write to the same address (Exception)
    // TODO Unsupported statement (Exception) SME.VHDL.RenderHelper line 167
    // TODO Return statement in a process, where it returns some value (Exception)
    // TODO Unsupported expression type (Exception) SME.VHDL.RenderHelper line 375
    // TODO UnclockedCycleException ?
}
