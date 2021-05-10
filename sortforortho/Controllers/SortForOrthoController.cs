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
using System.Text.RegularExpressions;

namespace sortforortho.Controllers
{
    class SortForOrthoController
    {

        private SortForOrthoView _view;
        private PhotoSorter _ps;
        private XMLReader _xmlReader;

        public SortForOrthoController(SortForOrthoView view, PhotoSorter ps, XMLReader xmlReader)
        {
            this._view = view;
            this._ps = ps;
            this._xmlReader = xmlReader;
        }

        public void StartApp()
        {
            string pathToXml = "./config.xml";
            string pathToShapeFile = "./ImageShape";
            string pathToFiles;
            float sensorWidth;
            float overlapPercentage = 50;
            int maxSecondsBetweenImages = 0;
            bool searchRecursive;
            string[] filters;
            string[] filePaths;
            List<Image> imageList = new List<Image>();

            try
            {
                pathToFiles = _xmlReader.ReadValueFromXML(pathToXml, "pathToFiles");

                if (!float.TryParse(_xmlReader.ReadValueFromXML(pathToXml, "sensorWidth"), out sensorWidth))
                {
                    _view.ParsingError(1);
                    System.Environment.Exit(-1);
                }

                if (!float.TryParse(_xmlReader.ReadValueFromXML(pathToXml, "overlapPercentage"), out overlapPercentage))
                {
                    _view.ParsingError(2);
                    System.Environment.Exit(-1);
                }

                if (!bool.TryParse(_xmlReader.ReadValueFromXML(pathToXml, "searchRecursive"), out searchRecursive))
                {
                    _view.ParsingError(3);
                    System.Environment.Exit(-1);
                }

                if (!Int32.TryParse(_xmlReader.ReadValueFromXML(pathToXml, "maxSecondsBetweenImages"), out maxSecondsBetweenImages))
                {
                    _view.ParsingError(4);
                    System.Environment.Exit(-1);
                }


                string filtersString = _xmlReader.ReadValueFromXML(pathToXml, "filter");
                filters = filtersString.Split(',');

                filePaths = GetFilePathsFrom(@pathToFiles, filters, searchRecursive);

                _view.ShowResult(filePaths);

                foreach (string filePath in filePaths)
                {
                    imageList.Add(CreateImage(filePath, sensorWidth));    
                }

                _view.ImageListCreated();
            }
            catch (Exception e)
            {
                _view.ConfigError(e);
            }

            List<List<String>> photosInBatches = _ps.SortForOrtho(imageList, overlapPercentage, maxSecondsBetweenImages, pathToShapeFile);



            if (_view.ShowSortOptions())
            {
                _ps.PutFilesInDirectories(_xmlReader.ReadValueFromXML(pathToXml, "pathToSortedBatches"), photosInBatches);
            }
            Console.ReadLine();
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

        private Image CreateImage(string filePath, float sensorWidth)
        {
            Image img = new Image();

            // Get metadata directories
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePath);

            // Get info from gps-directory
            GpsDirectory gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            GeoLocation centerPoint = gpsDirectory.GetGeoLocation();

            // Get info from exif-directory
            ExifSubIfdDirectory subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            string createDate = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

            // Get ImageSize
            int imageWidth;
            if (!Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth), @"\d+").Value, out imageWidth))
            {
                _view.ParsingError(5);
            }

            int imageHeight;
            if (!Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageHeight), @"\d+").Value, out imageHeight))
            {
                _view.ParsingError(6);
            }

            // Get focal length
            float focalLength = 0.0f;
            if (!float.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength), @"\d+,\d").Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out focalLength))
                _view.ParsingError(7);

            /**
             * Get info from xmp-directory
             */
            XmpDirectory xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();

            // Get altitude and flight yaw angle
            float flightYawDegree = 0.0f;
            float gimbalYawDegree = 0.0f;
            float altitude = 0.0f;
            foreach (var property in xmpDirectory.XmpMeta.Properties)
            {
                if (String.Equals(property.Path, "drone-dji:RelativeAltitude"))
                {
                    if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out altitude))
                    {
                        _view.ParsingError(8);
                    }
                }

                if (String.Equals(property.Path, "drone-dji:FlightYawDegree"))
                {
                    if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out flightYawDegree))
                    {
                        _view.ParsingError(9);
                    }
                }

                if (String.Equals(property.Path, "drone-dji:GimbalYawDegree"))
                {
                    if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out gimbalYawDegree))
                    {
                        _view.ParsingError(10);
                    }
                }
            }

            img.Path = filePath;
            img.CenterPoint = centerPoint;
            img.Altitude = altitude;
            img.SensorWidth = sensorWidth;
            img.FocalLength = focalLength;
            img.ImageHeight = imageHeight;
            img.ImageWidth = imageWidth;
            img.GimbalYawDegree = gimbalYawDegree;
            img.CreateDate = createDate;
            
            return img;
        }


    }
}
