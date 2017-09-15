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
		/// Loads and starts the supplied processes
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="items">The The types to start.</param>
		public static IEnumerable<IProcess> LoadItems(IEnumerable<IProcess> items)
		{
			return LoadItems(items.ToArray());
		}

		/// <summary>
		/// Loads and starts the supplied process
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="item">The The type to start.</param>
		public static IEnumerable<IProcess> LoadItem(IProcess item)
		{
			return LoadItems(new[] { item });
		}

		/// <summary>
		/// Loads and starts the supplied processes
		/// </summary>
		/// <returns>The list of loaded components.</returns>
		/// <param name="items">The The type to start.</param>
		public static IEnumerable<IProcess> LoadItems(params IProcess[] items)
		{
			var lst = new List<IProcess>();

			foreach (var item in items)
			{
				var p = (IProcess)AutoloadBusses(item);
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
		public static object AutoloadBusses(object o)
		{
			if (DebugBusAssignments)
				Console.WriteLine("Autoloading busses for {0}", o.GetType().FullName);

			foreach (var f in GetBusFields(o.GetType()))
				if (f.GetValue(o) == null)
				{
					var internalBus = (f.GetCustomAttributes(typeof(InternalBusAttribute)).FirstOrDefault() as InternalBusAttribute != null);

                    if (typeof(ISingletonBus).IsAssignableFrom(f.FieldType))
                    {
                        var bus = Scope.CreateOrLoadBus(f.FieldType, null, internalBus);
                        if (DebugBusAssignments)
                            Console.WriteLine("Setting field {0}.{1} = {2}:{3}:{4} -> {5}", f.DeclaringType.Name, f.Name, null, bus, f.FieldType.FullName, bus.GetHashCode());

                        f.SetValue(o, Scope.CreateOrLoadBus(f.FieldType, null, internalBus));
                    }
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

			if (all.Any())
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
		public static DependencyGraph RunUntilCompletion(this IProcess simulator, IEnumerable<IProcess> components, Action tickcallback = null)
		{
			return RunUntilCompletion(components.Union(new[] { simulator }), tickcallback);
		}

		/// <summary>
		/// Runs all the specified component instances until a process quits
		/// </summary>
		/// <param name="components">The started component instances.</param>
		/// <param name="tickcallback">An optional method to call after each tick.</param>
		public static DependencyGraph RunUntilCompletion(this IEnumerable<IProcess> components, Action tickcallback = null)
		{
            LoadItems(components);

			var dg = new DependencyGraph(components, tickcallback);
			while (dg.Execute())
			{ }

			return dg;
			
		}

		/// <summary>
		/// Reset all loaded items
		/// </summary>
		public static void Reset()
		{
			BusManager.Clear();
            Scope.Current.Clock.Clear();

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

