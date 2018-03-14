using System;
namespace SME.VHDL.CustomRenders
{
    public class BlockRamConfig
    {
        public readonly bool use36k;
        public readonly int instancemem;

        public readonly string targetdevice = "7SERIES";
        public readonly int linewidth = 256;

        public readonly int realaddrwidth;
        public readonly int paritybits;
        public readonly int wewidth;

        public readonly int datawidth;

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
