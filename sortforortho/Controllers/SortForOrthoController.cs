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
        private MetaDataReader _mdr;

        public SortForOrthoController(SortForOrthoView view, PhotoSorter ps, MetaDataReader mdr)
        {
            this._view = view;
            this._ps = ps;
            this._mdr = mdr;
        }

        public void StartApp()
        {
            string pathToShapeFile = "./ImageShape";
            string[] filePaths;
            List<Image> imageList = new List<Image>();
            SortForOrthoConfig config = new SortForOrthoConfig();

            try
            {
                ConfigReader cfgReader = new ConfigReader();
                config = cfgReader.GetConfigInfo();
            }
            catch (FormatException fe)
            {
                _view.ConfigError(fe);
                System.Environment.Exit(-1);
            }


            filePaths = GetFilePathsFrom(@config.PathToFiles, config.Filters, config.SearchRecursive);
            if (filePaths.Length <= 0)
            {
                _view.NoImages();
                System.Environment.Exit(-1);
            } else
            {
                _view.ShowResult(filePaths);
                int ignoredImages = 0;
                foreach (string filePath in filePaths)
                {
                    try
                    {
                        Image image = _mdr.CreateImage(filePath, config.SensorWidth);
                        imageList.Add(image);
                    }
                    catch
                    {
                        _view.ImageMetaDataNotComplete(filePath);
                        ignoredImages++;
                    }
                }
                _view.ImageListCreated(ignoredImages);
            }

            List<List<String>> photosInBatches = _ps.SortForOrtho(imageList, config.OverlapPercentage, config.MaxSecondsBetweenImages, pathToShapeFile); ;

            if (_view.ShowSortOptions())
            {
                _ps.PutFilesInDirectories(config.PathToSortedBatches, photosInBatches);
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
    }
}
