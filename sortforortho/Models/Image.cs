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
        private string _latlngCenter;
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

        public string LatlngCenter
        {
            get
            {
                return _latlngCenter;
            }
            set
            {
                _latlngCenter = value;
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

        public Image(string path, string latlngCenter, string[] polygonCoordinates)
        {
            this._path = path;
            this._latlngCenter = latlngCenter;
            this._polygonCoordinates = polygonCoordinates;
        }
    }
}
