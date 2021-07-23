using System;
using System.Linq;
using SME;

namespace UnitTester
{

    /// <summary>
    /// Tests a bunch of assignment operators
    /// </summary>
    public class AssignmentOperators : Test
    {
        public AssignmentOperators()
        {
            outputs = new int[inputs.Length];
            int init_tmp = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                init_tmp -= inputs[i];
                init_tmp &= 0x7FFF_FFFE;
                outputs[i] = init_tmp;
            }
        }

        int tmp = 0;
        int max_val = int.MaxValue;

        protected override void OnTick()
        {
            output.valid = input.valid;
            tmp -= input.value;
            tmp &= 0x7FFF_FFFF;
            tmp *= 1;
            tmp %= max_val;
            tmp >>= 1;
            tmp <<= 1;
            tmp ^= 0;
            output.value = tmp;
        }
    }

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
            output.value = checked((int)(tmp & 0x7FFFFFFF));
        }
    }

    /// <summary>
    /// Tests whether a ternary (conditional) expression is rendered correctly
    /// </summary>
    public class ConditionalExpressionTest : Test
    {
        public ConditionalExpressionTest()
        {
            outputs = inputs.Select(x => x % 2 == 0 ? x : ~x).ToArray();
        }

        protected override void OnTick()
        {
            output.valid = input.valid;
            output.value = input.value % 2 == 0 ? input.value : ~input.value;
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
            output.valid = input.valid;
            output.value = input.value;
        }

        protected override void OnTick()
        {
            LocalMethod();
        }
    }

    /// <summary>
    /// Tests whether some operators are correctly translated. The list is
    /// based on what wasn't already hit during running the other samples.
    /// </summary>
    public class MissingOperators : Test
    {
        public MissingOperators()
        {
            outputs = inputs
                .Select(x => ~(x & 0x7FFF_FFFE))
                .ToArray();
        }

        protected override void OnTick()
        {
            output.valid = input.valid;
            int tmp = input.value & 0x7FFF_FFFF;
            tmp = tmp >> 1;
            tmp = tmp << 1;
            tmp++;
            tmp--;
            output.value = ~tmp;
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
            output.valid = input.valid;
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
    // TODO VendorAltera attribute
    // TODO VendorSimulation attribute
    // TODO Simulation.Render() -- rather than .BuildVHDL()
    // TODO Cast expression
    // TODO Direction expression
    // TODO Array of std_logic (bool)
    // TODO Array of std_logic_vector
    // TODO Primitive bool
    // TODO A network with only components
    // TODO top-level bool
    // TODO top-level signed
    // TODO top-level std_logic_vector
    // TODO top-level custom type
    // TODO Fields with no default value (So it should be chosen from the type)
    // TODO VHDL records (C# struct? or is it a dictionary?)
    // TODO assigning to a bus signal through a memberreference (structs in a signal?)
    // TODO assigning to a bus signal through an indexer expression
    // TODO Array field, which has no default value. (This shouldn't be allowed?)
    // TODO custom renderer
    // TODO ExternalComponent, add case for byte and sbyte
    // TODO Array as parameter
    // TODO Array property
    // TODO IntPtr ??
    // TODO UIntPtr ??

}