using SME;
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
        public ImageInputConfiguration Configuration;

        [InputBus]
        public PaddedInputLine Data;

        [OutputBus]
        public ImageFragment Output = Scope.CreateBus<ImageFragment>();

        [InternalBus]
        public IInternal Internal = Scope.CreateInternalBus<IInternal>();

        private byte[] m_buffer = new byte[COLOR_WIDTH * STENCIL_HEIGHT * MAX_IMAGE_WIDTH];

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

                if (sourcepos > maxindex)
                {
                    var minindex = (centerindex - half_dist) * COLOR_WIDTH;
                    for (var i = 0; i < STENCIL_HEIGHT; i++)
                    {
                        for (var j = 0; j < STENCIL_WIDTH; j++)
                        {
                            var bufix = (minindex + (((Internal.InputWidth * i) + j) * COLOR_WIDTH)) % m_buffer.Length;
                            var outix = ((STENCIL_WIDTH * i) + j) * COLOR_WIDTH;

                            for (var k = 0; k < COLOR_WIDTH; k++)
                            {
                                if (ix + 3 == bufix && Data.IsValid)
                                {
                                    Output.Data[outix + k] = Data.Color[k];
                                }
                                else
                                {
                                    Output.Data[outix + k] = m_buffer[bufix + k];
                                }
                            }
                        }
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
