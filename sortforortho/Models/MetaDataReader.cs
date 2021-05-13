using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class MetaDataReader
    {
        public Image CreateImage(string filePath, float sensorWidth)
        {
            IEnumerable<MetadataExtractor.Directory> directories;
            GeoLocation centerPoint;
            string createDate = "";
            Image img = new Image();
            int imageWidth = 0;
            int imageHeight = 0;
            float focalLength = 0.0f;
            float flightYawDegree = 0.0f;
            float gimbalYawDegree = 0.0f;
            float altitude = 0.0f;

            // Get metadata directories
            try
            {
                directories = ImageMetadataReader.ReadMetadata(filePath);
            }
            catch
            {
                throw new Exception("No metadata directories");
            }

            // Get info from gps-directory
            GpsDirectory gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDirectory != null)
            {
                centerPoint = gpsDirectory.GetGeoLocation();
            } else throw new Exception("No gps directory"); ;


            // Get info from exif-directory
            ExifSubIfdDirectory subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDirectory != null)
            {
                createDate = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

                // Get ImageSize
                if (!Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth), @"\d+").Value, out imageWidth))
                {
                    throw new FormatException("image width");
                }

                if (!Int32.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageHeight), @"\d+").Value, out imageHeight))
                {
                    throw new FormatException("image height");
                }

                // Get focal length
                if (!float.TryParse(Regex.Match(subIfdDirectory?.GetDescription(ExifDirectoryBase.TagFocalLength), @"\d+,\d").Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out focalLength))
                    throw new FormatException("focal length");
            }

            /**
             * Get info from xmp-directory
             */
            XmpDirectory xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();
            if (xmpDirectory != null)
            {
                // Get altitude and flight yaw angle

                foreach (var property in xmpDirectory.XmpMeta.Properties)
                {
                    if (String.Equals(property.Path, "drone-dji:RelativeAltitude"))
                    {
                        if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out altitude))
                        {
                            throw new FormatException("altitude");
                        }
                    }

                    if (String.Equals(property.Path, "drone-dji:FlightYawDegree"))
                    {
                        if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out flightYawDegree))
                        {
                            throw new FormatException("flight yaw degree");
                        }
                    }

                    if (String.Equals(property.Path, "drone-dji:GimbalYawDegree"))
                    {
                        if (!float.TryParse(property.Value.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out gimbalYawDegree))
                        {
                            throw new FormatException("gimbal yaw degree");
                        }
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
            img.GimbalYawDegree = flightYawDegree;
            img.CreateDate = createDate;
            
            return img;
        }
    }
}
