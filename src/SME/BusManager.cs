using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SME
{
	/// <summary>
	/// Static class for instanciating and mapping Bus instances
	/// </summary>
	public static class BusManager
	{
		/// <summary>
		/// Lookup table of all busses in the system
		/// </summary>
		private static Dictionary<string, IBus> m_busses = new Dictionary<string, IBus>();

		/// <summary>
		/// List of all internal buss instances
		/// </summary>
		private static Dictionary<string, IBus> m_internalBusses = new Dictionary<string, IBus>();

		/// <summary>
		/// Lookup table of all clocked busses
		/// </summary>
		private static Dictionary<IBus, Clock> m_clockedBusses = new Dictionary<IBus, Clock>();

		/// <summary>
		/// Gets the bus for a specific interface type and namespace.
		/// </summary>
		/// <returns>The matching bus.</returns>
		/// <param name="t">The interface type to map</param>
		/// <param name="clock">The clock the Bus is defined for.</param>
		/// <param name="namespace">An optional namespace parameter.</param>
		/// <param name="internalBus">True if the bus is marked internal</param>
		public static IBus GetBus(Type t, Clock clock, string @namespace = null, bool internalBus = false)
		{
			if (internalBus)
			{
				var name = t.FullName;
				if (m_internalBusses.ContainsKey(name))
					return m_internalBusses[name];

				var bus = Bus.CreateFromInterface(t, clock);
				m_internalBusses.Add(name, bus);
				return bus;
			}
			else
			{
				var name = t.FullName;
				if (!string.IsNullOrEmpty(@namespace))
					name += "@" + @namespace;

				if (!m_busses.ContainsKey(name))
				{
					m_busses[name] = Bus.CreateFromInterface(t, clock);
					if (t.GetCustomAttributes(typeof(ClockedBusAttribute), true).FirstOrDefault() != null)
						m_clockedBusses[m_busses[name]] = clock;
				}


				return m_busses[name];
			}
		}

		/// <summary>
		/// Determines if is specified bus is clocked or not.
		/// </summary>
		/// <returns><c>true</c> if the specified bus is clocked; otherwise, <c>false</c>.</returns>
		/// <param name="bus">The Bus to look up.</param>
		public static bool IsBusClocked(IBus bus)
		{
			return m_clockedBusses.ContainsKey(bus);
		}

		/// <summary>
		/// Gets all the clocked busses.
		/// </summary>
		/// <value>The clocked busses.</value>
		public static IEnumerable<KeyValuePair<IBus, Clock>> ClockedBusses { get { return m_clockedBusses; } }

		/// <summary>
		/// Gets all non-clocked busses
		/// </summary>
		/// <value>The busses.</value>
		public static IEnumerable<IBus> Busses { get { return m_busses.Values; } }

		/// <summary>
		/// Gets all internal busses
		/// </summary>
		/// <value>The busses.</value>
		public static IEnumerable<IBus> InternalBusses { get { return m_internalBusses.Values; } }

		/// <summary>
		/// Gets the bus for a specific interface type and namespace.
		/// </summary>
		/// <returns>The matching bus.</returns>
		/// <param name="clock">The clock the Bus is defined for.</param>
		/// <param name="namespace">An optional namespace parameter.</param>
		/// <param name="internalBus">True if the bus is marked internal</param>
		/// <typeparam name="T">The interface type to map.</typeparam>
		public static T GetBus<T>(Clock clock, string @namespace = null, bool internalBus = false)
			where T : class, IBus
		{
			return (T)GetBus(typeof(T), clock, @namespace, internalBus);
		}

		/// <summary>
		/// Clears all registered busses, only supported for internal reset
		/// </summary>
		internal static void Clear()
		{
			m_busses.Clear();
			m_clockedBusses.Clear();
			m_internalBusses.Clear();
		}
	}
}

