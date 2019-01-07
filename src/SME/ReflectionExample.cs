using System;
using System.Collections.Generic;

namespace SME
{
    public interface IReflectionExample : IBus
    {
        bool Ready { get; set; }
        int Counter { get; set; }
    }

    public class ReflectionExample : IReflectionExample
    {
        private readonly Bus m_target;

        public ReflectionExample(Bus target)
        {
            m_target = target;
        }

        public Type BusType => m_target.BusType;
        public Clock Clock => m_target.Clock;
        public IBus Manager => throw new Exception("Unused?");
        public bool IsInternal => m_target.IsInternal;
        public bool IsClocked => m_target.IsInternal;

        public bool Ready { get => (bool)m_target.Read(nameof(Ready)); set => m_target.Write(nameof(Ready), value); }
        public int Counter { get => (int)m_target.Read(nameof(Counter)); set => m_target.Write(nameof(Counter), value); }

        public bool AnyStaged() => m_target.AnyStaged();
        public void Forward() => m_target.Forward();
        public IEnumerable<string> NonStaged() => m_target.NonStaged();
        public void Propagate() => m_target.Propagate();
    }
}