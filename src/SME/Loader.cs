using SME;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SME
{
	/// <summary>
	/// The class that initializes and starts all defined processes as well as loads all busses
	/// </summary>
	public static class Loader
	{
		/// <summary>
		/// Internal list of started items
		/// </summary>
		private static List<Task> m_startedItems = new List<Task>();

		/// <summary>
		/// Helper variable to toggle assignment debug output
		/// </summary>
		public static bool DebugBusAssignments = false;

		/// <summary>
		/// Loads and starts all IProcess items in the assemblies
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="assemblies">The assemblies to load.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadAssemblies(Clock clock, params Assembly[] assemblies)
		{
			return LoadAssemblies(assemblies, clock);
		}

		/// <summary>
		/// Loads and starts all IProcess items in the assemblies
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="assemblies">The assemblies to load.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadAssemblies(IEnumerable<Assembly> assemblies, Clock clock = null)
		{
			return LoadAssemblies(assemblies.ToArray(), clock);
		}

		/// <summary>
		/// Loads and starts all IProcess items in the assemblies
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="assemblies">The assemblies to load.</param>
		public static IEnumerable<IProcess> LoadAssemblies(params Assembly[] assemblies)
		{
			return LoadAssemblies(assemblies, null);
		}

		/// <summary>
		/// Loads and starts all IProcess items in an assembly
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="assembly">The assemblies to load.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadAssemblies(Assembly assembly = null, Clock clock = null)
		{
			return LoadAssemblies(new[] { assembly ?? Assembly.GetCallingAssembly() }, clock);
		}

		/// <summary>
		/// Loads and starts all IProcess items in the assemblies
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="assemblies">The assembly to load from. Defaults to the calling assembly.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadAssemblies(Assembly[] assemblies, Clock clock)
		{
			clock = clock ?? Clock.DefaultClock;

			var self = assemblies.Where(x => x == Assembly.GetExecutingAssembly()).FirstOrDefault();
			if (self != null)
				throw new Exception(string.Format("Attempted to load the {0} assembly itself, please specify the correct assembly when calling the {1} class", self.FullName, typeof(Loader).FullName));

			return LoadItems(assemblies.SelectMany(x => x.GetTypes()));
		}

		/// <summary>
		/// Loads and starts a specific types
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="types">The The types to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItems(Clock clock, params Type[] types)
		{
			return LoadItems(types, clock);
		}

		/// <summary>
		/// Loads and starts a specific types
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="types">The The types to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItems(IEnumerable<Type> types, Clock clock = null)
		{
			return LoadItems(types.ToArray(), clock);
		}

		/// <summary>
		/// Loads and starts a specific type
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="types">The The types to start.</param>
		public static IEnumerable<IProcess> LoadItems(params Type[] types)
		{
			return LoadItems(types, null);
		}

		/// <summary>
		/// Loads and starts a specific type
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="type">The The type to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItem(Type type, Clock clock = null)
		{
			return LoadItems(new[] { type }, clock);
		}

		/// <summary>
		/// Loads and starts a specific type
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="types">The The type to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItems(Type[] types, Clock clock)
		{
			clock = clock ?? Clock.DefaultClock;

			// We build it as a list to ensure we execute it all,
			// even if the Enumerable is not iterated
			return LoadItems(
				types
				.Where(x => typeof(IProcess).IsAssignableFrom(x) && !x.IsAbstract && x.IsClass)
				.Select(x =>
				{
					IProcess p = null;
					if (x.GetConstructor(new Type[] { typeof(Clock) }) != null)
						p = Activator.CreateInstance(x, new object[] { clock }, null) as IProcess;
					else if (x.GetConstructor(new Type[] { }) != null)
						p = Activator.CreateInstance(x, new object[] { }, null) as IProcess;

					return p;
				})
				.Where(x => x != null),
				clock
			).ToList();
		}

		/// <summary>
		/// Loads and starts the supplied processes
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="items">The The types to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItems(Clock clock, params IProcess[] items)
		{
			return LoadItems(items, clock);
		}

		/// <summary>
		/// Loads and starts the supplied processes
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="items">The The types to start.</param>
		public static IEnumerable<IProcess> LoadItems(params IProcess[] items)
		{
			return LoadItems(items, null);
		}

		/// <summary>
		/// Loads and starts the supplied processes
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="items">The The types to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItems(IEnumerable<IProcess> items, Clock clock = null)
		{
			return LoadItems(items.ToArray(), clock);
		}

		/// <summary>
		/// Loads and starts the supplied process
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="item">The The type to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItem(IProcess item, Clock clock = null)
		{
			return LoadItems(new[] { item }, clock);
		}

		/// <summary>
		/// Loads and starts the supplied processes
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="items">The The type to start.</param>
		/// <param name="clock">Optionally specify which clock to use.</param>
		public static IEnumerable<IProcess> LoadItems(IProcess[] items, Clock clock)
		{
			clock = clock ?? Clock.DefaultClock;

			var lst = new List<IProcess>();

			foreach (var item in items)
			{
				var p = (IProcess)AutoloadBusses(item, clock);
				StartTask(p);

				if (p != null)
					lst.Add(p);
			}

			return lst;
		}

		/// <summary>
		/// Starts a process and registers it as running
		/// </summary>
		/// <param name="f">The process to start.</param>
		public static void StartTask(IProcess f)
		{
			m_startedItems.Add(f.Run());
		}

		/// <summary>
		/// Gets all IBus fields in the specified type
		/// </summary>
		/// <returns>The bus fields.</returns>
		/// <param name="t">The type to examine.</param>
		public static IEnumerable<FieldInfo> GetBusFields(Type t)
		{
			return t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(n => typeof(IBus).IsAssignableFrom(n.FieldType));
		}

		/// <summary>
		/// Loads all IBus interface fields for the given object
		/// </summary>
		/// <returns>The object with the busses loaded.</returns>
		/// <param name="o">The object instance to load busses for.</param>
		/// <param name="clock">The clock that the busses operate under.</param>
		public static object AutoloadBusses(object o, Clock clock)
		{
			string default_namespace = null;
			var nsattr = o.GetType().GetCustomAttributes(typeof(NamespaceAttribute)).FirstOrDefault() as NamespaceAttribute;
			if (nsattr != null)
				default_namespace = nsattr.Name;

			if (DebugBusAssignments)
				Console.WriteLine("Autoloading busses for {0}", o.GetType().FullName);

			foreach (var f in GetBusFields(o.GetType()))
				if (f.GetValue(o) == null)
				{
					if (f.GetCustomAttributes(typeof(NoAutoLoadAttribute), true).FirstOrDefault() != null)
					{
						if (DebugBusAssignments)
							Console.WriteLine("No auto-load for field {0}", f.Name);
						
						continue;
					}
						
					var @namespace = default_namespace;
					var ns = f.GetCustomAttributes(typeof(NamespaceAttribute)).FirstOrDefault() as NamespaceAttribute;
					if (ns != null)
						@namespace = ns.Name;

					var internalBus = (f.GetCustomAttributes(typeof(InternalBusAttribute)).FirstOrDefault() as InternalBusAttribute != null);

					var bus = BusManager.GetBus(f.FieldType, clock, @namespace, internalBus);
					if (DebugBusAssignments)
						Console.WriteLine("Setting field {0}.{1} = {2}:{3}:{4} -> {5}", f.DeclaringType.Name, f.Name, @namespace, bus, f.FieldType.FullName, bus.GetHashCode());

					f.SetValue(o, BusManager.GetBus(f.FieldType, clock, @namespace, internalBus));
				}

			return o;
		}

		/// <summary>
		/// Checks if any registered processes have crashed and throws the appropriate exception
		/// </summary>
		public static void CheckForCrashes()
		{
			var all = from n in m_startedItems
				where n.Exception != null
				from x in n.Exception.InnerExceptions
				select x;

			if (all.Any(x => true))
				throw new AggregateException(all);
		}

		/// <summary>
		/// Checks if any registered processes have exited
		/// </summary>
		/// <returns><c>true</c>, if any processes where compled, <c>false</c> otherwise.</returns>
		public static bool CheckForCompletion()
		{
			return !m_startedItems.Where(x => x.IsCompleted).Any();
		}

		/// <summary>
		/// Runs all the specified component instances until a process quits
		/// </summary>
		/// <param name="components">The started component instances.</param>
		/// <param name="tickcallback">An optional method to call after each tick.</param>
		public static DependencyGraph RunUntilCompletion(IProcess simulator, IEnumerable<IProcess> components, Action tickcallback = null)
		{
			return RunUntilCompletion(components.Union(new[] { simulator }), tickcallback);
		}

		/// <summary>
		/// Runs all the specified component instances until a process quits
		/// </summary>
		/// <param name="components">The started component instances.</param>
		/// <param name="tickcallback">An optional method to call after each tick.</param>
		public static DependencyGraph RunUntilCompletion(IEnumerable<IProcess> components, Action tickcallback = null)
		{
			var dg = new DependencyGraph(components, tickcallback);
			while (dg.Execute())
			{ }

			return dg;
			
		}

		/// <summary>
		/// Runs all the specified component instances until a process quits
		/// </summary>
		/// <param name="assembly">The assembly to load from. Defaults to the calling assembly.</param>
		/// <param name="tickcallback">An optional method to call after each tick.</param>
		public static DependencyGraph RunUntilCompletion(Assembly assembly = null, Action tickcallback = null)
		{
			assembly = assembly ?? Assembly.GetCallingAssembly();
			return RunUntilCompletion(LoadAssemblies(assembly), tickcallback);
		}

		/// <summary>
		/// Reset all loaded items
		/// </summary>
		public static void Reset()
		{
			BusManager.Clear();
			Clock.DefaultClock.Clear();

			try
			{
				CheckForCrashes();
			}
			catch
			{
			}

			m_startedItems.Clear();
		}

	}
}

