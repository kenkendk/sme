using System;
using SME;
using static NoiseFilter.StencilConfig;

namespace NoiseFilter
{
	public class BorderEmitter : SimpleProcess
	{
		public interface IInternal : IBus
		{
			[InitialValue]
			bool HasSize { get; set; }

			ushort Width { get; set; }
			ushort Height { get; set; }

			uint SourcePixels { get; set; }
			uint TargetPixels { get; set; }

			uint SourceIndex { get; set; }
			uint TargetIndex { get; set; }
		}

		[InputBus]
		private ImageInputConfiguration Configuration;

		[InputBus]
		private ImageInputLine Input;

		[OutputBus]
		private PaddedInputLine Output;

		[OutputBus]
		private BorderDelayUpdate Delay;

		[InternalBus]
		private IInternal Internal;

		/// <summary>
		/// A buffer to hold all border pixels
		/// </summary>
		private byte[] m_buffer = new byte[(((MAX_IMAGE_WIDTH + BORDER_SIZE) * 2) + (MAX_IMAGE_HEIGHT * 2)) * COLOR_WIDTH];


		protected override void OnTick()
		{
			//DebugOutput = true;
			Delay.IsReady = false;
			Output.IsValid = false;

			if (!Internal.HasSize)
			{
				if (Configuration.IsValid)
				{
					Internal.HasSize = true;
					Internal.Width = Configuration.Width;
					Internal.Height = Configuration.Height;
					Internal.SourceIndex = 0;
					Internal.TargetIndex = 0;
					Internal.SourcePixels = (uint)(Configuration.Width * Configuration.Height);
					Internal.TargetPixels = (uint)((Configuration.Width + (BORDER_SIZE * 2)) * (Configuration.Height + (BORDER_SIZE * 2)));
				}
				else
					Delay.IsReady = true;					
			}
			else
			{
				if (Input.IsValid && Internal.SourceIndex < Internal.SourcePixels)
				{
					var ix = (Internal.SourceIndex * COLOR_WIDTH) % m_buffer.Length;
					for (var i = 0; i < COLOR_WIDTH; i++)
						m_buffer[ix + i] = Input.Color[i];
					
					Internal.SourceIndex++;
				}

				if (Internal.TargetIndex < Internal.TargetPixels)
				{
					var targetx = (int)(Internal.TargetIndex % (Internal.Width + (BORDER_SIZE * 2)));
					var targety = (int)(Internal.TargetIndex / (Internal.Width + (BORDER_SIZE * 2)));

					int sourcex;
					int sourcey;

					if (targetx == 0)
						sourcex = 0;
					else if (targetx == Internal.Width + (BORDER_SIZE * 2) - 1)
						sourcex = Internal.Width - 1;
					else
						sourcex = targetx - 1;

					if (targety == 0)
						sourcey = 0;
					else if (targety == Internal.Height + (BORDER_SIZE * 2) - 1)
						sourcey = Internal.Height - 1;
					else
						sourcey = targety - 1;

					var sourceix = Internal.Width * sourcey + sourcex;
					if (sourceix == Internal.SourceIndex && Input.IsValid)
					{
						Output.IsValid = true;
						for (var i = 0; i < COLOR_WIDTH; i++)
							Output.Color[i] = Input.Color[i];
						Internal.TargetIndex++;

						//PrintDebug("Border -> source pixel {0}x{1}, values: {2},{3},{4}", sourcex, sourcey, Input.Color[0], Input.Color[1], Input.Color[2]);
						PrintDebug("Border -> target pixel {0}x{1}, values: {2},{3},{4}", targetx, targety, Input.Color[0], Input.Color[1], Input.Color[2]);

					}
					else if (sourceix < Internal.SourceIndex)
					{
						var ix = (sourceix * COLOR_WIDTH) % m_buffer.Length;

						Output.IsValid = true;
						for (var i = 0; i < COLOR_WIDTH; i++)
							Output.Color[i] = m_buffer[ix + i];

						Internal.TargetIndex++;
						//PrintDebug("Border -> source pixel {0}x{1}, values: {2},{3},{4}", sourcex, sourcey, m_buffer[ix + 0], m_buffer[ix + 1], m_buffer[ix + 2]);
						PrintDebug("Border -> target pixel {0}x{1}, values: {2},{3},{4}", targetx, targety, m_buffer[ix + 0], m_buffer[ix + 1], m_buffer[ix + 2]);
					}

					if (Internal.TargetIndex == Internal.TargetPixels - 1)
					{
						PrintDebug("Wrote final pixel {0}", Internal.TargetIndex);
						Delay.IsReady = true;
						Internal.HasSize = false;
					}
				}
				else
				{
					PrintDebug("Ahead of source?");
				}
			}
		}
	}
}
