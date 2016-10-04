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

			m_props = BuildPropertyMap().ToArray();

			var first = true;
			using (var af = File.AppendText(m_filename))
			{
				foreach (var p in m_props)
				{
					if (first)
						first = false;
					else
						af.Write(",");
					
					af.Write(VHDLName.BusSignalNameToVHDLName(null, p.Property));
				}

				af.WriteLine();
			}
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
						var value = p.Property.GetValue(p.Bus);
						if ((value as ICSVSerializable) != null)
							af.Write(((ICSVSerializable)value).Serialize());
						else if (value is bool)
							af.Write(((bool)value) ? '1' : '0');
						else if (value.GetType().IsEnum)
							af.Write(VHDLName.ConvertToValidVHDLName(value.GetType().FullName + "." + value.ToString()).ToLower());
						else if (value is byte)
							af.Write(Convert.ToString((byte)value, 2).PadLeft(8, '0'));
						else if (value is long)
							af.Write(
								Convert.ToString((int)(((long)value >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
								Convert.ToString((int)((long)value & 0xffffffff), 2).PadLeft(32, '0')
							);
						else if (value is ulong)
							af.Write(
								Convert.ToString((int)(((ulong)value >> 32) & 0xffffffff), 2).PadLeft(32, '0') +
								Convert.ToString((int)((ulong)value & 0xffffffff), 2).PadLeft(32, '0')
							);
						else
							af.Write(value); 
					}
					catch
					{
						af.Write("U");
					}
				}

				af.WriteLine();
			}
		}
	}
}

