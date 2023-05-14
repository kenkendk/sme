using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SME
{
    /// <summary>
    /// The interface for a <see cref="T:SME.Bus"/>, which is a collection of signals.
    /// </summary>
    public interface IBus
    {
    }

    /// <summary>
    /// The interface for a <see cref="T:SME.Bus"/>, which is a collection of signals.
    /// </summary>
    public interface IRuntimeBus : IBus
    {
        /// <summary>
        /// The type of the mapped bus.
        /// </summary>
        Type BusType { get; }
        /// <summary>
        /// Propagates staged values.
        /// </summary>
        void Propagate();
        /// <summary>
        /// Forwards staged values.
        /// </summary>
        void Forward();
        /// <summary>
        /// Returns a value indicating if any values are staged.
        /// </summary>
        bool AnyStaged();
        /// <summary>
        /// Returns a list of non-staged properties.
        /// </summary>
        IEnumerable<string> NonStaged();
        /// <summary>
        /// Returns the clock used on the bus.
        /// </summary>
        Clock Clock { get; }
        /// <summary>
        /// Returns the manager for the bus.
        /// </summary>
        IBus Manager { get; }
        /// <summary>
        /// Returns a value indicating if the bus is internal.
        /// </summary>
        bool IsInternal { get; }
        /// <summary>
        /// Returns a value indicating if the bus is clocked.
        /// </summary>
        bool IsClocked { get; }
        /// <summary>
        /// Checks if the property can be read.
        /// </summary>
        /// <param name="property">The property to check</param>
        /// <returns><c>true</c> if the property can be read, <c>false</c> otherwise</returns>
        bool CanRead(string property);
    }

    /// <summary>
    /// The interface for a special bus that only exists once in the application.
    /// </summary>
    public interface ISingletonBus : IBus
    {
    }

    /// <summary>
    /// The interface of a component, which can be structural or functional.
    /// </summary>
    public interface IProcess
    {
        /// <summary>
        /// Flag indicating whether the process is clocked.
        /// </summary>
        bool IsClockedProcess { get; }
        /// <summary>
        /// The name of the process.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Array of buses, from which the process can read and which are clocked.
        /// </summary>
        IRuntimeBus[][] ClockedInputBusses { get; }
        /// <summary>
        /// Array of buses, from which the process can read.
        /// </summary>
        IRuntimeBus[][] InputBusses { get; }
        /// <summary>
        /// Array of buses, which are private to the process.
        /// </summary>
        IRuntimeBus[][] InternalBusses { get; }
        /// <summary>
        /// Array of buses, to which the process can write.
        /// </summary>
        IRuntimeBus[][] OutputBusses { get; }

        /// <summary>
        /// Method for waiting for the clock signal.
        /// </summary>
        Task ClockAsync();
        /// <summary>
        /// Method for reporting that the process has finished.
        /// </summary>
        Task Finished();
        /// <summary>
        /// Method for moving the state of the process back into waiting for input.
        /// </summary>
        Task ResetInputReady();
        /// <summary>
        /// Method for resetting the state of the process.
        /// </summary>
        Task ResetProcessReady();
        /// <summary>
        /// Method defining the behaviour of the process.
        /// </summary>
        Task Run();
        /// <summary>
        /// Method for signaling that the process has finished.
        /// </summary>
        void SignalFinished();
        /// <summary>
        /// Method for signaling that the process is ready for input.
        /// </summary>
        Task SignalInputReady();
        /// <summary>
        /// Method for waiting asynchronously until the given predicate becomes true.
        /// </summary>
        /// <param name="condition">The predicate to wait on.</param>
        Task WaitUntilAsync(Func<bool> condition);
        /// <summary>
        /// A custom renderer object, which will be called rather than the
        /// standard VHDL renderer.
        /// </summary>
        object CustomRenderer { get; }
    }
}
