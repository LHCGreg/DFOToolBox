using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.NpkReader.Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NpkReader npkReader = new NpkReader(@"C:\Neople\DFO\ImagePacks2\sprite_worldmap_act1.NPK"))
            {
                Console.WriteLine("Success!");
            }
        }
    }
}
