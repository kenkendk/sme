using System;

namespace SME.AST
{
	/// <summary>
	/// Common base class for all AST items
	/// </summary>
	public abstract class ASTItem
	{
		/// <summary>
		/// The name of the item
		/// </summary>
		public string Name;

		/// <summary>
		/// The parent item
		/// </summary>
		public ASTItem Parent;
	}

	/// <summary>
	/// The top level AST description of an SME network
	/// </summary>
	public class Network : ASTItem
	{
		/// <summary>
		/// The assembly source
		/// </summary>
		public System.Reflection.Assembly Source;
		/// <summary>
		/// The network busses
		/// </summary>
		public Bus[] Busses;

		/// <summary>
		/// The sequence of processes
		/// </summary>
		public Process[] Processes;

		/// <summary>
		/// The list of constants
		/// </summary>
		public Constant[] Constants;
	}

	/// <summary>
	/// Description of a bus
	/// </summary>
	public class Bus : ASTItem
	{
		/// <summary>
		/// Gets a value indicating if the bus is a top-level input
		/// </summary>
		public bool IsTopLevelInput;
		/// <summary>
		/// Gets a value indicating if the bus is a top-level output
		/// </summary>
		public bool IsTopLevelOutput;
		/// <summary>
		/// Gets a value indicating if the bus is clocked
		/// </summary>
		public bool IsClocked;
		/// <summary>
		/// Gets a value indicating if the bus is internal
		/// </summary>
		public bool IsInternal;

		/// <summary>
		/// The signals carried by the bus
		/// </summary>
		public BusSignal[] Signals;
		/// <summary>
		/// The data type of this bus
		/// </summary>
		public Type SourceType;
	}

	/// <summary>
	/// A single data element
	/// </summary>
	public class DataElement : ASTItem
	{
		/// <summary>
		/// The data type of this element
		/// </summary>
		public Type Type;

		/// <summary>
		/// The type as a Cecil type reference
		/// </summary>
		public Mono.Cecil.TypeReference CecilType;

		/// <summary>
		/// The source obtained with reflection, if any
		/// </summary>
		public object Source;

		/// <summary>
		/// The default source value, if any.
		/// </summary>
		public object DefaultValue;
	}

	/// <summary>
	/// A signal-like data element
	/// </summary>
	public class Signal : DataElement
	{
	}

		/// <summary>
	/// A bus signal data element
	/// </summary>
	public class BusSignal : Signal
	{
	}

	/// <summary>
	/// A parameter that is the input to a function
	/// </summary>
	public class Parameter : DataElement
	{
	}

	/// <summary>
	/// A variable used in code
	/// </summary>
	public class Variable : DataElement
	{
	}

	/// <summary>
	/// A constant used in code
	/// </summary>
	public class Constant : DataElement
	{
		/// <summary>
		/// The array source, if this constant is a
		/// reference to an array length
		/// </summary>
		public DataElement ArrayLengthSource;
	}

	/// <summary>
	/// An SME process
	/// </summary>
	public class Process : ASTItem
	{
		/// <summary>
		/// The input busses
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
		/// The list of class-wide variables
		/// </summary>
		public Variable[] SharedVariables;

		/// <summary>
		/// The list of class-wide signals
		/// </summary>
		public Signal[] SharedSignals;

		/// <summary>
		/// The main method.
		/// </summary>
		public Method MainMethod;
		/// <summary>
		/// Called methods.
		/// </summary>
		public Method[] Methods;

		/// <summary>
		/// The type that defines this process for reflection
		/// </summary>
		public Type SourceType;

		/// <summary>
		/// The instance that this AST element is based on
		/// </summary>
		public SME.IProcess SourceInstance;

		/// <summary>
		/// The type that defines this process for Cecil
		/// </summary>
		public Mono.Cecil.TypeReference CecilType;

		/// <summary>
		/// A value indicating if the process is used for simulation only
		/// </summary>
		public bool IsSimulation;

		/// <summary>
		/// A value indicating if the process is clocked
		/// </summary>
		public bool IsClocked;
	}

	/// <summary>
	/// An SME method
	/// </summary>
	public class Method : ASTItem
	{
		/// <summary>
		/// The method obtained with Cecil
		/// </summary>
		public Mono.Cecil.MethodDefinition SourceMethod;
		/// <summary>
		/// The input parameters used by the function
		/// </summary>
		public Parameter[] Parameters;
		/// <summary>
		/// The variables used in the function
		/// </summary>
		public Variable[] Variables;
		/// <summary>
		/// The statements inside the method body.
		/// </summary>
		public Statement[] Statements;
		/// <summary>
		/// The return variable, if any
		/// </summary>
		public Variable ReturnVariable;
		/// <summary>
		/// A flag indicating if this method should be ignored
		/// </summary>
		public bool Ignore;
	}
}
