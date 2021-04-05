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
            Controllers.SortForOrthoController controller = new Controllers.SortForOrthoController(view);
            controller.StartApp();
        }
    }
}
