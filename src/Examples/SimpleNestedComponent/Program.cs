using System;
using SME;

namespace SimpleNestedComponent
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			using (var sim = new Simulation())
			{
				var test_driver = new TestDriver();
				var counter_ticker = new CompositeItem.CounterTicker();
				var value_incrementer = new CompositeItem.ValueIncrementer();

				sim
					//.AddTicker(ticks => Console.WriteLine("Ticked {0}", ticks))
					.AddTopLevelInputs(test_driver.Input)
					.AddTopLevelOutputs(test_driver.Output)
					.BuildCSVFile()
					.BuildVHDL()
					.Run();

				Console.WriteLine("Execution complete after {0} ticks", Scope.Current.Clock.Ticks);
			}
		}
	}
}
