using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleAzureIoTCerts
{
    public class Helpers
    {
        public static byte[] LoadEmbeddedFile(string filename)
        {
            Console.WriteLine("Loading embedded file " + filename);
            Assembly assembly = Assembly.GetEntryAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("SimpleAzureIoTCerts.Resources." + filename))
            {
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
        }

        public static byte[] LoadLocalFile(string filename)
        {
            Console.WriteLine("Loading local file " + filename);
            byte[] ba = System.IO.File.ReadAllBytes(filename);
            return ba;
        }
          

    }
}
