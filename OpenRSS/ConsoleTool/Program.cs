using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using OpenRSS.Cache;

namespace ConsoleTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileStore = FileStore.Open(@"C:\Users\Alex\.Hydrascape32\runescape");
            //fileStore.GetFileCount()
            File.WriteAllText("C:/Cache.json", JsonConvert.SerializeObject(fileStore, Formatting.Indented));
            Console.ReadLine();
        }
    }
}
