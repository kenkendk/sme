using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SME.Tracer
{
	public class CSVTracer : Tracer
	{
		private string m_filename;

		public CSVTracer(string filename = null, string targetfolder = null)
		{
			m_filename = Path.GetFullPath(Path.Combine(targetfolder ?? ".", filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv"));

			while (filename == null && File.Exists(m_filename))
			{
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1.5));
				m_filename = Path.GetFullPath(Path.Combine(targetfolder ?? ".", filename ?? DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv"));
			}

			using (File.Create(m_filename)) { }
		}

		protected override void OutputSignalNames(SignalEntry[] signals)
		{
			var firstentry = true;
			using (var af = File.AppendText(m_filename))
			{
				foreach (var p in signals)
				{
					if (firstentry)
						firstentry = false;
					else
						af.Write(",");

					if (p.Property.PropertyType.IsGenericType && p.Property.PropertyType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
					{
						var attr = p.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();
						var propname = BusSignalToName(p);
						foreach (var n in Enumerable.Range(0, attr.Length))
							af.Write(string.Format("{0}({1}){2}", propname, n, n == attr.Length - 1 ? "" : ","));
					}
					else
						af.Write(BusSignalToName(p));
				}

				af.WriteLine();
			}
		}

		protected override void OuputSignalData(IEnumerable<Tuple<SignalEntry, object>> values)
		{
			var first = true;
			using (var af = File.AppendText(m_filename))
			{
				foreach (var signal in values)
				{
					if (first)
						first = false;
					else
						af.Write(",");

					var p = signal.Item1;
					var value = signal.Item2;

					if (p.Property.PropertyType.IsGenericType && p.Property.PropertyType.GetGenericTypeDefinition() == typeof(IFixedArray<>))
					{
						var attr = p.Property.GetCustomAttributes(typeof(FixedArrayLengthAttribute), false).Cast<FixedArrayLengthAttribute>().FirstOrDefault();

						try
						{
							var eltype = p.Property.PropertyType.GetGenericArguments().First();
							var m = value.GetType().GetProperties().FirstOrDefault(x => x.GetIndexParameters().Length == 1);
							foreach (var n in Enumerable.Range(0, attr.Length))
							{
								try
								{
									af.Write(ConvertToString(m.GetValue(value, new object[] { n }), eltype));
								}
								catch (Exception ex)
								{
									if (ex is TargetInvocationException)
										ex = ((TargetInvocationException)ex).InnerException;

									if (ex is ReadViolationException)
										af.Write("U");
									else
										af.Write("X");

								}
								if (n != attr.Length - 1)
									af.Write(",");
							}
						}
						catch (Exception ex)
						{
							if (ex is TargetInvocationException)
								ex = ((TargetInvocationException)ex).InnerException;

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
						af.Write(ConvertToString(value, p.Property.PropertyType));
					}
				}

				af.WriteLine();
			}
		}

		protected virtual string ConvertToString(object value, Type itemtype)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value is ReadViolationException)
				return "U";
			else if (value is Exception)
				return "X";
			else if (typeof(ITracerSerializable).IsAssignableFrom(itemtype))
				return ((ITracerSerializable)value).Serialize(this);
			else if (itemtype == typeof(bool))
				return ((bool)value) ? "1" : "0";
			else if (itemtype.IsEnum)
				return ConvertToValidName(value.GetType().FullName + "." + value.ToString()).ToLower();
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

		public virtual string ConvertToValidName(string name)
		{
			return name;
		}

		public virtual string BusSignalToName(SignalEntry p)
		{
			return p.SortKey;
		}
	}}
