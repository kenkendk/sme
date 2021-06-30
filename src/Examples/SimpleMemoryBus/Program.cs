using SME;
using System;
using System.IO;
using System.Linq;

namespace SimpleMemoryBus
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            new Simulation()
                .BuildCSVFile()
                .BuildVHDL()
                .Run(
                    // Same scope, but we make the memory first
                    new MockMemory(),
                    // Then we connect to the named bus
                    new MemoryTester(),

                    // Then, in the same scope we make a new bus
                    new SimpleMockMemory(),
                    // And connect to that
                    new MemoryTester()
                );


        }
    }
}
