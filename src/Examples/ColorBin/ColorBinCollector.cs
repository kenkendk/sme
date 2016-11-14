using SME;
using System;

namespace ColorBin
{
	public class ColorBinCollector : SimpleProcess
	{
		[InputBus]
		ImageInputLine Input;

		[InputBus, OutputBus]
		BinCountOutput Output;

		const uint HighThreshold = 200;
		const uint MediumThreshold = 100;

		protected override void OnTick()
		{
			var countlow = Output.Low;
			var countmed = Output.Medium;
			var counthigh = Output.High;

			if (Output.IsValid)
				countlow = countmed = counthigh = 0;
			
			if (Input.IsValid)
			{
				var color = (Input.R + Input.G + Input.B) / 3;
				if (color > HighThreshold)
					counthigh++;
				else if (color > MediumThreshold)
					countmed++;
				else
					countlow++;
			}

			Output.Low = countlow;
			Output.Medium = countmed;
			Output.High = counthigh;
			Output.IsValid = Input.IsValid && Input.LastPixel;
		}
	}
}
