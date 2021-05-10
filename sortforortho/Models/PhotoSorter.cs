using MetadataExtractor;
using OSGeo.OGR;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Models
{
    class PhotoSorter
    {
        private Views.SortForOrthoView _view;

        public PhotoSorter (Views.SortForOrthoView view)
        {
            this._view = view;
        }
        public List<List<String>> SortForOrtho(List<Image> imageList, float overlapPercentage, int maxSecondsBetweenImages, string pathToShapeFile)
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
                _view.CantGetDriver();
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
            DataSource ds = drv.CreateDataSource(pathToShapeFile, new string[] { });
            if (drv == null)
            {
                _view.CantGetDataSource();
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
                    _view.LayerExisted();
                    ds.DeleteLayer(i);
                    break;
                }
            }

            layer = ds.CreateLayer(layerName, sweref99, wkbGeometryType.wkbPolygon, new string[] { });
            if (layer == null)
            {
                _view.LayerCreationFailed();
                System.Environment.Exit(-1);
            }


            /* -------------------------------------------------------------------- */
            /*      Adding attribute fields                                         */
            /* -------------------------------------------------------------------- */

            FieldDefn fdefn = new FieldDefn("Name", FieldType.OFTString);

            fdefn.SetWidth(32);

            if (layer.CreateField(fdefn, 1) != 0)
            {
                _view.FieldCreationFailed();
                System.Environment.Exit(-1);
            }

            fdefn = new FieldDefn("DateTime", FieldType.OFTString);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                _view.DateFieldFailed();
                System.Environment.Exit(-1);
            }


            fdefn = new FieldDefn("Altitude", FieldType.OFTString);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                _view.AltitudeFieldFailed();
                System.Environment.Exit(-1);
            }


            /* -------------------------------------------------------------------- */
            /*      Adding features                                                 */
            /* -------------------------------------------------------------------- */

            foreach (Image img in imageList)
            {
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetField("Name", img.Path);
                feature.SetField("DateTime", img.CreateDate);
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


                if (feature.SetGeometry(geom) != 0)
                {
                    _view.AddGeometryFailed();
                    System.Environment.Exit(-1);
                }

                if (layer.CreateFeature(feature) != 0)
                {
                    _view.FeatureInShapeFileFailed();
                    System.Environment.Exit(-1);
                }
            }

            List<List<Feature>> sortedList = SortImages(layer, overlapPercentage, maxSecondsBetweenImages);

            ReportLayer(layer);

            List<List<String>> batchStringList = GetListOfFilePathBatches(sortedList);
            
            int numberOfLoners = 0;
            foreach (List<String> batch in batchStringList)
            {
                if (batch.Count <= 4)
                {
                    numberOfLoners++;
                }
            }
            _view.ShowNumberOfOrthoPhotos(overlapPercentage, sortedList.Count(), numberOfLoners);

            return batchStringList;
        }

        private List<List<Feature>> SortImages(Layer layer, float overlapPercentage, int maxSecondsBetweenImages)
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
            List<DateTime> batchImagesCreateDates = new List<DateTime>();
            Geometry union = null;

            while (featureList.Count() > 0)
            {
                int i = 0;
                while (i < featureList.Count())
                {
                    Feature feat1 = featureList[i];
                    Geometry geom1 = feat1.GetGeometryRef();
                    i++;
                    Console.WriteLine("Working with: " + feat1.GetFieldAsString(0));

                    float tempListAltitude = 0.0f;
                    float feat1Altitude = 0.0f;

                    if (tempList.Count() > 0)
                    {
                        if (!float.TryParse(tempList[0].GetFieldAsString(2), NumberStyles.Any, CultureInfo.InvariantCulture, out tempListAltitude))
                        {
                            Console.WriteLine("Error getting feature altitude");
                        }

                        if (!float.TryParse(feat1.GetFieldAsString(2), NumberStyles.Any, CultureInfo.InvariantCulture, out feat1Altitude))
                        {
                            Console.WriteLine("Error getting feature altitude");
                        }
                    }

                    DateTime feat1CreateDate = ConvertStringToDateTime(feat1.GetFieldAsString(1));
                    bool timeMatch = CheckIfTimeMatch(batchImagesCreateDates, feat1CreateDate, maxSecondsBetweenImages);

                    if (union == null)
                    {
                        tempList.Add(feat1);
                        batchImagesCreateDates.Add(feat1CreateDate);
                        featureList.Remove(feat1);
                        union = geom1;
                        i = 0;
                        Console.WriteLine("Starting new list.");
                    }

                    // Check for intersect and timematch.
                    else if (geom1.Intersect(union) && timeMatch)
                    {
                        Geometry intersect = geom1.Intersection(union);
                        double intersectedAreaInPercentate = (intersect.GetArea() / geom1.GetArea()) * 100;
                        if (intersectedAreaInPercentate >= overlapPercentage)
                        {
                            tempList.Add(feat1);
                            Console.WriteLine(feat1CreateDate);
                            batchImagesCreateDates.Add(feat1CreateDate);
                            featureList.Remove(feat1);
                            union = union.Union(geom1);
                            i = 0;
                            Console.WriteLine("Added to list.");
                        }
                    }

                    if (i == featureList.Count())
                    {
                        sorted.Add(tempList);
                        tempList = new List<Feature>();
                        batchImagesCreateDates = new List<DateTime>();
                        union = null;
                        i = 0;
                        Console.WriteLine("None of the remaining images overlaps, finishing list.");
                    }
                }
            }

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

        private double DegreesToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        private double RadiansToDegrees(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        private DateTime ConvertStringToDateTime(string dateString)
        {

            try
            {
                DateTime photoTaken = DateTime.ParseExact(dateString, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                return photoTaken;
            }
            catch
            {
                Console.WriteLine("Error parsing create date, can't sort image by date / time");
                return new DateTime();
            }
        }

        private bool CheckIfTimeMatch(List<DateTime> batchImageCreateDates, DateTime feat1CreateDate, int maxSecondsBetweenImages)
        {
            foreach (DateTime createDate in batchImageCreateDates)
            {
                if (feat1CreateDate > (createDate.AddSeconds(Convert.ToDouble(maxSecondsBetweenImages))) || feat1CreateDate < (createDate.AddSeconds(-Convert.ToDouble(maxSecondsBetweenImages))))
                {
                    return false;
                }
            }
            return true;
        }

        private List<List<String>> GetListOfFilePathBatches(List<List<Feature>> sortedList)
        {
            List<List<String>> filepathsInBatches = new List<List<String>>();
            foreach (List<Feature> lf in sortedList)
            {
                List<String> strList = new List<String>();
                foreach (Feature f in lf)
                {
                    strList.Add(f.GetFieldAsString(0));
                }
                filepathsInBatches.Add(strList);
            }
            return filepathsInBatches;
        } 

        public void PutFilesInDirectories(string pathToSortedBatches, List<List<String>> sortedList)
        {
            for (int i = 0; i < sortedList.Count; i++)
            {
                string directory = pathToSortedBatches + "/Batch" + (i + 1);
                System.IO.Directory.CreateDirectory(directory);
                foreach (string path in sortedList[i])
                {
                    // Get filename
                    string[] pathSplit = path.Split('\\');
                    string fileName = pathSplit[pathSplit.Length - 1];

                    // Move file
                    System.IO.File.Move(@path, directory + "/" + fileName);
                }
            }
        }
    }
}
