using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class SortForOrthoConfig
    {
        private string _pathToFiles;
        private string _pathToSortedBatches;
        private string _pathToShapeFile;
        private float _sensorWidth;
        private float _overlapPercentage;
        private int _maxSecondsBetweenImages;
        private bool _searchRecursive;
        private string[] _filters;
        private string _nodeOdmUrl;

        public string PathToFiles
        {
            get
            {
                return _pathToFiles;
            }
            set
            {
                _pathToFiles = value;
            }
        }
        public string PathToSortedBatches
        {
            get
            {
                return _pathToSortedBatches;
            }
            set
            {
                _pathToSortedBatches = value;
            }
        }

        public string PathToShapeFile
        {
            get
            {
                return _pathToShapeFile;
            }
            set
            {
                _pathToShapeFile = value;
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

        public float OverlapPercentage
        {
            get
            {
                return _overlapPercentage;
            }
            set
            {
                _overlapPercentage = value;
            }
        }

        public int MaxSecondsBetweenImages
        {
            get
            {
                return _maxSecondsBetweenImages;
            }
            set
            {
                _maxSecondsBetweenImages = value;
            }
        }

        public bool SearchRecursive
        {
            get
            {
                return _searchRecursive;
            }
            set
            {
                _searchRecursive = value;
            }
        }

        public string[] Filters
        {
            get
            {
                return _filters;
            }
            set
            {
                _filters = value;
            }
        }

        public string NodeOdmUrl
        {
            get
            {
                return _nodeOdmUrl;
            }
            set
            {
                _nodeOdmUrl = value;
            }
        }

        public SortForOrthoConfig(string pathToFiles, string pathToSortedBatches, string pathToShapeFile, float sensorWidth, float overlapPercentage, int maxSecondsBetweenImages, bool searchRecursive, string[] filters, string nodeOdmUrl)
        {
            this._pathToFiles = pathToFiles;
            this._pathToSortedBatches = pathToSortedBatches;
            this._pathToShapeFile = pathToShapeFile;
            this._sensorWidth = sensorWidth;
            this._overlapPercentage = overlapPercentage;
            this._maxSecondsBetweenImages = maxSecondsBetweenImages;
            this._searchRecursive = searchRecursive;
            this._filters = filters;
            this._nodeOdmUrl = nodeOdmUrl;
        }

        public SortForOrthoConfig()
        {

        }
    }
}
