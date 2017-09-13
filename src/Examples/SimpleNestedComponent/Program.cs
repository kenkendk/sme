using System;
using SME;

namespace SimpleNestedComponent
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            var network = new IProcess[] {
                new TestDriver(),
                new CompositeItem.CounterTicker(),
                new CompositeItem.ValueIncrementer()
            };

            Loader.RunUntilCompletion(network, () => Console.WriteLine("Ticked {0}", Clock.DefaultClock.Ticks));

			Console.WriteLine("Execution complete after {0} ticks", Clock.DefaultClock.Ticks);
		}
	}
}
