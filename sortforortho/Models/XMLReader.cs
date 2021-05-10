using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace sortforortho.Models
{
    class XMLReader
    {
        private Views.SortForOrthoView _view;
        public string ReadValueFromXML(string settingsFilePath, string valueToRead)
        {
            try
            {
                XPathDocument doc = new XPathDocument(settingsFilePath);
                XPathNavigator nav = doc.CreateNavigator();
                
                // Compile a standard XPath expression
                XPathExpression expr;
                expr = nav.Compile(@"/settings/" + valueToRead);
                XPathNodeIterator iterator = nav.Select(expr);
                
                while (iterator.MoveNext())
                {
                    return iterator.Current.Value;
                }
                return string.Empty;
            }
            catch (Exception e)
            {
                _view.ErrorWhileGettingDataFromXML(e); 
                return string.Empty;
            }
        }

        public XMLReader(Views.SortForOrthoView view)
        {
            this._view = view;
        }
    }
}
