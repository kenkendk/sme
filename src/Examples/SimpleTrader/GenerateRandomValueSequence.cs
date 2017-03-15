using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleTrader
{
	/// <summary>
	/// Generates a list of random uint values, simulating a trade sequence
	/// </summary>
	public static class GenerateRandomValueSequence
	{
		/// <summary>
		/// Generates an inifinite sequence of values in random waves
		/// </summary>
		/// <returns>The values.</returns>
		/// <param name="seed">The random seed.</param>
		/// <param name="maxrange">The maximum amount of numbers to generate in one direction before moving in the other.</param>
		public static IEnumerable<double> GetValues(int seed = 0, int maxrange = 1000)
		{
			var rnd = seed == 0 ? new Random() : new Random(seed);

			var current = rnd.NextDouble();
			var upOrDown = rnd.Next() > int.MaxValue / 2;

			while (true)
			{
				upOrDown = !upOrDown;
				var entries = rnd.Next(5, maxrange);

				while (entries-- > 0)
				{
					var next = rnd.NextDouble();
					var sign = (upOrDown ^ (rnd.Next() > int.MaxValue / 4)) ? 1 : -1;
					current = Math.Min(1, Math.Max(0, current + (next * 0.1 * sign)));
					if (current != 0 && current != 1)
						yield return current;
				}
			}
		}

		/// <summary>
		/// Generates an inifinite sequence of values in random waves
		/// </summary>
		/// <returns>The values.</returns>
		/// <param name="seed">The random seed.</param>
		/// <param name="maxrange">The maximum amount of numbers to generate in one direction before moving in the other.</param>
		/// <param name="range">The range in which to allow values.</param>
		public static IEnumerable<uint> GetUIntSequence(int seed = 0, int maxrange = 1000, uint range = 5000)
		{
			return GetValues(seed, maxrange).Select(x => (uint)(x * range));
		}

		public static void SaveSequence<T>(this IEnumerable<T> values, string filename, Func<T, string> serial = null)
		{
			if (serial == null)
				serial = x => x == null ? null : x.ToString();
			
			using (var fs = System.IO.File.OpenWrite(filename))
			using (var sw = new System.IO.StreamWriter(fs))
				foreach (var n in values)
					sw.WriteLine(serial(n));
		}

		public static IEnumerable<uint> LoadSequence(string filename)
		{
			return LoadSequence<uint>(filename, x => uint.Parse(x));
		}


		public static IEnumerable<T> LoadSequence<T>(string filename, Func<string, T> serial)
		{
			using (var fs = System.IO.File.OpenRead(filename))
			using (var sr = new System.IO.StreamReader(fs))
			{
				string str;
				while ((str = sr.ReadLine()) != null)
					yield return serial(str);
			}
		}
	}
}

