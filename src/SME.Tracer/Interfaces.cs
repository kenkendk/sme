using System;

namespace SME.Tracer
{
    /// <summary>
    /// The interface of an serializable tracer.
    /// </summary>
    public interface ITracerSerializable
    {
        /// <summary>
        /// Serializes the given tracer.
        /// </summary>
        /// <param name="tracer">The given tracer.</param>
        string Serialize(Tracer tracer);
    }
}
