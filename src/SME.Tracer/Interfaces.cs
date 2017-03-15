using System;
namespace SME.Tracer
{
	public interface ITracerSerializable
	{
		string Serialize(Tracer tracer);
	}
}
