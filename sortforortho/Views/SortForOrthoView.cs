﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Views
{
    class SortForOrthoView
    {
        public void ShowResult(string[] filePaths)
        {
            int count = 0;
            foreach (string filePath in filePaths)
            {
                Console.WriteLine(filePath);
                count++;
            }

            Console.WriteLine("Found " + count + " images");
            Console.Read();
        }

        public void ConfigError()
        {
            Console.WriteLine("Error occurred, check configuration!");
            Console.Read();
        }

        public void ParsingError(string error)
        {
            Console.WriteLine("Error parsing " + error + ".");
        }
    }
}
