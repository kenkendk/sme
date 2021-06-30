using System;

namespace SME.VHDL.CustomRenders
{
    /// <summary>
    /// A configuration of a Xilinx block RAM.
    /// <summary>
    public class BlockRamConfig
    {
        /// <summary>
        /// Flag indicating whether the 36k block RAM primitive should be used.
        /// </summary>
        public readonly bool use36k;
        /// <summary>
        /// Integer indicating the size of the block RAM primitive.
        /// </summary>
        public readonly int instancemem;

        /// <summary>
        /// String indicating the target device.
        /// </summary>
        public readonly string targetdevice = "7SERIES";
        /// <summary>
        /// The size of a line?
        /// </summary>
        public readonly int linewidth = 256;

        /// <summary>
        /// The width of the address bus for the block RAM primitive.
        /// </summary>
        public readonly int realaddrwidth;
        /// <summary>
        /// The amount of parity bits.
        /// </summary>
        public readonly int paritybits;
        /// <summary>
        /// The width of the write enable bus for the block RAM primitive.
        /// </summary>
        public readonly int wewidth;

        /// <summary>
        /// The width of the data bus for the block RAM primitive.
        /// </summary>
        public readonly int datawidth;

        /// <summary>
        /// Constructs a new instance of the block RAM configuration.
        /// </summary>
        /// <param name="renderer">The renderer currently rendering VHDL files.</param>
        /// <param name="datawidth">The width of the data bus.</param>
        /// <param name="memorysize">The size of the RAM.</param>
        /// <param name="isTrueDual">Flag indicating whether a true dual port RAM should be generated.</param>
        public BlockRamConfig(RenderStateProcess renderer, int datawidth, int memorysize, bool isTrueDual)
        {
            if (renderer.Parent.Config.DEVICE_VENDOR != FPGAVendor.Xilinx)
                throw new Exception("Blockram is only supported on Xlinix devices for now");

            if (memorysize > 36 * 1024)
                throw new Exception($"Unable to generate block ram with {memorysize} bits as the device only supports up to {36 * 1024} bits");

            use36k = (memorysize > 18 * 1024) || datawidth >= (isTrueDual ? 19 : 37);
            instancemem = use36k ? 1024 * 36 : 1024 * 18;

            targetdevice = "7SERIES";
            linewidth = 256;
            this.datawidth = datawidth;

            if (datawidth == 1)
            {
                paritybits = 0;
                wewidth = 1;
                realaddrwidth = use36k ? 15 : 14;
            }
            else if (datawidth == 2)
            {
                paritybits = 0;
                wewidth = 1;
                realaddrwidth = use36k ? 14 : 13;
            }
            else if (datawidth <= 4)
            {
                paritybits = 0;
                wewidth = 1;
                realaddrwidth = use36k ? 13 : 12;

            }
            else if (datawidth <= 9)
            {
                paritybits = Math.Max(datawidth - 8, 0);
                wewidth = 1;
                realaddrwidth = use36k ? 12 : 11;
            }
            else if (datawidth <= 18)
            {
                paritybits = Math.Max(datawidth - 16, 0);
                wewidth = 2;
                realaddrwidth = use36k ? 11 : 10;

            }
            else if (datawidth <= 36)
            {
                paritybits = Math.Max(datawidth - 32, 0);
                wewidth = 4;
                realaddrwidth = use36k ? 10 : 9;

            }
            else if (datawidth <= 72)
            {
                paritybits = Math.Max(datawidth - 64, 0);
                wewidth = 8;
                realaddrwidth = 9;
            }
            else
            {
                throw new Exception("Xilinx devices do not support more than 72 bit data width");
            }
        }
    }
}
