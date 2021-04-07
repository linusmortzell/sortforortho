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
        private GeoLocation _geoLocation;
        private short _altitude;
        private short _sensorWidth;
        private short _focalLength;
        private int _imageWidth;
        private int _imageHeight;
        private string _photoTaken;
        private string[] _polygonCoordinates;
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

        public GeoLocation GeoLocation
        {
            get
            {
                return _geoLocation;
            }
            set
            {
                _geoLocation = value;
            }
        }

        public short Altitude
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

        public short SensorWidth
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


        public short FocalLength
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

        public string[] PolygonCoordinates
        {
            get
            {
                return _polygonCoordinates;
            }

            set
            {
                _polygonCoordinates = value;
            }
        }


        public Image(string path, GeoLocation geoLocation, short altitude, short sensorWidth, short focalLength, int imageWidth, string photoTaken, string[] polygonCoordinates)
        {
            this._path = path;
            this._geoLocation = geoLocation;
            this._altitude = altitude;
            this._sensorWidth = sensorWidth;
            this._focalLength = focalLength;
            this._imageWidth = imageWidth;
            this._imageHeight = ImageHeight;
            this._photoTaken = photoTaken;
            this._polygonCoordinates = polygonCoordinates;
        }

        public List<GeoLocation> GetPolygonCoordinates(GeoLocation latlng, int flightYaw)
        {
            List<GeoLocation> list = new List<GeoLocation>();




            return list; 
        }

    }
}
