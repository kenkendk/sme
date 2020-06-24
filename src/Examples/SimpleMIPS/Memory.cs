using System;
using System.IO;
using SME;

namespace SimpleMIPS
{
    public enum MemoryConstants
    {
        max_addr = 1024,
    }

    [ClockedProcess]
    public class Memory : SimpleProcess
    {
        public Memory(string program_name)
        {
            // Read the binary file in the given path
            using (var reader = new BinaryReader(File.Open(program_name, FileMode.Open)))
            {
                int position = 0;
                int length = (int)reader.BaseStream.Length;
                while (position < length)
                {
                    mem[position >> 2] = reader.ReadUInt32();
                    position += sizeof(UInt32);
                }
                mem[position >> 2] = 0xFFFFFFFF; // Put in a terminate instruction
            }
        }

        [InputBus]
        public MemoryInput input;

        [OutputBus]
        public MemoryOutput output = Scope.CreateBus<MemoryOutput>();

        public uint[] mem = new uint[(uint)MemoryConstants.max_addr];

        protected override void OnTick()
        {
            if (input.ena)
            {
                output.rddata = mem[input.addr];
                if (input.wrena)
                    mem[input.addr] = input.wrdata;
            }
        }
    }
}
