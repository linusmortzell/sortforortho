using sortforortho.Models;
using sortforortho.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
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
            bool searchRecursive;
            string[] filters;
            string[] filePaths;

            try
            {
                path = ConfigurationManager.AppSettings.Get("pathToFiles");
                searchRecursive = bool.Parse(ConfigurationManager.AppSettings.Get("searchRecursive"));
                string filtersString = ConfigurationManager.AppSettings.Get("filter");
                filters = filtersString.Split(',');
                filePaths = GetFilePathsFrom(@path, filters, searchRecursive);
                _view.ShowResult(filePaths);
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
                filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound.ToArray();
        }
    }
}
