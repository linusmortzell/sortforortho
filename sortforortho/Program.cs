using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho
{
    class Program
    {
        static void Main(string[] args)
        {
            Views.SortForOrthoView view = new Views.SortForOrthoView();
            Models.PhotoSorter ps = new Models.PhotoSorter(view);
            Models.MetaDataReader mdr = new Models.MetaDataReader();
            Controllers.SortForOrthoController controller = new Controllers.SortForOrthoController(view, ps, mdr);
            controller.StartApp();
        }
    }
}
