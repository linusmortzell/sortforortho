using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
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
            string sensorWidth;
            bool searchRecursive;
            string[] filters;
            string[] filePaths;

            try
            {
                path = ConfigurationManager.AppSettings.Get("pathToFiles");

                // Should be replaced with data from database
                sensorWidth = ConfigurationManager.AppSettings.Get("sensorWidth");
                searchRecursive = bool.Parse(ConfigurationManager.AppSettings.Get("searchRecursive"));
                string filtersString = ConfigurationManager.AppSettings.Get("filter");
                filters = filtersString.Split(',');
                filePaths = GetFilePathsFrom(@path, filters, searchRecursive);
                _view.ShowResult(filePaths);
                Console.Read();

                // Get metadata directories
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePaths[0]);

                // Get info from gps-directory
                GpsDirectory gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
                GeoLocation geo = gpsDirectory.GetGeoLocation();

                // Get info from exif-directory
                ExifSubIfdDirectory subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                string dtStr = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                string photoTaken = dtStr.Remove(4, 1).Insert(4, "-").Remove(7, 1).Insert(7, "-");

                // Get ImageSize
                int imageWidth;
                if (Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth), @"\d+").Value, out imageWidth))
                {
                    Console.WriteLine("OK");
                } else
                {
                    Console.WriteLine("Bilden kastas bort eftersom den inte har tillräcklig metadata");
                }
                int imageHeight;
                if (Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageHeight), @"\d+").Value, out imageHeight))
                {
                    Console.WriteLine("OK");
                }
                else
                {
                    Console.WriteLine("Bilden kastas bort eftersom den inte har tillräcklig metadata");
                }

                string focalLength = Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength), @"\d+,\d").Value;

                // Get info from xmp-directory
                XmpDirectory xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();
                string altitude = null;
                string flightYawDegree = null;


                foreach (var property in xmpDirectory.XmpMeta.Properties)
                {
                    // Console.WriteLine($"Path={property.Path} Value={property.Value}");
                    if (String.Equals(property.Path, "drone-dji:RelativeAltitude"))  
                    {
                        altitude = property.Value;   
                    }

                    if (String.Equals(property.Path, "drone-dji:FlightYawDegree"))
                    {
                        flightYawDegree = property.Value;
                    }
                }

                Console.WriteLine("Path: " + filePaths[0] + "\nLocation: " + geo + "\nAltitude: " + altitude + "\nSensor width: " + sensorWidth + "\nFocal Length: " + focalLength + "\nImage width: " + imageWidth + "\nImage height: " + imageHeight + "\nPhoto taken: " + photoTaken + "\nFlight Yaw Degree: " + flightYawDegree);
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
