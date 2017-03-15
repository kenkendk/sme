using System;

namespace SME.VHDL
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
}

