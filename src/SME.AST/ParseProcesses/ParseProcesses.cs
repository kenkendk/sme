using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SME.AST
{
	// This partial part of the parser deals with the Reflection side of the parsing

	/// <summary>
	/// The main entry point for building an AST of an SME network
	/// </summary>
	public partial class ParseProcesses
	{
		/// <summary>
		/// A class that contains intermediate information collected while traversing the network
		/// </summary>
		protected class NetworkState : Network
		{
			/// <summary>
			/// The table of all the constants in a convenient lookup table
			/// </summary>
			public readonly Dictionary<Mono.Cecil.FieldDefinition, Constant> ConstantLookup = new Dictionary<Mono.Cecil.FieldDefinition, Constant>();
			/// <summary>
			/// A variable counter, used to make unique variable names
			/// </summary>
			public int VariableCount;
			/// <summary>
			/// A lookup table of bus instances
			/// </summary>
			public readonly Dictionary<Type, Bus> BusInstanceLookup = new Dictionary<Type, Bus>();
		}

		/// <summary>
		/// A class that contains intermediate information collected while traversion a process
		/// </summary>
		protected class ProcessState : Process
		{
			/// <summary>
			/// The variables shared in the current process
			/// </summary>
			public readonly Dictionary<string, Bus> BusInstances = new Dictionary<string, Bus>();

			/// <summary>
			/// The variables shared in the current process
			/// </summary>
			public readonly Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();
			/// <summary>
			/// The signals shared in the current process
			/// </summary>
			public readonly Dictionary<string, Signal> Signals = new Dictionary<string, Signal>();
			/// <summary>
			/// The list of methods to process
			/// </summary>
			public readonly Queue<Tuple<AST.Statement, MethodState, AST.InvocationExpression>> MethodTargets = new Queue<Tuple<AST.Statement, MethodState, AST.InvocationExpression>>();
			/// <summary>
			/// The decompiler context.
			/// </summary>
			public ICSharpCode.Decompiler.DecompilerContext DecompilerContext;
			/// <summary>
			/// The import statements
			/// </summary>
			public readonly List<string> Imports = new List<string>();
			/// <summary>
			/// A flag indicating if the method should be decompiled
			/// </summary>
			public bool Decompile;
		}

		/// <summary>
		/// A class that contains intermediate information collected while traversing a method
		/// </summary>
		protected class MethodState : Method
		{
			/// <summary>
			/// The local variables
			/// </summary>
			public readonly Dictionary<string, Variable> LocalVariables = new Dictionary<string, Variable>();
		
			/// <summary>
			/// The local variables
			/// </summary>
			public readonly Dictionary<string, string> LocalRenames = new Dictionary<string, string>();
		}

		/// <summary>
		/// Static method for building an AST
		/// </summary>
		/// <returns>The network.</returns>
		/// <param name="processes">The processes to build the AST for.</param>
		/// <param name="decompile">Set to <c>true</c> to enable decompilation of the IL code.</param>
		public static Network BuildNetwork(IEnumerable<IProcess> processes, bool decompile = false)
		{
			return new ParseProcesses().Parse(processes, decompile);
		}

		/// <summary>
		/// Static method for building an AST with a custom subclass
		/// </summary>
		/// <returns>The network.</returns>
		/// <param name="processes">The processes to build the AST for.</param>
		/// <param name="decompile">Set to <c>true</c> to enable decompilation of the IL code.</param>
		public static Network BuildNetwork<T>(IEnumerable<IProcess> processes, bool decompile = false)
			where T : ParseProcesses, new()
		{
			return new T().Parse(processes, decompile);
		}


		/// <summary>
		/// Builds an AST by inspecting the processes
		/// </summary>
		/// <param name="processes">The processes to build the AST for.</param>
		/// <param name="decompile">Set to <c>true</c> to enable decompilation of the IL code.</param>
		public virtual Network Parse(IEnumerable<IProcess> processes, bool decompile = false)
		{
			var sourceasm = processes.First().GetType().Assembly;
			var network = new NetworkState()
			{
				Source = sourceasm,
				Name = sourceasm.GetName().Name,
				Parent = null
			};

			network.Processes = processes
				.Where(n => 
				       n.GetType().GetCustomAttributes(typeof(IgnoreAttribute), true).FirstOrDefault() == null
							&&
						!(n is SimulationProcess))
				.Select(x => Parse(network, x))
				.ToArray();

			if (network.Processes.Length == 0)
				throw new Exception("No processes were found in the list");
			
			//network.Processes = processes.Select(x => Parse(network, x)).ToArray();

			network.Busses = network.Processes.SelectMany(x => x.InternalBusses.Union(x.InputBusses).Union(x.OutputBusses)).Distinct().ToArray();
			network.Constants = network.ConstantLookup.Values.ToArray();

			if (decompile)
			{
				foreach (var pr in network.Processes.Cast<ProcessState>().Where(x => x.Decompile))
				{
					var st = pr.SourceType;
					var method = st.GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, new Type[] { }, null);
					if (method == null)
						throw new Exception($"Could not find the \"OnTick\" method in class: {st.FullName}");

					Decompile(network, pr, method);
				}
			}

			network.Constants = network.ConstantLookup.Values.ToArray();

			// Patch up all types if they are missing
			foreach (var el in network.All().OfType<DataElement>().Where(x => x.CecilType == null))
				el.CecilType = LoadType(el.Type);

			foreach (var el in network.Constants.Where(x => x.DefaultValue == null))
			{
				if (el.Source is Mono.Cecil.FieldDefinition)
				{
					var ft = el.Source as Mono.Cecil.FieldDefinition;
					if (ft.IsStatic && ft.IsInitOnly)
						DecompileStaticInitializer(network, ft.DeclaringType);
				}
			}

			return network;
		}

		/// <summary>
		/// Attempts to remove redundant prefixes to names
		/// </summary>
		/// <returns>The name without the prefix.</returns>
		/// <param name="network">The top-level network instance.</param>
		/// <param name="fullname">The name to shorten.</param>
		protected virtual string NameWithoutPrefix(NetworkState network, string fullname)
		{
			if (fullname.StartsWith(network.Name + ".", StringComparison.Ordinal) && fullname.Length > network.Name.Length - 1)
				return fullname.Substring(network.Name.Length + 1);

			return fullname;			
		}

		/// <summary>
		/// Parses a process and builds an AST component for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="process">The process to build the AST for.</param>
		protected virtual Process Parse(NetworkState network, IProcess process)
		{
			var st = process.GetType();

			var inputbusses = process.InputBusses.Union(process.ClockedInputBusses).Distinct().ToArray();
			var outputbusses = process.OutputBusses.Distinct().ToArray();

			var res = new ProcessState()
			{
				Name = NameWithoutPrefix(network, st.FullName),
				SourceType = st,
				CecilType = LoadType(st),
				SourceInstance = process,
				Parent = network,
				IsSimulation = process is SimulationProcess,
				IsClocked = st.GetCustomAttribute<ClockedProcessAttribute>() != null,
				Decompile = typeof(SimpleProcess).IsAssignableFrom(st)
					&&
					!st.HasAttribute<SuppressOutputAttribute>()
					&&
					!st.HasAttribute<SuppressBodyAttribute>()
			};

			res.InputBusses = inputbusses.Select(x => Parse(network, res, x)).ToArray();
			res.OutputBusses = outputbusses.Select(x => Parse(network, res, x)).ToArray();
			res.InternalBusses = process.InternalBusses.Select(x => Parse(network, res, x)).ToArray();
			foreach (var ib in res.InternalBusses)
				ib.IsInternal = true;


			if (res.Decompile)
			{
				var proctype = res.CecilType.Resolve();

				// Register all variables
				foreach (var f in proctype.Fields)
					if (f.FieldType.IsBusType())
						RegisterBusReference(network, res, f);
					else
						RegisterVariable(network, res, f);
			}

			res.SharedSignals = res.Signals.Values.ToArray();
			res.SharedVariables = res.Variables.Values.ToArray();
			return res;
		}

		/// <summary>
		/// Parses a bus and builds an AST component for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the bus is located.</param>
		/// <param name="bus">The bus to build the AST for.</param>
		protected virtual Bus Parse(NetworkState network, ProcessState proc, IBus bus)
		{
			var st = bus.BusType;

			if (network.BusInstanceLookup.ContainsKey(st))
				return network.BusInstanceLookup[st];

			var res = new Bus()
			{
				Name = NameWithoutPrefix(network, st.FullName),
				SourceType = st,
				IsTopLevelInput = st.HasAttribute<TopLevelInputBusAttribute>(),
				IsTopLevelOutput = st.HasAttribute<TopLevelOutputBusAttribute>(),
				IsClocked = st.HasAttribute<ClockedBusAttribute>(),
				IsInternal = st.HasAttribute<InternalBusAttribute>(),
				Parent = network
			};

			network.BusInstanceLookup[st] = res;
			res.Signals = st.GetPropertiesRecursive().Where(x => x.DeclaringType != typeof(IBus)).Select(x => Parse(network, proc, res, x)).ToArray();
			return res;
		}

		/// <summary>
		/// Parses the bus property and builds an AST component for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the bus is located.</param>
		/// <param name="bus">The bus element.</param>
		/// <param name="pi">The property to build the AST for.</param>
		protected virtual BusSignal Parse(NetworkState network, ProcessState proc, Bus bus, PropertyInfo pi)
		{
			var st = pi.PropertyType;
			object defaultvalue =
					pi.PropertyType.IsValueType
					  ? Activator.CreateInstance(pi.PropertyType)
					  : null;

			var initvalattr = pi.GetCustomAttribute(typeof(InitialValueAttribute), true) as InitialValueAttribute;

			if (initvalattr != null && initvalattr.Value != null)
				defaultvalue = initvalattr.Value;

			return new BusSignal()
			{
				Name = pi.Name,
				Type = pi.PropertyType,
				Source = pi,
				Parent = bus,
				DefaultValue = defaultvalue,
				CecilType = LoadType(pi.PropertyType)
			};
		}

		/// <summary>
		/// Parses the specified parameter and builds an AST component for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method that the parameter belongs to.</param>
		/// <param name="p">The parameter to build an AST for.</param>
		protected virtual Parameter Parse(NetworkState network, ProcessState proc, Method method, ParameterInfo p)
		{
			return new Parameter()
			{
				Name = p.Name,
				Source = p,
				Type = p.ParameterType,
				Parent = method
			};
		}

		/// <summary>
		/// Parse the specified local variable and builds and AST component for it
		/// </summary>
		/// <param name="network">The top-level network.</param>
		/// <param name="proc">The process where the method is located.</param>
		/// <param name="method">The method that the parameter belongs to.</param>
		/// <param name="lv">The local variable to parse.</param>
		protected virtual Variable Parse(NetworkState network, ProcessState proc, Method method, LocalVariableInfo lv)
		{
			return new Variable()
			{
				Name = "local_" + lv.LocalIndex,
				Source = lv,
				Type = lv.LocalType,
				Parent = method
			};
		}

	}
}
