using System;
using SME;

namespace SimpleTradeImplementation
{
	/// <summary>
	/// The top-level bus that communicates input values
	/// </summary>
	[TopLevelInputBus]
	public interface ITraderInput : IBus
	{
		[InitialValue]
		bool Valid { get; set; }
		[InitialValue]
		bool Restart { get; set; }
		uint Value { get; set; }
	}
}
