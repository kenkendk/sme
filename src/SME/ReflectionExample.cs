using System;
using System.Collections.Generic;

namespace SME.InternalItems
{
    /// <summary>
    /// Example bus declaration.
    /// </summary>
    internal interface IReflectionExample : IBus
    {
        /// <summary>
        /// Example property.
        /// </summary>
        bool Ready { get; set; }
        /// <summary>
        /// Example property.
        /// </summary>
        int Counter { get; set; }
    }

    /// <summary>
    /// A manual implementation of the BusProxy.
    /// This implementation is not used by SME,
    /// but can be used to view the generated IL,
    /// for comparing with the generated proxy bus IL.
    /// </summary>
    internal class ReflectionExample : IReflectionExample, IRuntimeBus
    {
        /// <summary>
        /// The target.
        /// </summary>
        private readonly Bus m_target;

        /// <summary>
        /// Constructs an example reflection.
        /// </summary>
        /// <param name="target">The bus target with the real implementation.</param>
        public ReflectionExample(Bus target)
        {
            m_target = target;
        }

        /// <summary>
        /// Gets the underlying bus type.
        /// </summary>
        public Type BusType => m_target.BusType;
        /// <summary>
        /// Gets the bus clock.
        /// </summary>
        public Clock Clock => m_target.Clock;
        /// <summary>
        /// Gets the bus manager.
        /// </summary>
        public IBus Manager => ((IRuntimeBus)m_target).Manager;
        /// <summary>
        /// Gets the internal flag.
        /// </summary>
        public bool IsInternal => m_target.IsInternal;
        /// <summary>
        /// Gets the clocked flag.
        /// </summary>
        public bool IsClocked => m_target.IsInternal;

        /// <summary>
        /// Implements the example property.
        /// </summary>
        public bool Ready { get => m_target.Read<bool>(nameof(Ready)); set => m_target.Write(nameof(Ready), value); }
        /// <summary>
        /// Implements the example property.
        /// </summary>
        public int Counter { get => m_target.Read<int>(nameof(Counter)); set => m_target.Write(nameof(Counter), value); }

        /// <summary>
        /// Calls the AnyStaged method.
        /// </summary>
        public bool AnyStaged() => m_target.AnyStaged();
        /// <summary>
        /// Calls the Forward method.
        /// </summary>
        public void Forward() => m_target.Forward();
        /// <summary>
        /// Calls the NonStaged method.
        /// </summary>
        public IEnumerable<string> NonStaged() => m_target.NonStaged();
        /// <summary>
        /// Calls the Propagate method.
        /// </summary>
        public void Propagate() => m_target.Propagate();
        /// <summary>
        /// Checks if the property can be read.
        /// </summary>
        /// <param name="name">The property to check.</param>
        /// <returns><c>true</c> if the property can be read, <c>false</c> otherwise.</returns>
        public bool CanRead(string name) => m_target.CanRead(name);
    }
}
