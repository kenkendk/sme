using System;
using SME;
using SME.VHDL;
using System.Linq;

namespace SME.VHDL.Components
{
	[ClockedProcess]
	[SuppressBody]
	public abstract class TrueDualPortMemory<TAddress, TData> : SimpleProcess
	{
		public TrueDualPortMemory()
			: this(Clock.DefaultClock)
		{
		}

		public TrueDualPortMemory(Clock clock)
			: base(clock)
		{
			Setup(clock);
		}

		public interface IInputA : IBus
		{
			[InitialValue]
			bool WriteMode { get; set; }
			[InitialValue]
			bool WriteEnabled { get; set; }
			[InitialValue]
			TAddress Address { get; set; }
			TData Data { get; set; }
		}

		public interface IInputB : IBus
		{
			[InitialValue]
			bool WriteMode { get; set; }
			[InitialValue]
			bool WriteEnabled { get; set; }
			[InitialValue]
			TAddress Address { get; set; }
			TData Data { get; set; }
		}

		public interface IOutputA : IBus
		{
			TData Data { get; set; }
		}

		public interface IOutputB : IBus
		{
			TData Data { get; set; }
		}

		[InputBus]
		[NoAutoLoad]
		protected IInputA InA;
		[InputBus]
		[NoAutoLoad]
		protected IInputB InB;

		[OutputBus]
		[NoAutoLoad]
		protected IOutputA OutA;
		[OutputBus]
		[NoAutoLoad]
		protected IOutputB OutB;

		[Ignore]
		protected TData[] m_memory = new TData[1024];

		protected abstract int ConvertAddress(TAddress adr);
		protected abstract void Setup(Clock clock);

		protected void SetBusses<TInputA, TInputB, TOutputA, TOutputB>(Clock clock)
			where TInputA : class, IInputA
			where TInputB : class, IInputB
			where TOutputA : class, IOutputA
			where TOutputB : class, IOutputB
		{
			string default_namespace = null;
			var nsattr = this.GetType().GetCustomAttributes(typeof(NamespaceAttribute), true).FirstOrDefault() as NamespaceAttribute;
			if (nsattr != null)
				default_namespace = nsattr.Name;

			InA = BusManager.GetBus<TInputA>(clock, default_namespace, false);
			InB = BusManager.GetBus<TInputB>(clock, default_namespace, false);
			OutA = BusManager.GetBus<TOutputA>(clock, default_namespace, false);
			OutB = BusManager.GetBus<TOutputB>(clock, default_namespace, false);
			ReloadBusMaps();
		}

		protected override void OnTick()
		{
			if (InA.WriteMode)
			{
				if (InA.WriteEnabled)
					m_memory[ConvertAddress(InA.Address)] = InA.Data;
			}
			else
			{
				OutA.Data = m_memory[ConvertAddress(InA.Address)];
			}

			if (InB.WriteMode)
			{
				if (InB.WriteEnabled)
					m_memory[ConvertAddress(InB.Address)] = InB.Data;
			}
			else
			{
				OutB.Data = m_memory[ConvertAddress(InB.Address)];
			}
		}

	}
}
