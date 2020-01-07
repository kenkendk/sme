using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SME;

namespace SimpleMIPS
{
    public enum MemoryConstants
    {
        max_addr = 4096,
    }

    [ClockedProcess]
    public class Memory : SimpleProcess
    {
        public Memory(string program_name) 
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resource_name = assembly.GetManifestResourceNames().First(x => x.EndsWith(program_name));
            // Read the binary file in the given path
            using (var reader = new BinaryReader(assembly.GetManifestResourceStream(resource_name)))
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
        MemoryInput input = Scope.CreateOrLoadBus<MemoryInput>();

        [OutputBus]
        MemoryOutput output = Scope.CreateOrLoadBus<MemoryOutput>();

        uint[] mem = new uint[(uint)MemoryConstants.max_addr];

        protected override void OnTick()
        {
            if (input.ena) 
            {
                output.rddata = mem[input.addr];
                if (input.wrena) 
                {
                    Console.WriteLine("mem[{0}] = {1}", input.addr, input.wrdata);
                    mem[input.addr] = input.wrdata;
                }
            }
        }
    }
}
