﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;

namespace SME
{
	/// <summary>
	/// Bus property for manually defining a Bus instance
	/// </summary>
	public class BusProperty<T>
	{
		public readonly string Name;

		public BusProperty(string name)
		{
			Name = name;

			if (typeof(T).IsArray)
				throw new Exception(string.Format("Cannot create a field with arrays ({0}), use the {1} type instead", name, typeof(IFixedArray<>)));

			if (!typeof(T).IsValueType && !typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() != typeof(IFixedArray<>))
				throw new Exception(string.Format("Cannot create a field of non-value-type {0}", typeof(T).Name));
		}
	}

	/// <summary>
	/// Read violation exception.
	/// </summary>
	[Serializable]
	public class ReadViolationException : Exception
	{
		public ReadViolationException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// Write violation exception.
	/// </summary>
	[Serializable]
	public class WriteViolationException : Exception
	{
		public WriteViolationException(string message)
			: base(message)
		{
		}
	}

	/// <summary>
	/// Backend for a DynamicProxy instance of an interface
	/// </summary>
	public class Bus : IRuntimeBus
	{
		private Dictionary<string, object> m_readValues = new Dictionary<string, object>();
		private Dictionary<string, object> m_stageValues = new Dictionary<string, object>();
		private Dictionary<string, object> m_writeValues = new Dictionary<string, object>();
		private Dictionary<string, Type> m_signalTypes = new Dictionary<string, Type>();
		private List<TaskCompletionSource<bool>> m_waiters = new List<TaskCompletionSource<bool>>();

		public Clock Clock { get; private set; }

		/// <summary>
		/// The type the Bus represents
		/// </summary>
		public readonly Type BusType;

		/// <summary>
		/// Gets the type of the bus.
		/// </summary>
		/// <value>The type of the bus.</value>
		Type IRuntimeBus.BusType { get { return BusType; } }

