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
        private float _altitude;
        private float _sensorWidth;
        private float _focalLength;
        private float _gimbalYawDegree;
        private int _imageWidth;
        private int _imageHeight;
        private string _createDate;
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

        public float GimbalYawDegree
        {
            get
            {
                return _gimbalYawDegree;
            }
            set
            {
                _gimbalYawDegree = value;
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

        public string CreateDate
        {
            get
            {
                return _createDate;
            }
            set
            {
                _createDate = value;
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


        public Image(string path, GeoLocation centerPoint, float altitude, float sensorWidth, float focalLength, int imageWidth, float gimbalYawDegree, string createDate)
        {
            this._path = path;
            this._centerPoint = centerPoint;
            this._altitude = altitude;
            this._sensorWidth = sensorWidth;
            this._focalLength = focalLength;
            this._imageWidth = imageWidth;
            this._gimbalYawDegree = gimbalYawDegree;
            this._imageHeight = ImageHeight;
            this._createDate = createDate;
        }

        public Image() { }
     
    }
}
