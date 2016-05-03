using System;
using SME;

namespace SimpleNestedComponent
{
	public interface CounterInput : IBus
	{
		[InitialValue(false)]
		bool InputEnabled { get; set; }

		int StartRegister { get; set; }
		int RepeatCount { get; set; }
	}

	public interface CounterOutput : IBus
	{
		int RegisterNumber { get ; set; }
		[InitialValue(false)]
		bool OutputEnabled { get; set; }
	}
}

