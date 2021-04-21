using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class Image
    {
        private string _path;
        private GeoLocation _centerPoint;
        private GeoLocation _centerPointSweref;
        private float _altitude;
        private float _sensorWidth;
        private float _focalLength;
        private float _flightYawDegree;
        private int _imageWidth;
        private int _imageHeight;
        private string _photoTaken;
        private List<GeoLocation> _cornerCoordinates;
        public string Path
        {
            get {
                return _path;
            }
            set
            {
                _path = value;
            }
        }

        public GeoLocation CenterPoint
        {
            get
            {
                return _centerPoint;
            }
            set
            {
                _centerPoint = value;
            }
        }

        public float Altitude
        {
            get
            {
                return _altitude;
            }
            set
            {
                _altitude = value;
            }
        }

        public float SensorWidth
        {
            get
            {
                return _sensorWidth;
            }
            set
            {
                _sensorWidth = value;
            }
        }


        public float FocalLength
        {
            get
            {
                return _focalLength;
            }
            set
            {
                _focalLength = value;
            }
        }

        public float FlightYawDegree
        {
            get
            {
                return _flightYawDegree;
            }
            set
            {
                _flightYawDegree = value;
            }
        }

        public int ImageWidth
        {
            get
            {
                return _imageWidth;
            }
            set
            {
                _imageWidth = value;
            }
        }

        public int ImageHeight
        {
            get
            {
                return _imageHeight;
            }
            set
            {
                _imageHeight = value;
            }
        }

        public string PhotoTaken
        {
            get
            {
                return _photoTaken;
            }
            set
            {
                _photoTaken = value;
            }
        }

        public List<GeoLocation> CornerCoordinates
        {
            get
            {
                return _cornerCoordinates;
            }

            set
            {
                _cornerCoordinates = value;
            }
        }


        public Image(string path, GeoLocation centerPoint, float altitude, float sensorWidth, float focalLength, int imageWidth, string photoTaken, List<GeoLocation> cornerCoordinates)
        {
            this._path = path;
            this._centerPoint = centerPoint;
            this._altitude = altitude;
            this._sensorWidth = sensorWidth;
            this._focalLength = focalLength;
            this._imageWidth = imageWidth;
            this._imageHeight = ImageHeight;
            this._photoTaken = photoTaken;
            this._cornerCoordinates = cornerCoordinates;
        }

        public Image() { }

        public GeoLocation GetLatLong(double latitude, double longitude, double distanceInMetres, double bearing)
        {
            Console.WriteLine(bearing);
            double brngRad = DegreesToRadians(bearing);
            double latRad = DegreesToRadians(latitude);
            double lonRad = DegreesToRadians(longitude);
            int earthRadiusInMetres = 6371000;
            double distFrac = distanceInMetres / earthRadiusInMetres;

            double latitudeResult = RadiansToDegrees(Math.Asin(Math.Sin(latRad) * Math.Cos(distFrac) + Math.Cos(latRad) * Math.Sin(distFrac) * Math.Cos(brngRad)));
            double a = Math.Atan2(Math.Sin(brngRad) * Math.Sin(distFrac) * Math.Cos(latRad), Math.Cos(distFrac) - Math.Sin(latRad) * Math.Sin(latitudeResult));
            double longitudeResult = RadiansToDegrees((lonRad + a + 3 * Math.PI) % (2 * Math.PI) - Math.PI);

            return new GeoLocation(latitudeResult, longitudeResult);
        }

        public GeoLocation GetNewLatLong(double latitude, double longitude, double distanceInMetres, double bearing)
        {
            int earthRadiusInMetres = 6371000;
            double brngRad = DegreesToRadians(bearing);
            double latRad = DegreesToRadians(latitude);
            double lonRad = DegreesToRadians(longitude);
            double distFrac = distanceInMetres / earthRadiusInMetres;

            double nextLatitudeResult = Math.Asin(Math.Sin(latRad) * Math.Cos(distFrac) + Math.Cos(latRad) * Math.Sin(distFrac) * Math.Cos(brngRad));
            double nextLongitudeResult = (Math.Atan2(Math.Sin(brngRad) * Math.Sin(distFrac) * Math.Cos(latitude), Math.Cos(distFrac) - Math.Sin(latitude) * Math.Sin(nextLatitudeResult)));

            return new GeoLocation(RadiansToDegrees(nextLatitudeResult), longitude + RadiansToDegrees(nextLongitudeResult));
        }

        

        public double GetAngleB(int imageWidth, int imageHeight)
        {
            return 180 - 90 - (Math.Atan(Convert.ToDouble(imageHeight) / Convert.ToDouble(imageWidth)) * 180 / Math.PI);
        }

        public float GetGsd(float sensorWidth, float altitude, float focalLength, int imageWidth)
        {
            return (float)(sensorWidth * altitude) / (focalLength * imageWidth);
        }

        public double GetDistanceToCornersInMeters(int imageHeight, int imageWidth, float gsd)
        {
            return Math.Sqrt(Math.Pow(Convert.ToDouble(imageHeight / 2), 2) + Math.Pow(Convert.ToDouble(imageWidth / 2), 2)) * gsd;
        }

        public double GetUpperLeftBearing(double bAngle, float flightYawAngle)
        {
            double angle = 0 - bAngle + flightYawAngle;
            if (angle >= 360)
            {
                return angle - 360;
            } else if (angle < 0)
            {
                return angle + 360;
            }
            else return angle;
        }
        public double GetUpperRightBearing(double bAngle, float flightYawAngle)
        {
            double angle = 0 + bAngle + flightYawAngle;
            if (angle >= 360)
            {
                return angle - 360;
            }
            else if (angle < 0)
            {
                return angle + 360;
            }
            else return angle;
        }

        public double GetLowerLeftBearing(double bAngle, float flightYawAngle)
        {
            double angle = 180 + bAngle + flightYawAngle;
            if (angle >= 360)
            {
                return angle - 360;
            }
            else if (angle < 0)
            {
                return angle + 360;
            }
            else return angle;
        }

        public double GetLowerRightBearing(double bAngle, float flightYawAngle)
        {
            double angle = 180 - bAngle + flightYawAngle;
            if (angle >= 360)
            {
                return angle - 360;
            }
            else if (angle < 0)
            {
                return angle + 360;
            }
            else return angle;
        }

        public double DegreesToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public double RadiansToDegrees(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        public List<GeoLocation> GetCoordinateList(GeoLocation point1, int imageHeight, int imageWidth, float sensorWidth, float altitude, float focal, float flightYawAngle, float gimbalYawAngle)
        {
            double angleB = GetAngleB(imageWidth, imageHeight);
            float gsd = GetGsd(sensorWidth, altitude, focal, imageWidth);
            double distance = GetDistanceToCornersInMeters(imageHeight, imageWidth, gsd);

            double upperLeftBearing = GetUpperLeftBearing(angleB, flightYawAngle);
            double upperRightBearing = GetUpperRightBearing(angleB, flightYawAngle);
            double lowerRightBearing = GetLowerRightBearing(angleB, flightYawAngle);
            double lowerLeftBearing = GetLowerLeftBearing(angleB, flightYawAngle);

            List<GeoLocation> list = new List<GeoLocation>();

            list.Add(GetNewLatLong(point1.Latitude, point1.Longitude, distance, upperLeftBearing));
            list.Add(GetNewLatLong(point1.Latitude, point1.Longitude, distance, upperRightBearing));
            list.Add(GetNewLatLong(point1.Latitude, point1.Longitude, distance, lowerRightBearing));
            list.Add(GetNewLatLong(point1.Latitude, point1.Longitude, distance, lowerLeftBearing));

            return list;
        }
    }
}