		/// <summary>
		/// Gets a self reference
		/// </summary>
		/// <value>The manager.</value>
		IBus IRuntimeBus.Manager { get { return this; } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:SME.Bus"/> is clocked.
        /// </summary>
        public bool IsClocked { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:SME.Bus"/> is internal.
        /// </summary>
        public bool IsInternal { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SME.Bus"/> class from an interface definition.
		/// </summary>
		/// <param name="t">The interface type to map</param>
		/// <param name="clock">The clock to tick with</param>
		/// <param name="isClocked">A value indicating if the bus is clocked</param>
		/// <param name="isInternal">A value indicating if the bus is internal</param>
		public Bus(Type t, Clock clock, bool isClocked, bool isInternal)
		{
			if (!t.IsInterface)
				throw new Exception(string.Format("Cannot create bus from non-interface type: {0}", t.FullName));

			BusType = t;
            IsClocked = isClocked;
            IsInternal = isInternal;

			var props = t.GetProperties().Concat(t.GetInterfaces().Where(x => x != typeof(IBus)).SelectMany(x => x.GetProperties())).Distinct();
			foreach (var n in props)
			{
				if (n.PropertyType.IsArray)
					throw new Exception(string.Format("Cannot create a field with arrays ({0}), use the {1} type instead", n.Name, typeof(IFixedArray<>)));

				if (!n.PropertyType.IsValueType && !n.PropertyType.IsGenericType && n.PropertyType.GetGenericTypeDefinition() != typeof(IFixedArray<>))
					throw new Exception(string.Format("Cannot create a field of non-value-type {0}", n.Name));

				m_signalTypes[n.Name] = n.PropertyType;

				SetDefaultValues(n, n.PropertyType);
			}

			if (m_signalTypes.Count == 0)
				throw new Exception(string.Format("Bus {0} has no signals", t.FullName));

			Clock = clock;
		}

		/// <summary>
		/// Sets the default value for a bus signal
		/// </summary>
		/// <param name="t">The declaring type for the bus</param>
		/// <param name="n">The reflectioninfo for the signal.</param>
		/// <param name="memberType">The data type of the bus.</param>
		private void SetDefaultValues(MemberInfo n, Type memberType)
		{
			var dfv = n.GetCustomAttributes(typeof(InitialValueAttribute), true);
			if (dfv.Length > 1)
				throw new Exception(string.Format("The field {0} on type {1} has {2} default values, only one or zero is allowed", n.Name, BusType.FullName, dfv.Length));
			else if (dfv.Length == 1)
				m_readValues[n.Name] = Convert.ChangeType(((InitialValueAttribute)dfv[0]).Value ?? Activator.CreateInstance(memberType), memberType);
			else if (BusType.GetCustomAttributes(typeof(InitializedBusAttribute), true).FirstOrDefault() != null)
				m_readValues[n.Name] = Activator.CreateInstance(memberType);
			else if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
			{
				var len = n.GetCustomAttributes(typeof(FixedArrayLengthAttribute), true).FirstOrDefault() as FixedArrayLengthAttribute;
				if (len == null)
					throw new Exception(string.Format("Field {0} on {1} is missing a length attribute, add a an attribute of type {2}", n.Name, n.DeclaringType.FullName, typeof(FixedArrayLengthAttribute).Name));
				m_readValues[n.Name] = Activator.CreateInstance(typeof(FixedArray<>).MakeGenericType(memberType.GetGenericArguments()), len.Length);
			}
		}
			
		/// <summary>
		/// Set a specific value on the bus and check for multiple drivers
		/// </summary>
		/// <param name="name">Name of the signal to write.</param>
		/// <param name="value">Value to write.</param>
		public void Write(string name, object value)
		{
			if (!m_signalTypes.ContainsKey(name))
				throw new Exception(string.Format("No signal named {0} on bus {1}", name, BusType.FullName));
			if (m_writeValues.ContainsKey(name))
				throw new WriteViolationException(string.Format("Attempted to write {0} twice on {1}", name, BusType.FullName));
			m_stageValues[name] = value;
		}

		/// <summary>
		/// Read a value from the bus
		/// </summary>
		/// <param name="name">Name of the signal to read.</param>
		public object Read(string name)
		{
			object obj;
			if (m_readValues.TryGetValue(name, out obj))
				return obj;

			if (m_signalTypes.ContainsKey(name))
				throw new ReadViolationException(string.Format("Attempted to read the signal {0} on the bus {1} before the signal was set", name, BusType.FullName));
			else
				throw new ReadViolationException(string.Format("Attempted to read the signal {0} on the bus {1}, which has no such signal", name, BusType.FullName));
		}

		/// <summary>
		/// Read a value from the bus
		/// </summary>
		/// <param name="name">Name of the signal to read.</param>
		/// <typeparam name="T">The type of data to read.</typeparam>
		public T Read<T>(string name)
		{
			return (T)Read(name);
		}

		/// <summary>
		/// Checks if the property can be read
		/// </summary>
		/// <param name="name">The property to check</param>
		/// <returns><c>true</c> if the property can be read, <c>false</c> otherwise</returns>
		public bool CanRead(string name)
		{
			return m_readValues.ContainsKey(name);
		}

		/// <summary>
		/// Drives the bus and propagates all writen signals into the read side
		/// </summary>
		public virtual void Propagate()
		{
			Forward();

			var lst = m_waiters;
			m_waiters = new List<TaskCompletionSource<bool>>();
			
			foreach (var x in m_writeValues)
				m_readValues[x.Key] = x.Value;

			m_writeValues.Clear();

			foreach (var n in m_readValues.Values.Select(x => x as IFixedArrayInteraction).Where(x => x != null))
				n.Propagate();

			lst.ForEach(x => x.SetResult(true));
		}

		/// <summary>
		/// Forwards all staged values to the write area
		/// </summary>
		public virtual void Forward()
		{
			foreach (var x in m_stageValues)
				if (m_writeValues.ContainsKey(x.Key))
					throw new WriteViolationException(string.Format("Attempted to perform conflicting write {0} on {1}", x.Key, BusType.FullName));
				else
					m_writeValues[x.Key] = x.Value;

			m_stageValues.Clear();

			foreach (var n in m_readValues.Values.Select(x => x as IFixedArrayInteraction).Where(x => x != null))
				n.Forward();

		}

		/// <summary>
		/// Returns true if any values are staged, false otherwise
		/// </summary>
		/// <returns><c>true</c>, if the bus contains staged values, <c>false</c> otherwise.</returns>
		public virtual bool AnyStaged()
		{
			return m_stageValues.Count() > 0;
		}

		/// <summary>
		/// Returns the name of all non-staged properties
		/// </summary>
		public virtual IEnumerable<string> NonStaged()
		{
			return 
				from n in m_signalTypes.Keys
			 	where !m_stageValues.ContainsKey(n)
			 	select n;
		}

		/// <summary>
		/// Creates a bus from an interface.
		/// </summary>
		/// <returns>The DynamicProxy instance.</returns>
		/// <param name="t">The interface type to map.</param>
		/// <param name="clock">The clock to keep the Bus on.</param>
		public static IRuntimeBus CreateFromInterface(Type t, Clock clock, bool isClocked, bool isInternal)
		{
			if (!t.IsInterface)
				throw new Exception(string.Format("Cannot create proxy from non-interface type: {0}", t.FullName));

            return BusProxyCreator.CreateBusProxy(t, clock, isClocked, isInternal);

		}

		/// <summary>
		/// Creates a bus from an interface.
		/// </summary>
		/// <returns>The DynamicProxy instance.</returns>
		/// <param name="clock">The clock to keep the Bus on.</param>
		/// <typeparam name="T">The interface type to map.</typeparam>
		public static T CreateFromInterface<T>(Clock clock, bool isClocked, bool isInternal)
			where T : class, IBus
		{
			if (!typeof(T).IsInterface)
				throw new Exception(string.Format("Cannot create proxy from non-interface type: {0}", typeof(T).FullName));

            return (T)BusProxyCreator.CreateBusProxy(typeof(T), clock, isClocked, isInternal);
			
		}
	}
}

