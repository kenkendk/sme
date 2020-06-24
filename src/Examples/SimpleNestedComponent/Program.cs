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

                test_driver.Output = value_incrementer.Output;
                counter_ticker.Input = test_driver.Input;
                value_incrementer.Input = test_driver.Input;
                value_incrementer.Ticker = counter_ticker.Ticker;

                sim
                    .AddTopLevelInputs(test_driver.Input)
                    .AddTopLevelOutputs(test_driver.Output)
                    .BuildCSVFile()
                    .BuildVHDL()
                    .Run();
            }
        }
    }
}
