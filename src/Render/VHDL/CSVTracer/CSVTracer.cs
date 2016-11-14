using SME;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SME
{
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class CSVExtensionMethods
	{
		public static Simulation BuildCSVFile(this Simulation self, string filename = "trace.csv")
		{
			var tracer = new SME.Render.VHDL.CSVTracer(Path.Combine(self.TargetFolder, filename));

			self.AddTicker(x => tracer.OnClockTick());

			return self;
		}
	}
}

namespace SME.Render.VHDL
{
	public class CSVTracer
	{
		public class SignalEntry
		{
			public IBus Bus;
			public System.Reflection.PropertyInfo Property;
			public bool IsDriver;
		}

		private SignalEntry[] m_props;
		private string m_filename;
		private bool m_first = true;

		public CSVTracer(string filename = null)
		{
			m_filename = filename ?? Path.GetFullPath(filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv");


			while (filename == null && File.Exists(m_filename))
			{
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1.5));
				m_filename = filename ?? Path.GetFullPath(filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv");
			}

			using (File.Create(m_filename)) { }
		}

		public static IEnumerable<SignalEntry> BuildPropertyMap()
		{
			return
				 from bus in BusManager.Busses.Union(BusManager.ClockedBusses.Select(x => x.Key)).Distinct()
				 from prop in bus.BusType.GetProperties(System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
				 let isDriver = bus.BusType.CustomAttributes.Any(x => typeof(SME.TopLevelInputBusAttribute).IsAssignableFrom(x.AttributeType))
				 orderby isDriver descending
				 select new SignalEntry()
				 {
					 Bus = bus,
					 Property = prop,
					 IsDriver = isDriver
				 }
			 ;
				              
		}

		public void OnClockTick()
		{
			// Skip the very first clock tick to be in sync with the HW version
			if (m_first)
			{
				m_props = BuildPropertyMap().ToArray();

				var firstentry = true;
				using (var af = File.AppendText(m_filename))
				{
					foreach (var p in m_props)
					{
						if (firstentry)
							firstentry = false;
						else
							af.Write(",");

						if (p.Property.PropertyType.IsGenericType && p.Property.PropertyType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
						{
							var attr = p.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();
							var propname = VHDLName.BusSignalNameToVHDLName(null, p.Property);
							foreach(var n in Enumerable.Range(0, attr.Length))
								af.Write(string.Format("{0}({1}){2}", propname, n, n == attr.Length - 1 ? "" : ","));
						}
						else
							af.Write(VHDLName.BusSignalNameToVHDLName(null, p.Property));
					}

					af.WriteLine();
				}

				m_first = false;
				return;
			}

			var first = true;
			using (var af = File.AppendText(m_filename))
			{
				foreach (var p in m_props)
				{
					if (first)
						first = false;
					else
						af.Write(",");

					try
					{
						if (p.Property.PropertyType.IsGenericType && p.Property.PropertyType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
						{
							var attr = p.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();

							try
							{
								var eltype = p.Property.PropertyType.GetGenericArguments().First();
								var array = p.Property.GetValue(p.Bus);
								var m = array.GetType().GetProperties().Where(x => x.GetIndexParameters().Length == 1).FirstOrDefault();
								foreach (var n in Enumerable.Range(0, attr.Length))
								{
									try
									{
										af.Write(ConvertToString(m.GetValue(array, new object[] { n }), eltype));
									}
									catch (Exception ex)
									{
										if (ex is System.Reflection.TargetInvocationException)
											ex = ((System.Reflection.TargetInvocationException)ex).InnerException;

										if (ex is ReadViolationException)
											af.Write("U");
										else
											af.Write("X");
										
									}
									if (n != attr.Length - 1)
										af.Write(",");
								}
							}
							catch(Exception ex)
							{
								if (ex is System.Reflection.TargetInvocationException)
									ex = ((System.Reflection.TargetInvocationException)ex).InnerException;

								string c = "X";
								if (ex is ReadViolationException)
									c = "U";

								foreach (var n in Enumerable.Range(0, attr.Length))
								{
									af.Write(c);
									if (n != attr.Length - 1)
										af.Write(",");
								}
							}
						}
						else
						{
							af.Write(ConvertToString(p.Property.GetValue(p.Bus), p.Property.PropertyType));
						}
					}
					catch (Exception ex)
					{
						if (ex is System.Reflection.TargetInvocationException)
							ex = ((System.Reflection.TargetInvocationException)ex).InnerException;

						if (ex is ReadViolationException)
							af.Write("U");
						else
						{

							Console.WriteLine(string.Format("Failed to read item {0}.{1}, message: {2}", p.Property.DeclaringType.FullName, p.Property.Name, ex));
							af.Write("X");
						}
					}
				}

				af.WriteLine();
			}
		}

		private string ConvertToString(object value, Type itemtype)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			
			if (typeof(ICSVSerializable).IsAssignableFrom(itemtype))
				return ((ICSVSerializable)value).Serialize();
			else if (itemtype == typeof(bool))
				return ((bool)value) ? "1" : "0";
			else if (itemtype.IsEnum)
				return VHDLName.ConvertToValidVHDLName(value.GetType().FullName + "." + value.ToString()).ToLower();
			else if (itemtype == typeof(byte))
				return Convert.ToString((byte)value, 2).PadLeft(8, '0');
			else if (itemtype == typeof(ushort))
				return Convert.ToString((ushort)value, 2).PadLeft(16, '0');
			else if (itemtype == typeof(uint))
				return Convert.ToString((uint)value, 2).PadLeft(32, '0');
			else if (itemtype == typeof(sbyte))
				return Convert.ToString((sbyte)value, 2).PadLeft(8, '0');
			else if (itemtype == typeof(short))
				return Convert.ToString((short)value, 2).PadLeft(16, '0');
			else if (itemtype == typeof(int))
				return Convert.ToString((int)value, 2).PadLeft(32, '0');
			else if (itemtype == typeof(long))
				return
					Convert.ToString((int)(((long)value >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
					Convert.ToString((int)((long)value & 0xffffffff), 2).PadLeft(32, '0')
				;
			else if (itemtype == typeof(ulong))
				return
					Convert.ToString((int)(((ulong)value >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
					Convert.ToString((int)((ulong)value & 0xffffffff), 2).PadLeft(32, '0')
				;
			else
				return (value ?? string.Empty).ToString();
		}
	}
}

