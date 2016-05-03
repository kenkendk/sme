using System;
using SME;
using System.Threading.Tasks;

namespace SimpleNestedComponent
{
	public class CompositeItem
	{
		public interface TickBus : IBus
		{
			[InitialValue(false)]
			bool Tick { get; set; }
			[InitialValue(false)]
			bool Reset { get; set; }
		}

		private class CounterTicker : Process
		{
			[OutputBus]
			[Namespace("C2")]
			private TickBus Ticker;

			[InputBus]
			private CounterInput Input;

			public async override Task Run()
			{
				await ClockAsync();

				while (true)
				{
					await WaitUntilAsync(() => Input.InputEnabled );

					int cnt = Input.RepeatCount;
					Ticker.Reset = true;

					while (cnt-- > 0)
					{
						Ticker.Tick = true;
						await ClockAsync();

						if (cnt != 0)
							Ticker.Reset = false;
					}

					Ticker.Tick = false;
					Ticker.Reset = true;
				}

			}
		}

		private class ValueIncrementer : Process
		{
			[InputBus]
			[Namespace("C2")]
			private TickBus Ticker;

			[InputBus]
			private CounterInput Input;

			[OutputBus]
			private CounterOutput Output;

			public async override Task Run()
			{
				await ClockAsync();

				int regno = 0;

				while (true)
				{
					await WaitUntilAsync(() => Ticker.Tick || Ticker.Reset);

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

