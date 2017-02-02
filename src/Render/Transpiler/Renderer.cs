using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SME.Render.Transpiler.ILConvert;

namespace SME.Render.Transpiler
{
	/// <summary>
	/// Class for rendering the output in another language
	/// </summary>
	public static class Renderer
	{
		public static IDictionary<Type, string> TypeMap { get; private set; }

		public static IEnumerable<IBus> InputOnlyBusses(IProcess process)
		{
			return
				from n in process.InputBusses.Union(process.ClockedInputBusses)
				where !process.OutputBusses.Contains(n)
				select n;

		}

		public static IEnumerable<IBus> OutputOnlyBusses(IProcess process)
		{
			return
				from n in process.OutputBusses
				where !process.InputBusses.Union(process.ClockedInputBusses).Contains(n)
				select n;
		}

		public static IEnumerable<IBus> InputOutputBusses(IProcess process)
		{
			return
				from n in process.InputBusses.Union(process.ClockedInputBusses)
				where process.OutputBusses.Contains(n)
				select n;
		}

		public static IEnumerable<IBus> InternalBusses(IProcess process)
		{
			return process.InternalBusses;
		}

		public static IEnumerable<IBus> ClockedBusses(IEnumerable<IProcess> processes)
		{
			return processes
				.SelectMany(x => x.InputBusses.Union(x.OutputBusses).Union(x.ClockedInputBusses))
				.Distinct()
				.Where(x => IsClockedBus(x));
		}

		public static bool IsClockedBus(IBus bus)
		{
			return bus.BusType.GetCustomAttributes(typeof(ClockedBusAttribute), true).FirstOrDefault() != null;
		}
	}
}
