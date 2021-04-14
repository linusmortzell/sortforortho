/*
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace CRAMO_Worker
{
    internal class MapFactory
    {
        private MapConfiguration mapConfiguration;
        private Logger logger;
        public MapFactory(MapConfiguration mapConfiguration, NLog.Logger logger)
        {
            this.mapConfiguration = mapConfiguration;
            this.logger = logger;
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();
        }
        internal void Run()
        {//
         // CleanFolders(mapConfiguration);
            using (DataSource searchGridShape = Ogr.Open(Path.Combine(mapConfiguration.Settings.GridPath), 0))
            {
                using (Layer searchGridLayer = searchGridShape.GetLayerByIndex(0))
                {
                    FeatureDefn fn = searchGridLayer.GetLayerDefn();
                    long features = searchGridLayer.GetFeatureCount(0);
                    Dictionary<int, List<Helpers.File>> merge = new Dictionary<int, List<Helpers.File>>();
                    logger.Debug("Getting wkts");
                    DateTime n = DateTime.Now;
                    Dictionary<File, string> wktMapper = GetInterSectWkts(mapConfiguration.FilesToDownload);
                    logger.Debug("Total time for getting wkts: " + (DateTime.Now - n).TotalMilliseconds + " ms");
                    logger.Debug("---");
                    string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                    System.IO.File.Delete(tempFolder);
                    if (!Directory.Exists(tempFolder))
                    {
                        Directory.CreateDirectory(tempFolder);
                    }
                    using (Dataset ds = Gdal.Open(mapConfiguration.FilesToDownload[0].TCI_ImagePath, Access.GA_ReadOnly))
                    {
                        string projection = ds.GetProjectionRef();
                        //Console.WriteLine(projection);
                        SpatialReference srs = new SpatialReference(null);
                        srs.ImportFromWkt(ref projection);
                        Measuredistance(srs);

                        for (int i = 0; i < features; i++)
                        {
                            //logger.Debug("---");
                            //DateTime now = DateTime.Now;
                            Feature f = searchGridLayer.GetFeature(i);
                            Geometry g = f.GetGeometryRef();
                            List<Helpers.File> intersectFiles = new List<Helpers.File>();
                            foreach (var wkt in wktMapper)
                            {
                                string temp = wkt.Value;
                                Geometry t = Ogr.CreateGeometryFromWkt(ref temp, srs);

                                //CoordinateTransformation ctf = Osr.CreateCoordinateTransformation(t.GetSpatialReference(), searchGridLayer.GetFeature(0).GetGeometryRef().GetSpatialReference());
                                //t.Transform(ctf);
                                string wk1;
                                string wk2;
                                t.ExportToWkt(out wk1);
                                g.ExportToWkt(out wk2);
                                // This works and will intersect..
                                if (g.Intersect(t))
                                {
                                    intersectFiles.Add(wkt.Key);
                                }
                            }
                            //logger.Debug("Total time for 1 square File Nummer " + i + " : " + (DateTime.Now - now).TotalMilliseconds + " ms");
                            //logger.Debug("---");
                            // Get the actual id of the feature.                           
                            merge.Add(f.GetFieldAsInteger(f.GetFieldIndex("id")), intersectFiles);
                        }
                    }
                    DateTime ti = DateTime.Now;
                    List<SearchFeature> searchFeatures = GetSearchFeatures(searchGridLayer);

                    Parallel.ForEach(merge, (e) =>
                    {
                        // TODAYS images only
                        if (e.Value.Count > 0)
                        {
                            new GDALVrt().BuildVrt(e, Path.Combine(mapConfiguration.mapPath, "todayCloud"), searchFeatures, mapConfiguration.Settings.CurrentSearchDate);
                        }
                    });
                    Parallel.ForEach(merge, (e) =>
                    {
                        //string earlierFile = Path.Combine(mapConfiguration.GetLatestMapPath, e.Key.ToString() + "_.jp2");
                        string earlierFile = Directory.GetFiles(mapConfiguration.GetLatestMapPath).Where(p => p.StartsWith(e.Key.ToString() + "_") && p.EndsWith(".jp2")).FirstOrDefault();
                        if (earlierFile != null)
                        {
                            // Create a temporare copy of the file and reference that in the vrt. Remove it in the end.
                            //string fileCopyPath = Path.Combine(tempFolder, e.Key.ToString()) + "_.jp2";
                            string fileCopyPath = Path.Combine(tempFolder, earlierFile.Split('\\').Last());
                            System.IO.File.Copy(earlierFile, fileCopyPath);
                            e.Value.Insert(0, new CRAMO_Worker.Helpers.File(false) { TCI_ImagePath = fileCopyPath });
                        }
                        if (e.Value.Count > 0)
                        {
                            new GDALVrt().BuildVrt(e, mapConfiguration.mapPath, searchFeatures, mapConfiguration.Settings.CurrentSearchDate);
                        }
                    });

                    logger.Debug("Total time for vrt Building: " + (DateTime.Now - ti).TotalMilliseconds + " ms");
                    logger.Debug("---");
                    ti = DateTime.Now;
                    Parallel.ForEach(Directory.GetFiles(Path.Combine(mapConfiguration.mapPath, "todayCloud")).Where(p => p.EndsWith(".vrt")).ToList(), (f) =>
                    {
                        new GDALTranslate().Translate(f, searchGridLayer, Path.Combine(mapConfiguration.mapPath, "todayCloud"), searchFeatures, false);
                    });
                    VerifyNoData(Directory.GetFiles(Path.Combine(mapConfiguration.mapPath, "todayCloud")).Where(f => f.EndsWith(".jp2")));
                    Parallel.ForEach(Directory.GetFiles(Path.Combine(mapConfiguration.mapPath, "todayCloud")).Where(f => f.EndsWith(".jp2")), (f) =>
                    {
                        new GDALVrt(f).Run();
                    });
                    Parallel.ForEach(Directory.GetFiles(Path.Combine(mapConfiguration.mapPath, "todayCloud")).Where(p => p.EndsWith("_T.vrt")), (f) =>
                    {
                        new GDALAddo(f).Build();
                    });
                    Parallel.ForEach(Directory.GetFiles(mapConfiguration.mapPath).Where(p => !p.EndsWith("_3006.vrt")).Where(p => p.EndsWith(".vrt")).ToList(), (f) =>
                    {
                        new GDALTranslate().Translate(f, searchGridLayer, mapConfiguration.GetLatestMapPath, searchFeatures, true);
                    });
                    logger.Debug("Total time for translating: " + (DateTime.Now - ti).TotalMilliseconds + " ms");
                    logger.Debug("---");
                    // Get all files for tileindex
                    string[] folders = Directory.GetDirectories(mapConfiguration.mapRootPath).Where(d =>
                    {
                        DateTime temp;
                        if (DateTime.TryParse(d.Split('\\').Last(), out temp))
                        {
                            return temp <= DateTime.Parse(mapConfiguration.Settings.CurrentSearchDate);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    )
                    .ToArray();
                    List<string> paths = new List<string>();
                    foreach (var item in folders)
                    {
                        paths.AddRange(Directory.GetFiles(Path.Combine(item, "todayCloud")).Where(f => f.EndsWith("_T.vrt")).ToList());
                    };
                    // Create tileindex
                    // Create optfile
                    string optFile = Path.Combine(mapConfiguration.mapRootPath, "optfile.txt");
                    if (System.IO.File.Exists(optFile))
                    {
                        System.IO.File.Delete(optFile);
                    }
                    System.IO.File.WriteAllLines(optFile, paths);
                    string indexName = "index.shp";
                    new GDALTIndex().Run(optFile, Path.Combine(mapConfiguration.mapRootPath, indexName));

                    AddColumnsToIndex(indexName);
                    UpdateColumns(indexName);

                    foreach (string item in Directory.GetFiles(tempFolder))
                    {
                        if (item.EndsWith(".jp2"))
                        {
                            System.IO.File.Delete(item);
                        }
                    }

                    //System.IO.File.Delete(tempFolder);
                }

            }

            // Cleanup, remove .vrt and jp2 + tif
            foreach (string item in Directory.GetFiles(mapConfiguration.GetLatestMapPath))
            {
                if (item.EndsWith("xml"))
                {
                    System.IO.File.Delete(item);
                }
            }
            foreach (string item in Directory.GetFiles(Path.Combine(mapConfiguration.mapPath, "todayCloud")))
            {
                if (item.EndsWith("xml"))
                {
                    System.IO.File.Delete(item);
                }
            }
        }
        private void CleanFolders(MapConfiguration mapConfiguration)
        {
            foreach (var item in Directory.GetFiles(mapConfiguration.mapPath).Where(p => p.EndsWith("_P.vrt")))
            {
                System.IO.File.Delete(item);
            }
            foreach (var item in Directory.GetFiles(Path.Combine(mapConfiguration.mapPath, "today")).Where(p => p.EndsWith(".vrt")))
            {
                System.IO.File.Delete(item);
            }
        }
        private List<SearchFeature> GetSearchFeatures(Layer layer)
        {
            List<SearchFeature> result = new List<SearchFeature>();
            long featureCount = layer.GetFeatureCount(0);
            for (int i = 0; i < featureCount; i++)
            {
                using (Envelope env = new Envelope())
                {
                    //string fid = e.Key.ToString();
                    Feature f = layer.GetFeature(i);
                    f.GetGeometryRef().GetEnvelope(env);
                    result.Add(new SearchFeature()
                    {
                        Id = f.GetFieldAsInteger(f.GetFieldIndex("id")),
                        MaxX = env.MaxX,
                        MaxY = env.MaxY,
                        MinX = env.MinX,
                        MinY = env.MinY
                    });
                }
            }
            return result;
        }
        private void VerifyNoData(IEnumerable<string> enumerable)
        {
            DateTime now = DateTime.Now;
            List<string> noDataValues = new List<string>();
            foreach (var filePath in enumerable)
            {
                using (Dataset ds = Gdal.Open(filePath, Access.GA_Update))
                {
                    noDataValues.Add(new NoDataWorker(ds, logger, filePath).Initialize().ToString());
                    ds.FlushCache();
                    string err = Gdal.GetLastErrorMsg();
                    ds.Dispose();
                }
            }
            noDataValues.Reverse();
            noDataValues.Add("End Day - " + mapConfiguration.Settings.CurrentSearchDate);
            noDataValues.Insert(0, "Start Day - " + mapConfiguration.Settings.CurrentSearchDate);
            logger.Debug("Total time for checking no data: " + (DateTime.Now - now).TotalMilliseconds + " ms");
            DateTime file = DateTime.Now;
            System.IO.File.WriteAllLines(@"c:/Temp/downloads/logs/" + mapConfiguration.Settings.MapName + "_" + mapConfiguration.Settings.CurrentSearchDate
                + "_nodata.txt", noDataValues.ToArray());
            logger.Debug("Total time for writing no data file: " + (DateTime.Now - file).TotalMilliseconds + " ms");
            logger.Debug("---");
        }
        private Dictionary<File, string> GetInterSectWkts(List<Helpers.File> filesToDownload)
        {
            Dictionary<File, string> wkts = new Dictionary<File, string>();
            foreach (var item in filesToDownload)
            {
                string newPath = item.TCI_ImagePath.Replace(".jp2", "_3006.vrt");
                if (!System.IO.File.Exists(newPath))
                {
                    Thread.Sleep(10000);
                }
                using (Dataset ds = Gdal.Open(newPath, Access.GA_ReadOnly))
                {
                    double[,] coords = new double[,] {
                                { GDALInfoGetPosition(ds, 0.0, 0.0).First().Key, GDALInfoGetPosition(ds, 0.0, 0.0).First().Value},
                                { GDALInfoGetPosition(ds, 0.0, ds.RasterYSize).First().Key, GDALInfoGetPosition(ds, 0.0, ds.RasterYSize).First().Value},
                                { GDALInfoGetPosition(ds, ds.RasterXSize, 0.0).First().Key, GDALInfoGetPosition(ds, ds.RasterXSize, 0.0).First().Value},
                                { GDALInfoGetPosition(ds, ds.RasterXSize, ds.RasterYSize).First().Key,  GDALInfoGetPosition(ds, ds.RasterXSize, ds.RasterYSize).First().Value}
                                };

                    string projection = ds.GetProjectionRef();
                    SpatialReference srs = new SpatialReference(null);
                    srs.ImportFromWkt(ref projection);
                    var p = "";
                    srs.ExportToPrettyWkt(out p, 0);
                    string srid = srs.GetAttrValue("AUTHORITY", 0) + ":" + srs.GetAttrValue("AUTHORITY", 1);
                    if (string.Compare(srid, mapConfiguration.Settings.SRID, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        // coordinate system does not match
                        logger.Debug("Reprojection has failed");
                    }
                    string w = string.Format("POLYGON (({0} {1}, {2} {3}, {4} {5}, {6} {7}, {0} {1}))",
                        coords[0, 0].ToString().Replace(',', '.'),
                        coords[0, 1].ToString().Replace(',', '.'),
                        coords[2, 0].ToString().Replace(',', '.'),
                        coords[2, 1].ToString().Replace(',', '.'),
                        coords[3, 0].ToString().Replace(',', '.'),
                        coords[3, 1].ToString().Replace(',', '.'),
                        coords[1, 0].ToString().Replace(',', '.'),
                        coords[1, 1].ToString().Replace(',', '.'),
                        coords[0, 0].ToString().Replace(',', '.'),
                        coords[0, 1].ToString().Replace(',', '.'));
                    wkts.Add(item, w);
                }
            }
            return wkts;
        }
        private static Dictionary<double, double> GDALInfoGetPosition(Dataset ds, double x, double y)
        {
            double[] adfGeoTransform = new double[6];
            double dfGeoX, dfGeoY;
            ds.GetGeoTransform(adfGeoTransform);
            dfGeoX = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
            dfGeoY = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;
            var res = new Dictionary<double, double>();
            res.Add(dfGeoX, dfGeoY);
            return res;
        }
        private static string GDALInfoGetPositionString(Dataset ds, double x, double y)
        {
            double[] adfGeoTransform = new double[6];
            double dfGeoX, dfGeoY;
            ds.GetGeoTransform(adfGeoTransform);
            dfGeoX = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
            dfGeoY = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;
            return dfGeoX.ToString() + ", " + dfGeoY.ToString();
        }
        private void AddColumnsToIndex(string indexName)
        {
            using (DataSource shp = Ogr.Open(Path.Combine(mapConfiguration.mapRootPath, indexName), 1))
            {
                using (Layer layer = shp.GetLayerByIndex(0))
                {
                    if (layer.FindFieldIndex("date", 0) == -1)
                    {
                        FieldDefn fdn = new FieldDefn("date", FieldType.OFTString);
                        layer.CreateField(fdn, 0);
                    }
                    if (layer.FindFieldIndex("cloud", 0) == -1)
                    {
                        FieldDefn fdn = new FieldDefn("cloud", FieldType.OFTReal);
                        layer.CreateField(fdn, 0);
                    }
                }
            }
        }
        private void UpdateColumns(string indexName)
        {
            using (DataSource shp = Ogr.Open(Path.Combine(mapConfiguration.mapRootPath, indexName), 1))
            {
                using (Layer layer = shp.GetLayerByIndex(0))
                {
                    int index = layer.FindFieldIndex("date", 0);
                    for (int i = 0; i < layer.GetFeatureCount(0); i++)
                    {
                        Feature f = layer.GetFeature(i);
                        // Gives file name
                        string date = f.GetFieldAsString(0).Split('_')[2];
                        double cloud = double.Parse(f.GetFieldAsString(0).Split('_')[1].Replace('!', '.'), CultureInfo.InvariantCulture);
                        f.SetField("date", date);
                        f.SetField("cloud", cloud);
                        layer.SetFeature(f);
                    }

                }
                shp.SyncToDisk();
            }
        }
        private void Measuredistance(SpatialReference srs)
        {
            foreach (var point in mapConfiguration.Settings.Points)
            {
                string point1 = point.Point1;
                string point2 = point.Point2;
                Geometry fromPoint = Ogr.CreateGeometryFromWkt(ref point1, srs);
                Geometry toPoint = Ogr.CreateGeometryFromWkt(ref point2, srs);
                double distance = fromPoint.Distance(toPoint);
                if (distance != point.Distance)
                {
                    // Something is wrong..
                }
            }
        }
    }
} */