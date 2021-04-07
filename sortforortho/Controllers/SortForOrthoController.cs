using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using sortforortho.Models;
using sortforortho.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sortforortho.Controllers
{
    class SortForOrthoController
    {

        private SortForOrthoView _view;

        public SortForOrthoController(SortForOrthoView view)
        {
            this._view = view;
        }

        public void StartApp()
        {
            string path;
            bool searchRecursive;
            string[] filters;
            string[] filePaths;

            try
            {
                path = ConfigurationManager.AppSettings.Get("pathToFiles");
                searchRecursive = bool.Parse(ConfigurationManager.AppSettings.Get("searchRecursive"));
                string filtersString = ConfigurationManager.AppSettings.Get("filter");
                filters = filtersString.Split(',');
                filePaths = GetFilePathsFrom(@path, filters, searchRecursive);
                _view.ShowResult(filePaths);
                Console.Read();

                /*
                 * Metadata
                 */
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePaths[0]);

                // Get info from gps-directory
                GpsDirectory gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
                GeoLocation geo = gpsDirectory.GetGeoLocation();
                string altitude = gpsDirectory.GetDescription(GpsDirectory.TagAltitude);


                // Get info from exif-directory
                ExifSubIfdDirectory subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                string dateTime = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                int imageWidth = int.Parse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth), @"\d+").Value, NumberFormatInfo.InvariantInfo);
                string focalLength = Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength), @"\d+,\d").Value;

                Console.WriteLine(focalLength);
                Console.Read();
            }
            catch
            {
                _view.ConfigError();
            }
        }

        private string[] GetFilePathsFrom(string searchFolder, string[] filters, bool isRecursive)
        {
            List<string> filesFound = new List<string>();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(System.IO.Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound.ToArray();
        }


    }
}
