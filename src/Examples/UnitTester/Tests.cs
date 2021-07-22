using System;
using System.Linq;
using SME;

namespace UnitTester
{

    /// <summary>
    /// Tests whether SME can translate a member reference to base
    /// </summary>
    public class BaseMemberReference : ThisMemberReference
    {
        public BaseMemberReference() : base() { }

        protected override void OnTick()
        {
            output.valid = base.valid;
            output.value = base.value + base.const_val;

            base.valid = input.valid;
            base.value = input.value;
        }
    }

    /// <summary>
    /// Tests whether checked and unchecked expressions are correctly handled.
    /// </summary>
    public class CheckedAndUnchecked : Test
    {
        protected override void OnTick()
        {
            output.valid = input.valid;
            uint tmp = unchecked((uint)input.value);
            output.value = checked((int)tmp);
        }
    }

    /// <summary>
    /// Tests whether a method is correctly ignored, so not rendered in VHDL.
    /// </summary>
    public class IgnoreMethod : Test
    {
        [Ignore]
        private int BoringMethod(int input)
        {
            return input + 42;
        }
    }

    /// <summary>
    /// Tests an invocation expression
    /// </summary>
    public class InvocationExpression : Test
    {
        private void LocalMethod()
        {
            output.valid = true;
            output.value = input.value;
        }

        protected override void OnTick()
        {
            LocalMethod();
        }
    }

    /// <summary>
    /// Tests whether a public readonly or constant variable is correctly
    /// parsed.
    /// </summary>
    public class PublicReadonlyOrConstant : Test
    {
        public PublicReadonlyOrConstant()
        {
            outputs = inputs.Select(x => x + ro_value + c_value).ToArray();
        }

