using System.Configuration;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = ConfigurationManager.AppSettings.Get("path");
            Console.WriteLine(path);
            Console.Read();
        }
    }
}
