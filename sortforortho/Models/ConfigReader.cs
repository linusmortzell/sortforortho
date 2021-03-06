using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class ConfigReader
    {
        public SortForOrthoConfig GetConfigInfo()
        {
            string pathToFiles;
            string pathToSortedBatches;
            string pathToShapeFile;
            float sensorWidth;
            float overlapPercentage;
            int maxSecondsBetweenImages;
            bool searchRecursive;
            string[] filters;
            string odmUrl;


            pathToFiles = ConfigurationManager.AppSettings.Get("pathToFiles");
            if (!System.IO.Directory.Exists(pathToFiles))
                throw new FormatException("pathToFiles");

            pathToSortedBatches = ConfigurationManager.AppSettings.Get("pathToSortedBatches");
            pathToShapeFile = ConfigurationManager.AppSettings.Get("pathToShapeFile");
            odmUrl = ConfigurationManager.AppSettings.Get("nodeOdmUrl");


            if (!float.TryParse(ConfigurationManager.AppSettings.Get("sensorWidth"), out sensorWidth))
            {
                throw new FormatException("sensorWidth");
            }
            if (!float.TryParse(ConfigurationManager.AppSettings.Get("overlapPercentage"), out overlapPercentage))
            {
                throw new FormatException("overlapPercentage");
            }

            if (!bool.TryParse(ConfigurationManager.AppSettings.Get("searchRecursive"), out searchRecursive))
            {
                throw new FormatException("searchRecursive");
            }

            if (!Int32.TryParse(ConfigurationManager.AppSettings.Get("maxSecondsBetweenImages"), out maxSecondsBetweenImages))
            {
                throw new FormatException("maxSecondsBetweenImages");
            }


            string filtersString = ConfigurationManager.AppSettings.Get("filter");
            if (filtersString != null)
            {
                filters = filtersString.Split(',');
            }
            else throw new FormatException("filter");

            return new SortForOrthoConfig(pathToFiles, pathToSortedBatches, pathToShapeFile, sensorWidth, overlapPercentage, maxSecondsBetweenImages, searchRecursive, filters, odmUrl);
        }
    }
}
