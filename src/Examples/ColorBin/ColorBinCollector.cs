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
				//R=0.299, G=0.587, B=0.114
				var color = ((Input.R * 299u) + (Input.G * 587u) + (Input.B * 114u)) / 1000u;
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
