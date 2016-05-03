using SME;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace SME.Render.VHDL
{
	public class CSVTracer
	{
		private KeyValuePair<IBus, System.Reflection.PropertyInfo>[] m_props;
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
					
					af.Write(VHDLName.BusSignalNameToVHDLName(null, p.Value));
				}

				af.WriteLine();
			}
		}

		public static IEnumerable<KeyValuePair<IBus, System.Reflection.PropertyInfo>> BuildPropertyMap()
		{
			return
				from bus in BusManager.Busses.Union(BusManager.ClockedBusses.Select(x => x.Key)).Distinct()
					from prop in bus.BusType.GetProperties(System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
					select new KeyValuePair<IBus, System.Reflection.PropertyInfo>(bus, prop);
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
						var value = p.Value.GetValue(p.Key);
						if ((value as ICSVSerializable) != null)
							af.Write(((ICSVSerializable)value).Serialize());
						else if (value is bool)
							af.Write(((bool)value) ? '1' : '0');
						else if (value.GetType().IsEnum)
							af.Write(VHDLName.ConvertToValidVHDLName(value.GetType().FullName + "." + value.ToString()).ToLower());
						else if (value is byte)
							af.Write(Convert.ToString((byte)value, 2).PadLeft(8, '0'));
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

