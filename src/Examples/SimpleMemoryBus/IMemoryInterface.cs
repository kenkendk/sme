using System;
using SME;

namespace Tester
{
	public interface IMemoryInterface : IBus
	{
		[InitialValue(false)]
		bool WriteEnabled { get; set; }
		[InitialValue(false)]
		bool ReadEnabled { get; set; }

		uint ReadAddr { get; set; }
		uint WriteAddr { get; set; }

		ulong WriteValue { get; set; }
		ulong ReadValue { get; set; }
	}
}

