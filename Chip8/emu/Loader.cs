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

            string[] fileEntries = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"roms\"));
            int i = 0; 
            foreach (string fileName in fileEntries)
            {
                ++i;
                string[] split = fileName.Split('\\');
                string _fileName = split[split.Length - 1]; 
                Console.WriteLine(i.ToString() + ". " + _fileName); 
            }

            int input = 0;
            int.TryParse(Console.ReadLine(), out input);

            string fileSelect = fileEntries[input - 1]; 

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"roms\", fileSelect);
            byte[] file = File.ReadAllBytes(path);

            return file; 
        }
    }
}
