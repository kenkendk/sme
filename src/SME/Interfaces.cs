using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SME
{
	/// <summary>
	/// The interface for a Bus, which is a collection of signals
	/// </summary>
	public interface IBus
	{
		/// <summary>
		/// The type of the mapped bus
		/// </summary>
		/// <value></value>
		Type BusType { get; }
		/// <summary>
		/// Propagates staged values
		/// </summary>
		void Propagate();
		/// <summary>
		/// Forwards staged values
		/// </summary>
		void Forward();
		/// <summary>
		/// Returns a value indicating if any values are staged
		/// </summary>
		bool AnyStaged();
        /// <summary>
        /// Returns a list of non-staged properties
        /// </summary>
        IEnumerable<string> NonStaged();
        /// <summary>
        /// Returns the clock used on the bus
        /// </summary>
        Clock Clock { get; }
        /// <summary>
        /// Returns the manager for the bus
        /// </summary>
        IBus Manager { get; }
        /// <summary>
        /// Returns a value indicating if the bus is internal
        /// </summary>
        bool IsInternal { get; }
		/// <summary>
		/// Returns a value indicating if the bus is clocked
		/// </summary>
        bool IsClocked { get; }
        /// <summary>
        /// Checks if the property can be read
        /// </summary>
        /// <param name="name">The property to check</param>
        /// <returns><c>true</c> if the property can be read, <c>false</c> otherwise</returns>
        bool CanRead(string property);
	}

    /// <summary>
    /// The interface for a special Bus that only exists once in the application
    /// </summary>
    public interface ISingletonBus : IBus
    {
    }
		
	/// <summary>
	/// The interface of a component, which can be structural or functional
	/// </summary>
	public interface IProcess
	{
		Task ClockAsync();
		Task WaitUntilAsync(Func<bool> condition);
		Task SignalInputReady();

		IBus[] InputBusses { get; }
		IBus[] OutputBusses { get; }
		IBus[] ClockedInputBusses { get; }
		IBus[] InternalBusses { get; }

		bool IsClockedProcess { get; }
        string Name { get; }

		Task Run();
	}
}