        public readonly int ro_value = 42;
        public const int c_value = 33;

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.value = input.value + ro_value + c_value;
        }
    }

    /// <summary>
    /// Tests whether SME can translate a member reference with process type
    /// prefix
    /// </summary>
    public class SelfTypeMemberReference : Test
    {
        public SelfTypeMemberReference()
        {
            outputs = inputs.Select(x => x + const_val).ToArray();
        }

        private static readonly int const_val = 1;

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.value = input.value + SelfTypeMemberReference.const_val;
        }
    }

    /// <summary>
    /// Tests whether SimulationOnly() is truely ignored.
    /// </summary>
    public class SimulationOnlyTest : Test
    {
        protected override void OnTick()
        {
            output.valid = input.valid;
            output.value = input.value;
            SimulationOnly(() => inputs[0] = inputs[0]);
        }
    }

    /// <summary>
    /// Helper class containing static fields and methods
    /// </summary>
    public class StaticHelper
    {
        public static int StaticMethod(int input) // TODO Static methods outside of processes aren't rendered correctly
        {
            return input + 42;
        }
    }

    /// <summary>
    /// Member access to a static method
    /// </summary>
    public class StaticMethod : Test
    {
        public StaticMethod()
        {
            outputs = inputs.Select(x => x + 42).ToArray();
        }

        public static int LocalStaticMethod(int input) // TODO generated method can be simplified
        {
            return input + 42;
        }

        protected override void OnTick()
        {
            output.valid = true;
            output.value = StaticMethod.LocalStaticMethod(input.value);
        }
    }

    /// <summary>
    /// Tests whether SME can translate a member reference to this
    /// </summary>
    public class ThisMemberReference : Test
    {
        public ThisMemberReference()
        {
            outputs = inputs.Select(x => x + const_val).ToArray();
        }

        protected bool valid = false;
        protected int  value = 0;
        protected readonly int const_val = 1;

        protected override void OnTick()
        {
            output.valid = this.valid;
            output.value = this.value + const_val;

            this.valid = input.valid;
            this.value = input.value;
        }
    }

    // Missing tests
    // TODO constant array
    // TODO BusProperty?
    // TODO IRuntimeBus.Manager ?
    // TODO divided clock ?
    // TODO Using clocked buses
    // TODO array of buses
    // TODO auto loading buses (or rather: remove this feature?)
    // TODO Scope.RegisterBus() ?
    // TODO Scope.LoadBus() ?
    // TODO SingletonBus ?
    // TODO Scope.FindProcess() ?
    // TODO Scope.RecursiveLookup() ?
    // TODO Scope.ScopeKey ?? It should return the root scope, if there's no scope key, so I'm guessing multiple scopes?
    // TODO Adding a preloader to a simulation
    // TODO Simulation.Run(Assembly) (should be removed?)
    // TODO Generic typing with own types (SME.AST.ParseProcesses.ResolveGenericType)
    // TODO build an AST with a custom subclass (Why would you do this?)
    // TODO Parse an parameter?? (SME.AST.ParseProcesses)
    // TODO Parse a variable?? (SME.AST.ParseProcesses)
    // TODO ArrayCreationExpression
    // TODO UsingStatement
    // TODO GotoStatement
    // TODO LabeledStatement
    // TODO Local decleration of a bus type (Is this allowed?? Maybe in an aliasing sort of fashion)
    // TODO Local declaration with multiple values
    // TODO ResolveArrayLengthOrPrimitive() (SME.AST.ParseProcessStatement)
    // TODO arithmetic operations with float or double (Although it isn't fully implemented yet.)
    // TODO Generic types
    // TODO Load type array? (I'm guessing custom types)
    // TODO Bool type
    // TODO sbyte type
    // TODO long type
    // TODO float type
    // TODO double type
    // TODO SME.Signal
    // TODO Custom type inside an OnTick
    // TODO public static constant ?
    // TODO TryLocateElement(Method) (SME.AST)
    // TODO Variable in Ontick (hasn't this been done??)
    // TODO Property
    // TODO SME.AST.ParseProcesses.LocateBus() ??
    // TODO State process without any awaits
    // TODO State process with a for loop without any awaits
    // TODO State process with a for loop, whose body isn't a block statement
    // TODO State process which has a series of normal statements, amongst which one has to be an await
    // TODO SME.AST.BuildStateMachine.ToBlockStatement() with an empty set of statements given??
    // TODO State process which has a cycle from while loops. In other words, it tries to go back to itself, when it is trying to inline statements, which it can't (and shouldn't).
    // TODO Switch statement, which uses goto
    // TODO Self casting (SME.AST.Transform.RemoveDoubleCast)
    // TODO SME.BusSignal
    // TODO SME.Parameter
    // TODO WrappingExpression
    // TODO EmptyArrayCreateExpression
    // TODO JSON trace ??
    // TODO -=
    // TODO *=
    // TODO %=
    // TODO <<=
    // TODO >>=
    // TODO &=
    // TODO ^=
    // TODO Trigger the warning in SimpleDualPortMemory
    // TODO Trigger the warning in TrueDualPortMemory
    // TODO Simulation.BuildGraph(render_buses: false)
    // TODO Trigger a trace file, which contains U and X
    // TODO enum in trace (on buses)
    // TODO sbyte in trace (on buses)
    // TODO short in trace (on buses)
    // TODO ushort in trace (on buses)
    // TODO long in trace (on buses)
    // TODO native components, rather than inferred
    // TODO SME.VHDL.BaseTemplate
    // TODO Shared signals ??
    // TODO Buses, which are both in and out buses??
    // TODO Non-clocked process with multiple processes writing to it's buses
    // TODO Intermediate signals, which are also topleveloutput
    // TODO (public?) Constant field in a nested process
    // TODO non-static for loop
    // TODO VHDLTypes.INTEGER << VHDLTypes.INTEGER or VHDLTypes.INTEGER >> VHDLTypes.INTEGER
    // TODO << CastExpression()
    // TODO RemoveNonstaticSwitchLabels so a switch with conversion expressions in their case?
    // TODO Unary operator, which is not a ++ or --
    // TODO UntagleElseStatements
    // TODO VHDLComponentAttribute
    // TODO All of the VHDL.* types
    // TODO A process with the same name as the project. (I'm sure this is normally hit, but not during testing.)
    // TODO A bus signal with a keyword as name (SME.VHDL.Naming line 119)
    // TODO << operator
    // TODO >> operator
    // TODO ~ operator
    // TODO ++ operator
    // TODO -- operator
    // TODO VendorAltera attribute
    // TODO VendorSimulation attribute
    // TODO Simulation.Render() -- rather than .BuildVHDL()
    // TODO Function without a return variable
    // TODO Temporary variables
    // TODO Conditional expression
    // TODO Cast expression
    // TODO Direction expression
    // TODO Array of std_logic (bool)
    // TODO Array of std_logic_vector
    // TODO Function without arguments
    // TODO Primitive bool
    // TODO A network with only components
    // TODO top-level bool
    // TODO top-level signed
    // TODO top-level std_logic_vector
    // TODO top-level custom type
    // TODO Fields with no default value (So it should be chosen from the type)
    // TODO VHDL records (C# struct? or is it a dictionary?)
    // TODO non-array constants
    // TODO assigning to a bus signal through a memberreference (structs in a signal?)
    // TODO assigning to a bus signal through an indexer expression
    // TODO Array field, which has no default value. (This shouldn't be allowed?)
    // TODO custom renderer
    // TODO ExternalComponent, add case for byte and sbyte
    // TODO Array as parameter
    // TODO Array property
    // TODO IntPtr ??
    // TODO UIntPtr ??

    // Missing exception tests
    // TODO Two, or more, processes writing to the same bus field (WriteViolationException)
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

}