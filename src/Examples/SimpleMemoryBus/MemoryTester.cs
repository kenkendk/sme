using System;
using SME;
using System.Threading.Tasks;

namespace Tester
{
	[ClockedProcess]
	public class MemoryTester : Process
	{
		[InputBus, OutputBus]
		private IMemoryInterface Interface;

		public async override Task Run()
		{
			Console.WriteLine("Pre-tick");

			await ClockAsync();

			Console.WriteLine("Stage 1");

			Interface.WriteAddr = 22;
			Interface.WriteValue = 32;
			Interface.WriteEnabled = true;

			Console.WriteLine("Wait for Stage 2");

			await ClockAsync();

			Console.WriteLine("Stage 2");

			Interface.WriteAddr = 23;
			Interface.WriteValue = 33;
			Interface.WriteEnabled = true;

			Console.WriteLine("Wait for Stage 3");

			await ClockAsync();

			Console.WriteLine("Stage 3");

			Interface.WriteEnabled = false;
			Interface.ReadEnabled = true;
			Interface.ReadAddr = 22;

			Console.WriteLine("Wait for Stage 4");

			await ClockAsync();

			Console.WriteLine("Stage 4");

			Console.WriteLine("Expecting ReadValue to be {0} and it is {1}", 32, Interface.ReadValue);
			System.Diagnostics.Debug.Assert(Interface.ReadValue == 32);

			Interface.WriteEnabled = false;
			Interface.ReadEnabled = true;
			Interface.ReadAddr = 23;

			await ClockAsync();

			Console.WriteLine("Stage 5");
			Interface.ReadEnabled = false;

			Console.WriteLine("Expecting ReadValue to be {0} and it is {1}", 33, Interface.ReadValue);
			System.Diagnostics.Debug.Assert(Interface.ReadValue == 33);

		}
	}
}

