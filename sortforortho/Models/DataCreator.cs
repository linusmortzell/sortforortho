﻿using MetadataExtractor;
using OSGeo.OGR;
using OSGeo.OSR;
using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class DataCreator
    {
        public void CreateShapeFile(List<Image> imageList)
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


            // Set spatial reference
            //var spatialReference = new SpatialReference(null);
            //spatialReference.SetWellKnownGeogCS("wgs84");

            SpatialReference srs = new SpatialReference(Osr.SRS_WKT_WGS84);


            /* -------------------------------------------------------------------- */
            /*      Creating the datasource                                         */
            /* -------------------------------------------------------------------- */
            DataSource ds = drv.CreateDataSource("D:/images", new string[] { });
            if (drv == null)
            {
                Console.WriteLine("Can't create the datasource.");
                System.Environment.Exit(-1);
            }

            /* -------------------------------------------------------------------- */
            /*      Creating the layer                                              */
            /* -------------------------------------------------------------------- */

            string layerName = "images";
            Layer layer;

            int i;
            for (i = 0; i < ds.GetLayerCount(); i++)
            {
                layer = ds.GetLayerByIndex(i);
                if (layer != null && layer.GetLayerDefn().GetName() == layerName)
                {
                    Console.WriteLine("Layer already existed. Recreating it.\n");
                    ds.DeleteLayer(i);
                    break;
                }
            }

            layer = ds.CreateLayer(layerName, null, wkbGeometryType.wkbPoint, new string[] { });
            if (layer == null)
            {
                Console.WriteLine("Layer creation failed.");
                System.Environment.Exit(-1);
            }

            /* -------------------------------------------------------------------- */
            /*      Adding attribute fields                                         */
            /* -------------------------------------------------------------------- */

            FieldDefn fdefn = new FieldDefn("Name", FieldType.OFTString);

            fdefn.SetWidth(32);

            if (layer.CreateField(fdefn, 1) != 0)
            {
                Console.WriteLine("Creating fields failed.");
                System.Environment.Exit(-1);
            }

            fdefn = new FieldDefn("Latitude", FieldType.OFTInteger);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                Console.WriteLine("Creating Latitude field failed.");
                System.Environment.Exit(-1);
            }

            fdefn = new FieldDefn("Longitude", FieldType.OFTReal);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                Console.WriteLine("Creating Longitude field failed.");
                System.Environment.Exit(-1);
            }


            /* -------------------------------------------------------------------- */
            /*      Adding features                                                 */
            /* -------------------------------------------------------------------- */

            string latitude = imageList[9].CornerCoordinates[0].Latitude.ToString().Replace(",", ".");
            string longitude = imageList[9].CornerCoordinates[0].Longitude.ToString().Replace(",", ".");
            Console.WriteLine(latitude);
            Feature feature = new Feature(layer.GetLayerDefn());

            feature.SetField("Name", imageList[9].Path);
            feature.SetField("Latitude", latitude);
            feature.SetField("Longitude", longitude);
            Console.ReadLine();

            Geometry geom = Geometry.CreateFromWkt("POINT( " + latitude + " " + longitude + " )");

            if (feature.SetGeometry(geom) != 0)
            {
                Console.WriteLine("Failed add geometry to the feature");
                System.Environment.Exit(-1);
            }

            if (layer.CreateFeature(feature) != 0)
            {
                Console.WriteLine("Failed to create feature in shapefile");
                System.Environment.Exit(-1);
            }

            //string temp = "POLYGON (10 54, 30 50, 44 65)";
            //Geometry geom = Ogr.CreateGeometryFromWkt(ref wkbGeometryType.wkbPolygon, srs);

            //if (feature.SetGeometry(geom) != 0)
            //{
            //    Console.WriteLine("Failed add geometry to the feature");
            //    System.Environment.Exit(-1);
            //}

            //foreach (Image image in imageList)
            //{   
            //    Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
            //    foreach (GeoLocation geo in image.CornerCoordinates)
            //    {
            //        ring.AddPoint(Convert.ToDouble(geo.Latitude.ToString().Replace(",", ".")), Convert.ToDouble(geo.Longitude.ToString().Replace(",", ".")), 0);
            //    }
            //    geom.AddGeometry(ring);
            //}

            //if (layer.CreateFeature(feature) != 0)
            //{
            //    Console.WriteLine("Failed to create feature in shapefile");
            //    System.Environment.Exit(-1);
            //}

            ReportLayer(layer);
        }

        public static void ReportLayer(Layer layer)
        {
            FeatureDefn def = layer.GetLayerDefn();
            Console.WriteLine("Layer name: " + def.GetName());
            Console.WriteLine("Feature Count: " + layer.GetFeatureCount(1));
            Envelope ext = new Envelope();
            layer.GetExtent(ext, 1);
            Console.WriteLine("Extent: " + ext.MinX + "," + ext.MaxX + "," +
                ext.MinY + "," + ext.MaxY);

            /* -------------------------------------------------------------------- */
            /*      Reading the spatial reference                                   */
            /* -------------------------------------------------------------------- */
            OSGeo.OSR.SpatialReference sr = layer.GetSpatialRef();
            string srs_wkt;
            if (sr != null)
            {
                sr.ExportToPrettyWkt(out srs_wkt, 1);
            }
            else
                srs_wkt = "(unknown)";


            Console.WriteLine("Layer SRS WKT: " + srs_wkt);

            /* -------------------------------------------------------------------- */
            /*      Reading the fields                                              */
            /* -------------------------------------------------------------------- */
            Console.WriteLine("Field definition:");
            for (int iAttr = 0; iAttr < def.GetFieldCount(); iAttr++)
            {
                FieldDefn fdef = def.GetFieldDefn(iAttr);

                Console.WriteLine(fdef.GetNameRef() + ": " +
                    fdef.GetFieldTypeName(fdef.GetFieldType()) + " (" +
                    fdef.GetWidth() + "." +
                    fdef.GetPrecision() + ")");
            }

            /* -------------------------------------------------------------------- */
            /*      Reading the shapes                                              */
            /* -------------------------------------------------------------------- */
            Console.WriteLine("");
            Feature feat;
            while ((feat = layer.GetNextFeature()) != null)
            {
                ReportFeature(feat, def);
                feat.Dispose();
            }
        }

        public static void ReportFeature(Feature feat, FeatureDefn def)
        {
            Console.WriteLine("Feature(" + def.GetName() + "): " + feat.GetFID());
            for (int iField = 0; iField < feat.GetFieldCount(); iField++)
            {
                FieldDefn fdef = def.GetFieldDefn(iField);

                Console.Write(fdef.GetNameRef() + " (" +
                    fdef.GetFieldTypeName(fdef.GetFieldType()) + ") = ");

                if (feat.IsFieldSet(iField))
                    Console.WriteLine(feat.GetFieldAsString(iField));
                else
                    Console.WriteLine("(null)");

            }

            if (feat.GetStyleString() != null)
                Console.WriteLine("  Style = " + feat.GetStyleString());

            Geometry geom = feat.GetGeometryRef();
            if (geom != null)
                Console.WriteLine("  " + geom.GetGeometryName() +
                    "(" + geom.GetGeometryType() + ")");

            Envelope env = new Envelope();
            geom.GetEnvelope(env);
            Console.WriteLine("   ENVELOPE: " + env.MinX + "," + env.MaxX + "," +
                env.MinY + "," + env.MaxY);

            string geom_wkt;
            geom.ExportToWkt(out geom_wkt);
            Console.WriteLine("  " + geom_wkt);

            Console.WriteLine("");
        }
    }
}
