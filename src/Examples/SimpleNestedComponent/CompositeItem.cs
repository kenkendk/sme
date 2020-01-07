using System;
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
            private TickBus Ticker = Scope.CreateOrLoadBus<TickBus>();

			[InputBus]
            private CounterInput Input = Scope.CreateOrLoadBus<CounterInput>();

			protected async override Task OnTickAsync()
			{
				await ClockAsync();
				while (!Input.InputEnabled)
					await ClockAsync();
                
				int cnt = Input.RepeatCount;
				Ticker.Reset = true;

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

		public class ValueIncrementer : StateProcess
		{
			[InputBus]
            private TickBus Ticker = Scope.CreateOrLoadBus<TickBus>();

			[InputBus]
			private CounterInput Input = Scope.CreateOrLoadBus<CounterInput>();

			[OutputBus]
			private CounterOutput Output = Scope.CreateOrLoadBus<CounterOutput>();

			protected async override Task OnTickAsync()
			{
				int regno = 0;

				await ClockAsync();
				while (!(Ticker.Tick || Ticker.Reset))
					await ClockAsync();
				
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

