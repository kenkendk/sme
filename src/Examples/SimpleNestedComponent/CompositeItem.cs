using SME;
using System.Threading.Tasks;

namespace SimpleNestedComponent
{
    public class CompositeItem
    {
        [SingletonBus]
        public interface TickBus : IBus
        {
            [InitialValue(false)]
            bool Tick { get; set; }
            [InitialValue(false)]
            bool Reset { get; set; }
        }

        public class CounterTicker : StateProcess
        {
            [OutputBus]
            public TickBus Ticker = Scope.CreateBus<TickBus>();

            [InputBus]
            public CounterInput Input;

            protected async override Task OnTickAsync()
            {
                await ClockAsync();
                while (!Input.InputEnabled)
                    await ClockAsync();

                int cnt = Input.RepeatCount;
                Ticker.Reset = true;
                await ClockAsync();

                while (cnt > 0)
                {
                    cnt--;
                    Ticker.Tick = true;
                    await ClockAsync();

                    if (cnt != 0)
                        Ticker.Reset = false;
                }

                Ticker.Tick = false;
                Ticker.Reset = true;
            }
        }

        public class ValueIncrementer : SimpleProcess
        {
            [InputBus]
            public TickBus Ticker;

            [InputBus]
            public CounterInput Input;

            [OutputBus]
            public CounterOutput Output = Scope.CreateBus<CounterOutput>();

            int regno = 0;

            protected override void OnTick()
            {
                if (Ticker.Tick || Ticker.Reset)
                {
                    if (Ticker.Reset)
                        regno = Input.StartRegister;
                    else if (Ticker.Tick)
                        regno++;

                    Output.OutputEnabled = Ticker.Tick;
                    Output.RegisterNumber = regno;
                }
            }
        }
    }
}

