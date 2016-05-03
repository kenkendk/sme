using System;

namespace SME.Render.VHDL
{
	/// <summary>
	/// Attribute for supplying the type used in VHDL output
	/// </summary>
	public class VHDLTypeAttribute : Attribute
	{
		public string Type { get; set; }
		public string Alias { get; set; }

		public VHDLTypeAttribute(string type, string alias = null)
		{
			Type = type;
			Alias = alias;
		}
	}

	/// <summary>
	/// Marker attribute for rendering a variable as a signal
	/// </summary>
	public class VHDLSignalAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for ignoring a member or process
	/// </summary>
	public class VHDLIgnoreAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for attempting to compile a member method
	/// </summary>
	public class VHDLCompileAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for supressing VHDL body
	/// </summary>
	public class VHDLSuppressBodyAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for supressing VHDL output
	/// </summary>
	public class VHDLSuppressOutputAttribute : Attribute { }

	/// <summary>
	/// Attribute for marking process as a named component
	/// </summary>
	public class VHDLComponentAttribute : Attribute 
	{
		public string Name { get; set; }

		public VHDLComponentAttribute(string name)
		{
			Name = name;
		}
	}

	/// <summary>
	/// Attribute for marking an array argument as having a fixed size
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
	public class VHDLRangeAttribute : Attribute 
	{
		public int LowerBound { get; set; }
		public int UpperBound { get; set; }

		public VHDLRangeAttribute(int upperBound)
		{
			UpperBound = upperBound;
		}

		public VHDLRangeAttribute(int lowerBound, int upperBound)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}
	}

}

