using System;
using SME;

namespace SimpleNestedComponent
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            new Simulation()
                .AddTicker(ticks => Console.WriteLine("Ticked {0}", ticks))
                .BuildCSVFile()
                .BuildVHDL()
                .Run(
                    new TestDriver(),
				    new CompositeItem.CounterTicker(),
				    new CompositeItem.ValueIncrementer()
				);

			Console.WriteLine("Execution complete after {0} ticks", Scope.Current.Clock.Ticks);
		}
	}
}
