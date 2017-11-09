using SME;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace SME.Tracer
{
	public abstract class Tracer : IDisposable
	{
		protected SignalEntry[] m_props;
		private bool m_first = true;

        public static IEnumerable<SignalEntry> BuildPropertyMap(Simulation simulation)
		{
			return
				(from bus in simulation.Graph.AllBusses
				 from prop in bus.BusType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
				 let isDriver = bus.BusType.CustomAttributes.Any(x => typeof(TopLevelInputBusAttribute).IsAssignableFrom(x.AttributeType))
				 select new SignalEntry()
				 {
					 Bus = bus,
					 Property = prop,
					 IsDriver = isDriver,
                     SortKey = simulation.BusNames[bus] + "." + prop.Name
				})
				.Where(x => !x.Bus.IsInternal)
				.OrderByDescending(x => x.IsDriver)
				.ThenBy(x => x.SortKey)
			 ;
		}


		protected abstract void OutputSignalNames(SignalEntry[] signals);
		protected abstract void OuputSignalData(IEnumerable<Tuple<SignalEntry, object>> values);

		protected virtual IEnumerable<Tuple<SignalEntry, object>> GetValues()
		{
			foreach (var p in m_props)
			{
				object value = null;

				try
				{
					value = p.Property.GetValue(p.Bus);
				}
				catch (Exception ex)
				{
					if (ex is System.Reflection.TargetInvocationException)
						ex = ((System.Reflection.TargetInvocationException)ex).InnerException;

					if (!(ex is SME.ReadViolationException))
						Console.WriteLine(string.Format("Failed to read item {0}.{1}, message: {2}", p.Property.DeclaringType.FullName, p.Property.Name, ex));
					value = ex;
				}

				yield return new Tuple<SignalEntry, object>(p, value);
			}
		}

        public void OnClockTick(Simulation parent)
		{
			// Skip the very first clock tick to be in sync with the HW version
			if (m_first)
			{
                
				m_props = BuildPropertyMap(parent).ToArray();
				OutputSignalNames(m_props);
				m_first = false;
				return;
			}

			OuputSignalData(GetValues());
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}

