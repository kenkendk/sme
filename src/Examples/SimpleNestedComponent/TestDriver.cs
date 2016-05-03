using System;
using SME;
using System.Threading.Tasks;

namespace SimpleNestedComponent
{
	[ClockedProcess]
	public class TestDriver : Process
	{
		[OutputBus]
		private CounterInput Input;

		[InputBus]
		private CounterOutput Output;

		public async override Task Run()
		{
			await ClockAsync();

			Input.InputEnabled = true;
			Input.StartRegister = 4;
			Input.RepeatCount = 4;

			await WaitUntilAsync(() => { Input.InputEnabled = false; return Output.OutputEnabled; });

			Console.WriteLine("Output is {0} expected {1}", Output.RegisterNumber, 4);

			await WaitUntilAsync(() => Output.OutputEnabled);

			Console.WriteLine("Output is {0} expected {1}", Output.RegisterNumber, 5);

			await WaitUntilAsync(() => Output.OutputEnabled);

			Console.WriteLine("Output is {0} expected {1}", Output.RegisterNumber, 6);

			await WaitUntilAsync(() => Output.OutputEnabled);

			Console.WriteLine("Output is {0} expected {1}", Output.RegisterNumber, 7);

			await ClockAsync();

			Console.WriteLine("Output is expected to be false and it is: {0}", Output.OutputEnabled);
		}
	}
}

