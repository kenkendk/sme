﻿using System;

namespace SME
{
	/// <summary>
	/// Attribute for setting a multiplier on a Component
	/// </summary>
	public class ClockMultiplierAttribute : Attribute
	{
		public int Multiplier { get; set; }

		public ClockMultiplierAttribute(int multiplier)
		{
			Multiplier = multiplier;
		}
	}

	/// <summary>
	/// Helper class for setting a default value on a bus signal
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class InitialValueAttribute : Attribute
	{
		public object Value { get; set; }

		public InitialValueAttribute()
		{
		}

		public InitialValueAttribute(object value)
		{
			Value = value;
		}
	}

    /// <summary>
    /// Marker attribute to signal that the bus only exists once and is automatically assigned
    /// </summary>
    public class SingletonBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus with being initialized with default values
	/// </summary>
	public class InitializedBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus as Input only
	/// </summary>
	public class InputBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus as Output only
	/// </summary>
	public class OutputBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus for a component
	/// </summary>
	public class ComponentBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus as automatic loading
	/// </summary>
	public class AutoloadBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus as internal only
	/// </summary>
	public class InternalBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute ot mark a process as depending on the clock only
	/// </summary>
	public class ClockedProcessAttribute : Attribute { }

	/// <summary>
	/// Marker attribute for marking a bus as being driven by the clock only
	/// </summary>
	public class ClockedBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus as being the top-level input
	/// </summary>
	public class TopLevelInputBusAttribute : Attribute { }

	/// <summary>
	/// Marker attribute to mark a bus as being the top-level output
	/// </summary>
	public class TopLevelOutputBusAttribute : Attribute { }

	/// <summary>
	/// Attribute for specifying the length of a FixedArray
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class FixedArrayLengthAttribute : Attribute 
	{
		/// <summary>
		/// Gets or sets the length or the fixed array.
		/// </summary>
		/// <value>The length.</value>
		public int Length { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.FixedArrayLength"/> class with the specified length.
		/// </summary>
		/// <param name="length">Length.</param>
		public FixedArrayLengthAttribute(int length)
		{
			Length = length;
		}
	}


	/// <summary>
	/// Marker attribute for ignoring a member or process
	/// </summary>
	public class IgnoreAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for attempting to compile a member method
	/// </summary>
	public class CompileAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for supressing the body
	/// </summary>
	public class SuppressBodyAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for supressing output
	/// </summary>
	public class SuppressOutputAttribute : Attribute { }
	/// <summary>
	/// Marker attribute for rendering a variable as a signal
	/// </summary>
	public class SignalAttribute : Attribute { }

	/// <summary>
	/// Attribute for marking an array argument as having a fixed size
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
	public class RangeAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the lower bound.
		/// </summary>
		public int LowerBound { get; set; }
		/// <summary>
		/// Gets or sets the upper bound.
		/// </summary>
		public int UpperBound { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.Render.Transpiler.SMERangeAttribute"/> class.
		/// </summary>
		/// <param name="upperBound">The upper bound to use.</param>
		public RangeAttribute(int upperBound)
		{
			UpperBound = upperBound;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.Render.Transpiler.SMERangeAttribute"/> class.
		/// </summary>
		/// <param name="lowerBound">The lower bound.</param>
		/// <param name="upperBound">The upper bound.</param>
		public RangeAttribute(int lowerBound, int upperBound)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}
	}
}

