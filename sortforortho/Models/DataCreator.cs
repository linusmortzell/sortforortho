using MetadataExtractor;
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
        public void CreateShapeFile(List<Image> imageList, float overlapPercentage)
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
            SpatialReference wgs84 = new SpatialReference(null);
            wgs84.ImportFromEPSG(4326);

            SpatialReference sweref99 = new SpatialReference(null);
            sweref99.ImportFromEPSG(3006);


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

            layer = ds.CreateLayer(layerName, sweref99, wkbGeometryType.wkbPolygon, new string[] { });
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

            fdefn = new FieldDefn("Date", FieldType.OFTString);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                Console.WriteLine("Creating Date field failed.");
                System.Environment.Exit(-1);
            }


            fdefn = new FieldDefn("Altitude", FieldType.OFTString);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                Console.WriteLine("Creating Altitude field failed.");
                System.Environment.Exit(-1);
            }


            /* -------------------------------------------------------------------- */
            /*      Adding features                                                 */
            /* -------------------------------------------------------------------- */

            foreach (Image img in imageList)
            {
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetField("Name", img.Path);
                feature.SetField("Date", img.PhotoTaken);
                feature.SetField("Altitude", img.Altitude);

                CoordinateTransformation transform = Osr.CreateCoordinateTransformation(wgs84, sweref99);
                double[] transformedPoints = { img.CenterPoint.Longitude, img.CenterPoint.Latitude };
                transform.TransformPoint(transformedPoints);
                GeoLocation centerPointSweref = new GeoLocation(transformedPoints[1], transformedPoints[0]);

                img.CornerCoordinates = GetCornerCoordinatesSweref(centerPointSweref, img.ImageHeight, img.ImageWidth, GetGsd(img.SensorWidth, img.Altitude, img.FocalLength, img.ImageWidth));

                List<GeoLocation> rotated = new List<GeoLocation>();

                foreach (GeoLocation point in img.CornerCoordinates)
                {
                    GeoLocation geo = Rotate(centerPointSweref, point, img.GimbalYawDegree);
                    rotated.Add(geo);
                }

                string point1lat = rotated[0].Latitude.ToString().Replace(",", ".");
                string point1lon = rotated[0].Longitude.ToString().Replace(",", ".");
                string point2lat = rotated[1].Latitude.ToString().Replace(",", ".");
                string point2lon = rotated[1].Longitude.ToString().Replace(",", ".");
                string point3lat = rotated[2].Latitude.ToString().Replace(",", ".");
                string point3lon = rotated[2].Longitude.ToString().Replace(",", ".");
                string point4lat = rotated[3].Latitude.ToString().Replace(",", ".");
                string point4lon = rotated[3].Longitude.ToString().Replace(",", ".");

                string wkt = "POLYGON(( " + point1lon + " " + point1lat + ", " + point2lon + " " + point2lat + ", " + point3lon + " " + point3lat + ", " + point4lon + " " + point4lat + ", " + point1lon + " " + point1lat + " ))";
                Geometry geom = Ogr.CreateGeometryFromWkt(ref wkt, sweref99);


                // geom.TransformTo(wgs84);

                // SpatialReference rotation = new SpatialReference("");
                // rotation.ImportFromProj4("-s_srs EPSG:3006 -t_srs EPSG:3006 ")

                // Console.WriteLine(arr);

                // geom.TransformTo(sweref99);


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
            }

            List<List<Feature>> list = SortImages(layer, overlapPercentage);
            Console.WriteLine("Antal ortofoton (med minimum överlapp " + overlapPercentage + "%) att skapa är: " + list.Count());
            ReportLayer(layer);
        }

        public List<List<Feature>> SortImages (Layer layer, float overlapPercentage)
        {

            List<List<Feature>> sorted = new List<List<Feature>>();
            
            // Create a list of all features
            List<Feature> featureList = new List<Feature>();

            // Make sure to read from the first feature
            layer.ResetReading();

            for (int j = 0; j < layer.GetFeatureCount(0); j++)
            {
                featureList.Add(layer.GetNextFeature());
            }

            foreach (Feature f in featureList)
            {
                Console.WriteLine(f.GetFieldAsString(0));
            }

            List<Feature> tempList = new List<Feature>();
            Geometry union = null;

            while (featureList.Count() > 0)
            {
                int i = 0;
                while (i < featureList.Count())
                {
                    Feature feat1 = featureList[i];
                    Geometry geom1 = feat1.GetGeometryRef();
                    i++;
                    Console.WriteLine("Bearbetar: " + feat1.GetFieldAsString(0));

                    if (union == null)
                    {
                        tempList.Add(feat1);
                        featureList.Remove(feat1);
                        union = geom1;
                        i = 0;
                        Console.WriteLine("Börjar på ny lista: " + feat1.GetFieldAsString(0));
                    }                        
                    else if (geom1.Intersect(union))
                    {
                        Geometry intersect = geom1.Intersection(union);
                        double intersectedAreaInPercentate = (intersect.GetArea() / geom1.GetArea()) * 100;
                        if (intersectedAreaInPercentate >= overlapPercentage)
                        {
                            tempList.Add(feat1);
                            featureList.Remove(feat1);
                            union = union.Union(geom1);
                            i = 0;
                            Console.WriteLine("La till i ny lista: " + feat1.GetFieldAsString(0));
                        }
                    } 
                    
                    if (i == featureList.Count())
                    {
                        sorted.Add(tempList);
                        tempList = new List<Feature>();
                        union = null;
                        i = 0;
                        Console.WriteLine("Ingen av kvarvarande överlappade, avslutar lista.");
                    }
                }
            }


            //for (int j = 0; j < featureList.Count - 1; j++)
            //{
            //    Geometry geom1;
            //    if (aggr == null)
            //    {
            //        feature1 = layer.GetNextFeature();
            //        geom1 = feature1.GetGeometryRef();
            //    }
            //    else geom1 = aggr;

            //    feature2 = layer.GetNextFeature();
            //    Geometry geom2 = feature2.GetGeometryRef();


            //    if (geom1.Intersect(geom2))
            //    {
            //        Geometry intersect = geom1.Intersection(geom2);
            //        double intersectedArea = (intersect.GetArea() / geom2.GetArea()) * 100;
            //        if (intersectedArea >= percentageOverlap)
            //        {
            //            Console.WriteLine("geom1Area: " + geom1.GetArea());
            //            aggr = geom1.Union(geom2);
            //            Console.WriteLine("aggrArea: " + aggr.GetArea());
            //            if (list.Count() < 2)
            //            {
            //                list.Add(feature1.GetFieldAsString(0));
            //            }
            //            list.Add(feature2.GetFieldAsString(0));
            //        }
            //    }

            //    foreach (String s in list)
            //    {
            //        Console.WriteLine(s);
            //    }
            //}

            return sorted;
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
        public float GetGsd(float sensorWidth, float altitude, float focalLength, int imageWidth)
        {
            return (float)(sensorWidth * altitude) / (focalLength * imageWidth);
        }
        public List<GeoLocation> GetCornerCoordinatesSweref(GeoLocation centerPointInSweref, int imageHeight, int imageWidth, float gsd)
        {
            List<GeoLocation> cornerCoordinates = new List<GeoLocation>();
            GeoLocation upperLeftCorner = new GeoLocation(centerPointInSweref.Latitude + (imageHeight / 2) * gsd, centerPointInSweref.Longitude - (imageWidth / 2) * gsd);
            GeoLocation upperRightCorner = new GeoLocation(centerPointInSweref.Latitude + (imageHeight / 2) * gsd, centerPointInSweref.Longitude + (imageWidth / 2) * gsd);
            GeoLocation loweLeftCorner = new GeoLocation(centerPointInSweref.Latitude - (imageHeight / 2) * gsd, centerPointInSweref.Longitude + (imageWidth / 2) * gsd);
            GeoLocation lowerRightCorner = new GeoLocation(centerPointInSweref.Latitude - (imageHeight / 2) * gsd, centerPointInSweref.Longitude - (imageWidth / 2) * gsd);

            cornerCoordinates.Add(upperLeftCorner);
            cornerCoordinates.Add(upperRightCorner);
            cornerCoordinates.Add(loweLeftCorner);
            cornerCoordinates.Add(lowerRightCorner);
            return cornerCoordinates;
        }

        private GeoLocation Rotate(GeoLocation center, GeoLocation cornerPoint, double angle)
        {
            double angleInRadians = DegreesToRadians(angle);
            double dLongitude = center.Longitude + Math.Cos(angleInRadians) * (cornerPoint.Longitude - center.Longitude) - Math.Sin(angleInRadians) * (cornerPoint.Latitude - center.Latitude);
            double dLatitude = center.Latitude + Math.Sin(angleInRadians) * (cornerPoint.Longitude - center.Longitude) + Math.Cos(angleInRadians) * (cornerPoint.Latitude - center.Latitude);
            return new GeoLocation(dLatitude, dLongitude);
        }

        public double DegreesToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public double RadiansToDegrees(double radians)
        {
            return (180 / Math.PI) * radians;
        }
    }
}
