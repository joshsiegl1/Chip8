using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chip8.emu
{
    public class Loader
    {
        public Loader()
        {

        }

        public static string ByteArrayToString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", ""); 
        }


        public byte[] LoadFile()
        {
            Console.WriteLine("Please select a file to load"); 

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"roms\IBM.ch8");
            byte[] file = File.ReadAllBytes(path);

            return file; 
        }
    }
}
