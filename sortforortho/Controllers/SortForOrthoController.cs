﻿using MetadataExtractor;
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
        private DataCreator dc = new DataCreator();

        public SortForOrthoController(SortForOrthoView view)
        {
            this._view = view;
        }

        public void StartApp()
        {
            string path;
            float sensorWidth;
            bool searchRecursive;
            string[] filters;
            string[] filePaths;
            List<Image> imageList = new List<Image>();

            try
            {
                path = ConfigurationManager.AppSettings.Get("pathToFiles");

                sensorWidth = float.Parse(ConfigurationManager.AppSettings.Get("sensorWidth"));
                searchRecursive = bool.Parse(ConfigurationManager.AppSettings.Get("searchRecursive"));
                string filtersString = ConfigurationManager.AppSettings.Get("filter");
                filters = filtersString.Split(',');
                filePaths = GetFilePathsFrom(@path, filters, searchRecursive);
                _view.ShowResult(filePaths);
                Console.Read();

                foreach (string filePath in filePaths)
                {
                    imageList.Add(CreateImage(filePath, sensorWidth));
                }
               
                Console.WriteLine("Imagelist created!");


                
                Console.Read();
            }
            catch
            {
                _view.ConfigError();
            }

            dc.CreateShapeFile(imageList);

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
            string dtStr = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
            string photoTaken = dtStr.Remove(4, 1).Insert(4, "-").Remove(7, 1).Insert(7, "-");

            // Get ImageSize
            int imageWidth;
            if (!Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth), @"\d+").Value, out imageWidth))
            {
                _view.ParsingError("image width");
            }

            int imageHeight;
            if (!Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageHeight), @"\d+").Value, out imageHeight))
            {
                _view.ParsingError("image height");
            }

            // Get focal length
            float focalLength = 0.0f;
            if (!float.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength), @"\d+,\d").Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out focalLength))
                _view.ParsingError("focal length");

            /**
             * Get info from xmp-directory
             */
            XmpDirectory xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();

            // Get altitude and flight yaw angle
            float flightYawDegree = 0.0f;
            float altitude = 0.0f;
            foreach (var property in xmpDirectory.XmpMeta.Properties)
            {
                if (String.Equals(property.Path, "drone-dji:RelativeAltitude"))
                {
                    if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out altitude))
                    {
                        _view.ParsingError("altitude");
                    }
                }

                if (String.Equals(property.Path, "drone-dji:FlightYawDegree"))
                {
                    if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out flightYawDegree))
                    {
                        _view.ParsingError("flight yaw degree");
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
            img.PhotoTaken = photoTaken;
            img.CornerCoordinates = img.GetCoordinateList(centerPoint, imageHeight, imageWidth, sensorWidth, altitude, focalLength, flightYawDegree);
            
            return img;
        }
    }
}
