using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Views
{
    class SortForOrthoView
    {
        public void ShowResult(string[] images)
        {
            int count = 0;
            for (int i = 0; i < images.Length; i++)
            {
                string s = images[i];
                Console.WriteLine(s);
                count++;
            }

            Console.WriteLine("Found " + count + " images");
            Console.Read();
        }

        public void ConfigError()
        {
            Console.WriteLine("Konfigurationen är knas, programmet kan ej fortsätta!");
            Console.Read();
        }
    }
}
