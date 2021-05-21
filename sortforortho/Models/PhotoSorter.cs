using MetadataExtractor;
using OSGeo.OGR;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        public List<List<String>> SortForOrtho(List<Image> imageList, float overlapPercentage, int maxSecondsBetweenImages, string pathToShapeFile, int ignoredImages)
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
                throw new Exception("Error getting driver");
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
                throw new Exception("Error getting datasource");
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
                    ds.DeleteLayer(i);
                    break;
                }
            }

            layer = ds.CreateLayer(layerName, sweref99, wkbGeometryType.wkbPolygon, new string[] { });
            if (layer == null)
            {
                throw new Exception("Layer creation failed");
            }


            /* -------------------------------------------------------------------- */
            /*      Adding attribute fields                                         */
            /* -------------------------------------------------------------------- */

            FieldDefn fdefn = new FieldDefn("Name", FieldType.OFTString);

            fdefn.SetWidth(32);

            if (layer.CreateField(fdefn, 1) != 0)
            {
                throw new Exception("Namefield creation failed");
            }

            fdefn = new FieldDefn("CreateDate", FieldType.OFTString);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                throw new Exception("Datefield creation failed");
            }


            /* -------------------------------------------------------------------- */
            /*      Adding features                                                 */
            /* -------------------------------------------------------------------- */

            foreach (Image img in imageList)
            {
                Feature feature = new Feature(layer.GetLayerDefn());
                feature.SetField("Name", img.Path);
                feature.SetField("CreateDate", img.CreateDate);

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
                    throw new Exception("Add geometry to feature failed");
                }

                if (layer.CreateFeature(feature) != 0)
                {
                    throw new Exception("Adding feature to shapefile failed");
                }
            }

            List<List<Feature>> sortedList = SortImages(layer, overlapPercentage, maxSecondsBetweenImages);

            CreateShapeFileOfSortedFiles(sortedList, pathToShapeFile);

            List<List<String>> batchStringList = GetListOfFilePathBatches(sortedList);

            return batchStringList;
        }

        private List<List<Feature>> SortImages(Layer layer, float overlapPercentage, int maxSecondsBetweenImages)
        {

            List<List<Feature>> sorted = new List<List<Feature>>();

            // Create a list of all features
            List<Feature> featureList = new List<Feature>();

            // Make sure to read from the first feature
            layer.ResetReading();

            // Place all features in the list of features.
            for (int i = 0; i < layer.GetFeatureCount(0); i++)
            {
                featureList.Add(layer.GetNextFeature());
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
                    _view.ShowWorkingWithFile(feat1.GetFieldAsString(0));

                    DateTime feat1CreateDate = ConvertStringToDateTime(feat1.GetFieldAsString(1));
                    bool timeMatch = CheckIfTimeMatch(batchImagesCreateDates, feat1CreateDate, maxSecondsBetweenImages);

                    // If union is null a new batch is created.
                    if (union == null)
                    {
                        tempList.Add(feat1);
                        batchImagesCreateDates.Add(feat1CreateDate);
                        featureList.Remove(feat1);
                        union = geom1;
                        i = 0;
                    }

                    // Check for intersect and timematch.
                    else if (geom1.Intersect(union) && timeMatch)
                    {
                        Geometry intersect = geom1.Intersection(union);
                        double intersectedAreaInPercentate = (intersect.GetArea() / geom1.GetArea()) * 100;
                        if (intersectedAreaInPercentate >= overlapPercentage)
                        {
                            tempList.Add(feat1);
                            batchImagesCreateDates.Add(feat1CreateDate);
                            featureList.Remove(feat1);
                            union = union.Union(geom1);
                            i = 0;
                        }
                    }
                }

                // If no more geometrys does overlap the union or match in time 
                // the batch are being saved in the list of sorted batches.
                sorted.Add(tempList);
                tempList = new List<Feature>();
                batchImagesCreateDates = new List<DateTime>();
                union = null;
                i = 0;
            }
            return sorted;
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
            // searches the current directory
            int fCount;
            if (System.IO.Directory.Exists(pathToSortedBatches))
            {
                fCount = System.IO.Directory.GetDirectories(pathToSortedBatches).Length;
            }
            else
            {
                System.IO.Directory.CreateDirectory(pathToSortedBatches);
                fCount = 0;
            }
                
            for (int i = 0; i < sortedList.Count; i++)
            {
                string directory = pathToSortedBatches + "/Batch" + (fCount + i + 1);
                System.IO.Directory.CreateDirectory(directory);
                foreach (string path in sortedList[i])
                {
                    // Get filename
                    string[] pathSplit = path.Split('\\');
                    string fileName = pathSplit[pathSplit.Length - 1];

                    // Move file
                    System.IO.File.Copy(@path, directory + "/" + fileName, true);
                }
            }
        }

        public void CreateShapeFileOfSortedFiles(List<List<Feature>> sortedImages, string pathToShapeFile)
        {
            /* -------------------------------------------------------------------- */
            /*      Get driver                                                      */
            /* -------------------------------------------------------------------- */
            OSGeo.OGR.Driver drv = Ogr.GetDriverByName("ESRI Shapefile");
            if (drv == null)
            {
                throw new Exception("Error getting driver");
            }

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
                throw new Exception("Error when creating datasource");
            }


            /* -------------------------------------------------------------------- */
            /*      Creating the layer                                              */
            /* -------------------------------------------------------------------- */

            string layerName = "images";
            Layer layer;

            for (int i = 0; i < ds.GetLayerCount(); i++)
            {
                layer = ds.GetLayerByIndex(i);
                if (layer != null && layer.GetLayerDefn().GetName() == layerName)
                {
                    // _view.LayerExisted();
                    ds.DeleteLayer(i);
                    break;
                }
            }

            layer = ds.CreateLayer(layerName, sweref99, wkbGeometryType.wkbPolygon, new string[] { });
            if (layer == null)
            {
                throw new Exception("Layercreation failed");
            }

            /* -------------------------------------------------------------------- */
            /*      Adding attribute fields                                         */
            /* -------------------------------------------------------------------- */

            FieldDefn fdefn = new FieldDefn("Batch", FieldType.OFTString);

            fdefn.SetWidth(32);

            if (layer.CreateField(fdefn, 1) != 0)
            {
                throw new Exception("Batchfield creation failed");
            }
            
            fdefn = new FieldDefn("Name", FieldType.OFTString);

            if (layer.CreateField(fdefn, 1) != 0)
            {
                throw new Exception("Namefield creation failed");
            }

            fdefn = new FieldDefn("DateTime", FieldType.OFTString);
            if (layer.CreateField(fdefn, 1) != 0)
            {
                throw new Exception("DateTimefield creation failed");
            }

            /* -------------------------------------------------------------------- */
            /*      Adding features                                                 */
            /* -------------------------------------------------------------------- */

            for (int i = 0; i < sortedImages.Count; i++)
            {
                foreach (Feature f in sortedImages[i])
                {
                    Feature feature = new Feature(layer.GetLayerDefn());
                    feature.SetField("Batch", "Batch" + (i + 1));
                    feature.SetField("Name", f.GetFieldAsString(0));
                    feature.SetField("DateTime", f.GetFieldAsString(1));

                    Geometry geom = f.GetGeometryRef();

                    if (feature.SetGeometry(geom) != 0)
                    {
                        throw new Exception("Add geometry failed");
                    }
                    if (layer.CreateFeature(feature) != 0)
                    {
                        throw new Exception("Adding feature to shapefile failed");
                    }
                }
            }
        }
    }
}
