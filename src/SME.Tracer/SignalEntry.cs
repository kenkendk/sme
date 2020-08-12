using System;
using System.Reflection;

namespace SME.Tracer
{
    /// <summary>
    /// Class holding values for a signal.
    /// </summary>
    public class SignalEntry
    {
        /// <summary>
        /// The bus the signal belongs to.
        /// </summary>
        public IRuntimeBus Bus;
        /// <summary>
        /// Property meta data corresponding to the signal.
        /// </summary>
        public PropertyInfo Property;
        /// <summary>
        /// Flag indicating whether the signal is top level.
        /// </summary>
        public bool IsDriver;
        /// <summary>
        /// Flag indicating whether the signal is internal.
        /// </summary>
        public bool IsInternal;
        /// <summary>
        /// Key to sort the signals by.
        /// </summary>
        public string SortKey;
    }
}

