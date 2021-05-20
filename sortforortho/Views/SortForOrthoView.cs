using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sortforortho.Views
{
    class SortForOrthoView
    {
        public void ShowResult(string[] filePaths)
        {
            int count = 0;
            foreach (string filePath in filePaths)
            {
                Console.WriteLine(filePath);
                count++;
            }

            Console.WriteLine("Found " + count + " images");
            Console.ReadLine();
        }

        public void ConfigError(Exception e)
        {
            Console.WriteLine("Error occurred, check configuration!");
            Console.WriteLine("Exception: " + e);
            Console.ReadLine();
        }

        public void NoImages()
        {
            Console.WriteLine("No images in the configured root folder, program will exit.");
            Console.ReadLine();
        }

        public void ParsingError(int caseSwitch)
        {
            string configType = "";
            switch (caseSwitch)
            {
                case 1: 
                    configType = "sensorwidth";
                    break;
                case 2:
                    configType = "overlap percentage";
                    break;
                case 3:
                    configType = "search recursive";
                    break;
                case 4:
                    configType = "max seconds between images";
                    break;
                case 5:
                    configType = "image width";
                    break;
                case 6:
                    configType = "image height";
                    break;
                case 7:
                    configType = "focal length";
                    break;
                case 8:
                    configType = "altitude";
                    break;
                case 9:
                    configType = "flight yaw degree";
                    break;
                case 10:
                    configType = "gimbal yaw degree";
                    break;
                case 11:
                    configType = "feature (from list) altitude";
                    break;
                case 12:
                    configType = "feature altitude";
                    break;
            }
            Console.WriteLine("Error parsing " + configType + ".");
            Console.ReadLine();
        }

        public void NewList()
        {
            Console.WriteLine("Starting new list.");
        }

        public void AddedToList()
        {
            Console.WriteLine("Added to list.");
        }

        public void NoImageOverLap()
        {
            Console.WriteLine("None of the remaining images overlaps, finishing list.");
        }

        public void ShapeFileCreated()
        {
            Console.WriteLine("Shapefile successfully created!");
        }

        public void CreateShapeFileFailed(Exception e)
        {
            Console.WriteLine("Error when creating shapefile!");
            Console.WriteLine("Exception: " + e);
        }

        public void ErrorWhileGettingDataFromXML(Exception e)
        {
            Console.WriteLine("Error while getting data from XML, program will not work properly");
            Console.WriteLine("Exception: " + e);
        }

        public void ShowWorkingWithFile(string filePath)
        {
            Console.WriteLine("Working with: " + filePath);
        }

        public void CantGetDriver()
        {
            Console.WriteLine("Can't get driver.");
        }

        public void CantGetDataSource()
        {
            Console.WriteLine("Can't create the datasource.");
        }

        public void ShowNumberOfOrthoPhotos(float overlapPercentage, int numberOfOrthophotos, int numberOfLoners, int ignoredImages)
        {
            Console.WriteLine("Number of ignored images: " + ignoredImages);
            Console.WriteLine("Number of batches with less than 5 images: " + numberOfLoners);
            Console.WriteLine("Number of orthophotos (with a minimum overlap: " + overlapPercentage + "%) to create is: " + (numberOfOrthophotos - numberOfLoners));
        }

        public void LayerExisted()
        {
            Console.WriteLine("Layer already existed. Recreating it.\n");
        }

        public void LayerCreationFailed()
        {
            Console.WriteLine("Layer creation failed.");
        }

        public void FieldCreationFailed()
        {
            Console.WriteLine("Creating fields failed.");
        }

        public void DateFieldFailed()
        {
            Console.WriteLine("Creating Date field failed.");
        }

        public void AltitudeFieldFailed()
        {
            Console.WriteLine("Creating Altitude field failed.");
        }

        public void AddGeometryFailed()
        {
            Console.WriteLine("Failed add geometry to the feature");
        }

        public void FeatureInShapeFileFailed()
        {
            Console.WriteLine("Failed to create feature in shapefile");
        }

        public void ImageMetaDataNotComplete(string filePath)
        {
            Console.WriteLine("Image metadata not complete, ignoring image: " + filePath);
        }

        public bool ShowSortOptions()
        {
            Console.WriteLine("Press <Enter> to place these batches in different directories");
            Console.WriteLine("Press (Esc) key to quit: \n");

            ConsoleKey ck;
            do
            {
                ck = Console.ReadKey(true).Key;
                if (ck == ConsoleKey.Enter)
                {
                    Console.WriteLine("Moving files to directories.");
                    return true;
                }
                if (ck == ConsoleKey.Escape)
                {
                    return false;
                }
            }
            while (ck != ConsoleKey.Enter || ck != ConsoleKey.Escape);
            return true;
        }

        public void CouldNotCreateOrthos(Exception e)
        {
            Console.WriteLine("Could not create ortho photos. Exception: " + e);
        }
    }
}
