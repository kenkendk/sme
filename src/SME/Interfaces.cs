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
		Type BusType { get; }
		void Propagate();
		void Forward();
		bool AnyStaged();
		IEnumerable<string> NonStaged();
		Clock Clock { get; }
		IBus Manager { get; }
        bool IsInternal { get; }
        bool IsClocked { get; }
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

