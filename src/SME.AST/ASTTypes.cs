using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SME.AST
{
    /// <summary>
    /// Common base class for all AST items.
    /// </summary>
    public abstract class ASTItem
    {
        /// <summary>
        /// The name of the item.
        /// </summary>
        public string Name;

        /// <summary>
        /// The parent item.
        /// </summary>
        public ASTItem Parent;
    }

    /// <summary>
    /// The top level AST description of an SME network.
    /// </summary>
    public class Network : ASTItem
    {
        /// <summary>
        /// The compilation associated with this network.
        /// </summary>
        public Compilation compilation;

        /// <summary>
        /// The network busses.
        /// </summary>
        public Bus[] Busses;

        /// <summary>
        /// The sequence of processes.
        /// </summary>
        public Process[] Processes;

        /// <summary>
        /// The list of constants.
        /// </summary>
        public Constant[] Constants;
    }

    /// <summary>
    /// Description of a bus.
    /// </summary>
    public class Bus : DataElement
    {
        /// <summary>
        /// Gets a value indicating if the bus is a top-level input.
        /// </summary>
        public bool IsTopLevelInput;
        /// <summary>
        /// Gets a value indicating if the bus is a top-level output.
        /// </summary>
        public bool IsTopLevelOutput;
        /// <summary>
        /// Gets a value indicating if the bus is clocked.
        /// </summary>
        public bool IsClocked;
        /// <summary>
        /// Gets a value indicating if the bus is internal.
        /// </summary>
        public bool IsInternal;

        /// <summary>
        /// The signals carried by the bus.
        /// </summary>
        public BusSignal[] Signals;
        /// <summary>
        /// The data type of this bus.
        /// </summary>
        public Type SourceType;
        /// <summary>
        /// The instance that this AST bus represents.
        /// </summary>
        public IBus[] SourceInstances;

        /// <summary>
        /// The name of the instance, if different from the type name.
        /// </summary>
        public string InstanceName;
    }

    /// <summary>
    /// A single data element.
    /// </summary>
    public class DataElement : ASTItem
    {
        /// <summary>
        /// The data type of this element.
        /// </summary>
        public Type Type;

        /// <summary>
        /// The Microsoft.Codeanalysis type.
        /// </summary>
        public ITypeSymbol MSCAType;

        /// <summary>
        /// The source obtained with reflection, if any.
        /// </summary>
        public object Source;

        /// <summary>
        /// The default source value, if any.
        /// </summary>
        public object DefaultValue;
    }

    /// <summary>
    /// A signal-like data element.
    /// </summary>
    public class Signal : DataElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Signal()
        {
        }

        /// <summary>
        /// Constructs a new signal.
        /// </summary>
        /// <param name="name">The signal name.</param>
        /// <param name="defaultValue">The default value.</param>
        public Signal(string name, object defaultValue = null)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// A bus signal data element.
    /// </summary>
    public class BusSignal : Signal
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BusSignal()
        {
        }

        /// <summary>
        /// Constructs a new signal.
        /// </summary>
        /// <param name="name">The signal name.</param>
        /// <param name="defaultValue">The default value.</param>
        public BusSignal(string name, object defaultValue = null)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// A parameter that is the input to a function.
    /// </summary>
    public class Parameter : DataElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Parameter()
        {
        }

        /// <summary>
        /// Constructs a new parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="defaultValue">The default value.</param>
        public Parameter(string name, object defaultValue = null)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// A variable used in code.
    /// </summary>
    public class Variable : DataElement
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Variable()
        {
        }

        /// <summary>
        /// Constructs a new variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="defaultValue">The default value.</param>
        public Variable(string name, object defaultValue = null)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
        }

        public bool isLoopIndex;
    }

    /// <summary>
    /// A constant used in code.
    /// </summary>
    public class Constant : DataElement
    {
        /// <summary>
        /// The array source, if this constant is a reference to an array length.
        /// </summary>
        public DataElement ArrayLengthSource;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Constant()
        {
        }

        /// <summary>
        /// Constructs a new variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="arrayLengthSource">The item indicating the array length source, if this constant references an array length.</param>
        public Constant(string name, object defaultValue = null, DataElement arrayLengthSource = null)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.ArrayLengthSource = arrayLengthSource;
        }
    }

    /// <summary>
    /// An SME process.
    /// </summary>
    public class Process : ASTItem
    {
        /// <summary>
        /// The input busses.
        /// </summary>
        public Bus[] InputBusses;
        /// <summary>
        /// The output busses.
        /// </summary>
        public Bus[] OutputBusses;
        /// <summary>
        /// The internal busses.
        /// </summary>
        public Bus[] InternalBusses;

        /// <summary>
        /// The list of class-wide variables.
        /// </summary>
        public Variable[] SharedVariables;

        /// <summary>
        /// The list of class-wide signals.
        /// </summary>
        public Signal[] SharedSignals;

        /// <summary>
        /// The list of internal class-wide data elements.
        /// </summary>
        public DataElement[] InternalDataElements;

        /// <summary>
        /// The main method.
        /// </summary>
        public Method MainMethod;
        /// <summary>
        /// Called methods.
        /// </summary>
        public Method[] Methods;

        /// <summary>
        /// The type that defines this process for reflection.
        /// </summary>
        public Type SourceType;

        /// <summary>
        /// The instance that this AST element is based on.
        /// </summary>
        public SME.ProcessMetadata SourceInstance;

        /// <summary>
        /// The Microsoft.Codeanalysis type.
        /// </summary>
        public ITypeSymbol MSCAType;

        /// <summary>
        /// A value indicating if the process is used for simulation only.
        /// </summary>
        public bool IsSimulation;

        /// <summary>
        /// A value indicating if the process is clocked.
        /// </summary>
        public bool IsClocked;

        /// <summary>
        /// The name of the instance, if different from the type name.
        /// </summary>
        public string InstanceName;

        /// <summary>
        /// A lookup table with the names of the busses within this process.
        /// </summary>
        public readonly Dictionary<Bus, string> LocalBusNames = new Dictionary<Bus, string>();

        public Constant[] SharedConstants;
    }

    /// <summary>
    /// An SME method.
    /// </summary>
    public class Method : ASTItem
    {
        /// <summary>
        /// The Microsoft.Codeanalysis type.
        /// </summary>
        public MethodDeclarationSyntax MSCAMethod;
        /// <summary>
        /// The return type of the method.
        /// </summary>
        public ITypeSymbol MSCAReturnType;
        /// <summary>
        /// The data flow analysis of the method.
        /// </summary>
        public DataFlowAnalysis MSCAFlow;
        /// <summary>
        /// The input parameters used by the method.
        /// </summary>
        public Parameter[] Parameters;
        /// <summary>
        /// All variables used in all scopes in the method.
        /// </summary>
        public Variable[] AllVariables;
        /// <summary>
        /// Variables used in the top scope in the method.
        /// </summary>
        public Variable[] Variables;
        /// <summary>
        /// The statements inside the method body.
        /// </summary>
        public Statement[] Statements;
        /// <summary>
        /// The return variable, if any.
        /// </summary>
        public Variable ReturnVariable;
        /// <summary>
        /// A flag indicating if this method should be ignored.
        /// </summary>
        public bool Ignore;
        /// <summary>
        /// A flag indicating if the method is a state machine.
        /// </summary>
        public bool IsStateMachine;
    }
}
