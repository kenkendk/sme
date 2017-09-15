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
		private static List<IBus> m_busses = new List<IBus>();

		/// <summary>
		/// List of all internal buss instances
		/// </summary>
		private static List<IBus> m_internalBusses = new List<IBus>();

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
		/// <param name="internalBus">True if the bus is marked internal</param>
		public static IBus CreateBus(Type t, Clock clock, bool internalBus = false)
		{
			var bus = Bus.CreateFromInterface(t, clock);
            if (internalBus)
                m_internalBusses.Add(bus);
            else
            {
				m_busses.Add(bus);
				if (t.GetCustomAttributes(typeof(ClockedBusAttribute), true).FirstOrDefault() != null)
					m_clockedBusses[bus] = clock;
			}

            return bus;
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
        public static IEnumerable<IBus> Busses { get { return m_busses.AsReadOnly(); } }

		/// <summary>
		/// Gets all internal busses
		/// </summary>
		/// <value>The busses.</value>
        public static IEnumerable<IBus> InternalBusses { get { return m_internalBusses.AsReadOnly(); } }

		/// <summary>
		/// Gets the bus for a specific interface type and namespace.
		/// </summary>
		/// <returns>The matching bus.</returns>
		/// <param name="clock">The clock the Bus is defined for.</param>
		/// <param name="internalBus">True if the bus is marked internal</param>
		/// <typeparam name="T">The interface type to map.</typeparam>
		public static T CreateBus<T>(Clock clock, bool internalBus = false)
			where T : class, IBus
		{
			return (T)CreateBus(typeof(T), clock, internalBus);
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

