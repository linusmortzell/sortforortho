using OSGeo.OGR;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class Sorting
    {
        public void SortByIntersection()
        {
            /* -------------------------------------------------------------------- */
            /*      Register format(s).                                             */
            /* -------------------------------------------------------------------- */
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();


            /* -------------------------------------------------------------------- */
            /*      Get driver                                                      */
            /* -------------------------------------------------------------------- */
            OSGeo.OGR.Driver drv = Ogr.GetDriverByName("ESRI Shapefile");
            if (drv == null)
            {
                Console.WriteLine("Can't get driver.");
                System.Environment.Exit(-1);
            }

            // string shapeFilePath = Path.Combine(Environment.CurrentDirectory, @"Shape\images.shp");
            Ogr.RegisterAll();

            // 0 means read-only, 1 means modifiable  
            DataSource ds = Ogr.Open("D:/images/images.shp", 0);
            Console.WriteLine(ds.GetLayerCount());
            OSGeo.OGR.Layer layer = ds.GetLayerByIndex(0);

            Feature f = layer.GetNextFeature();
            Console.WriteLine(layer.GetFeatureCount(0));
            // Reset feature reading to start on the first feature.
            //layer.ResetReading();


            //var f = layer.GetNextFeature();
            //Geometry geom1 = f.GetGeometryRef();
            //f = layer.GetFeature(1);
            //Geometry geom2 = f.GetGeometryRef();

            //Console.WriteLine(geom1.Intersect(geom2));


            //    // Set spatial reference
            //    SpatialReference wgs84 = new SpatialReference(null);
            //    wgs84.ImportFromEPSG(4326);

            //    SpatialReference sweref99 = new SpatialReference(null);
            //    sweref99.ImportFromEPSG(3006);



            //    return new List<List<String>>();
            //}

            //private List<string> GetFieldList(Layer mLayer)
            //{
            //    List<string> newFieldList = new List<string>();
            //    FeatureDefn oDefn = mLayer.GetLayerDefn();
            //    int FieldCount = oDefn.GetFieldCount();
            //    for (int i = 0; i < FieldCount; i++)
            //    {
            //        FieldDefn oField = oDefn.GetFieldDefn(i);
            //        string fieldName = oField.GetNameRef();
            //        newFieldList.Add(fieldName);
            //    }
            //    return newFieldList;
            //}
        }
    }
}
