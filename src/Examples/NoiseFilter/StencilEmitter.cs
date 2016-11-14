using SME;
using System;
using static NoiseFilter.StencilConfig;

namespace NoiseFilter
{
	/// <summary>
	/// Process that reads in pixels, 
	/// one each clock cycle,
	/// and outputs fragments the size of the stencil area
	/// </summary>
	public class StencilEmitter : SimpleProcess
	{
		public interface IInternal : IBus
		{
			[InitialValue]
			bool HasSize { get; set; }

			ushort InputWidth { get; set; }
			ushort InputHeight { get; set; }
			uint InputPixels { get; set; }
			uint InputIndex { get; set; }

			uint OutputIndex { get; set; }
			ushort OutputWidth { get; set; }
			ushort OutputHeight { get; set; }
			uint OutputPixels { get; set; }
		}

		[InputBus]
		private ImageInputConfiguration Configuration;

		[InputBus]
		private PaddedInputLine Data;

		[OutputBus]
		private ImageFragment Output;

		[InternalBus]
		private IInternal Internal;

		private readonly byte[] m_buffer = new byte[COLOR_WIDTH * STENCIL_HEIGHT * MAX_IMAGE_WIDTH];

		protected override void OnTick()
		{
			Output.IsValid = false;

			if (!Internal.HasSize)
			{
				if (Configuration.IsValid)
				{
					Internal.HasSize = true;
					Internal.InputWidth = (ushort)(Configuration.Width + (BORDER_SIZE * 2));
					Internal.InputHeight = (ushort)(Configuration.Height + (BORDER_SIZE * 2));
					Internal.InputIndex = 0;
					Internal.InputPixels = (uint)((Configuration.Width + (BORDER_SIZE * 2)) * (Configuration.Height + (BORDER_SIZE * 2)));

					Internal.OutputIndex = 0;
					Internal.OutputPixels = (uint)(Configuration.Width * Configuration.Height);
					Internal.OutputWidth = Configuration.Width;
					Internal.OutputHeight = Configuration.Height;
				}
			}
			else
			{
				var ix = Internal.InputIndex % m_buffer.Length;

				if (Data.IsValid)
				{
					for (var i = 0; i < COLOR_WIDTH; i++)
						m_buffer[ix + i] = Data.Color[i];
					
					Internal.InputIndex += COLOR_WIDTH;
				}

				var outputx = Internal.OutputIndex % Internal.OutputWidth;
				var outputy = Internal.OutputIndex / Internal.OutputWidth;

				var centerindex = (outputy + (STENCIL_HEIGHT - 1) / 2) * Internal.InputWidth + outputx + ((STENCIL_WIDTH - 1) / 2);

				// Half the length of the stencil, counting in pixels in the input image
				var half_dist = ((STENCIL_HEIGHT - 1) / 2) * Internal.InputWidth + ((STENCIL_WIDTH - 1) / 2);

				var maxindex = centerindex + half_dist;

				var sourcepos = (Internal.InputIndex / COLOR_WIDTH) + (Data.IsValid ? 1 : 0);

				//if (Internal.OutputIndex >= 47)
				//	Console.WriteLine("{0}: {1} > {2}", Internal.OutputIndex, sourcepos, maxindex);

				if (sourcepos > maxindex)
				{
					//Console.WriteLine("Stencil {0} {1}x{2} ->", Internal.OutputIndex, outputx, outputy);
					
					var minindex = (centerindex - half_dist) * COLOR_WIDTH;
					for (var i = 0; i < STENCIL_HEIGHT; i++)
					{
						for (var j = 0; j < STENCIL_WIDTH; j++)
						{
							var bufix = (minindex + (((Internal.InputWidth * i) + j) * COLOR_WIDTH)) % m_buffer.Length;
							var outix = ((STENCIL_WIDTH * i) + j) * COLOR_WIDTH;

							//Console.Write("{0} => {1}: ", bufix / 3, outix / 3);

							for (var k = 0; k < COLOR_WIDTH; k++)
							{
								if (ix + 3 == bufix && Data.IsValid)
								{
									Output.Data[outix + k] = Data.Color[k];
									//Console.Write("{0}, ", Data.Color[k]);
								}
								else
								{
									Output.Data[outix + k] = m_buffer[bufix + k];
									//Console.Write("{0}, ", m_buffer[bufix + k]);
								}
							}

							//Console.Write(" ");
						}

						//Console.WriteLine();
					}

					Output.IsValid = true;
					Output.Index = Internal.OutputIndex + 1;
					Internal.OutputIndex++;

					if (Internal.OutputIndex == Internal.OutputPixels - 1)
						Internal.HasSize = false;
				}
			}
		}
	}
}
