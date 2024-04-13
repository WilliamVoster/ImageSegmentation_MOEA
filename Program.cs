using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation_MOEA
{
    internal class Program
    {
        //Bitmap image;

        Program() { }


        public static Bitmap loadImage(string filepath)
        {
            Bitmap image = null;
            try
            {
                image = new Bitmap(filepath);
            }
            catch { Console.WriteLine("could not load/find file: " + filepath); }

            return image;
        }

        public static void runEvaluator()
        {
            try
            {
                Console.WriteLine("\nStarting segmentation evaluation...\n");

                ProcessStartInfo startInfo = new ProcessStartInfo();

                startInfo.FileName = "python";
                startInfo.Arguments = "C:\\Repo\\BioAI\\ImageSegmentation_MOEA\\Project_3_evaluator\\run.py";
                startInfo.UseShellExecute = false;

                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    // show script output in c# window
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Console.WriteLine("output:");
                    Console.WriteLine(output);
                    Console.WriteLine("error:");
                    Console.WriteLine(error);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed evaulation script: {e.Message}");
            }

        }

        public static void moveImages(string source, string destination, string filter, bool deleteAllInDestination)
        {

            try
            {
                if (deleteAllInDestination)
                {

                    string[] filesToDelete = Directory.GetFiles(destination, "*.jpg");

                    foreach (string filePath in filesToDelete)
                    {
                        File.Delete(filePath);
                        Console.WriteLine("Deleted: " + filePath);
                    }

                }


                List<string> groundTruths = Directory.GetFiles(source, filter).ToList<string>();
                //groundTruths.Add(source + "Test image.jpg"); 

                foreach (string imagePath in groundTruths)
                {
                    string imageFileName = Path.GetFileName(imagePath);
                    string imageDestinationPath = Path.Combine(destination, imageFileName);

                    File.Copy(imagePath, imageDestinationPath);

                    Console.WriteLine("copy: " + imageFileName);
                }

            }
            catch { Console.WriteLine("could not copy/find files"); }

        }

        public static void saveSolution(Bitmap solution, string destination)
        {
            try
            {
                solution.Save(destination, System.Drawing.Imaging.ImageFormat.Png);

            }
            catch { Console.WriteLine("could not save file"); }

            Console.WriteLine("\nSaved solution: \n" + destination + "\n");

        }


    public static void Main(string[] args)
        {

            Program program = new Program();

            string solutionDir = Directory.GetCurrentDirectory() + "\\..\\..\\..\\";
            String[] images = { "86016", "118035", "147091", "176035", "176039", "353013" };
            string trainImageFolderPath = solutionDir + "Project_3_training_images\\" + images[1];
            string evaluatorPath = solutionDir + "Project_3_evaluator\\";
            string imagePath = trainImageFolderPath + "\\Test image.jpg";
            string solutionsFolder = evaluatorPath + "student_segments";


            //Program.moveImages(trainImageFolderPath, evaluatorPath + "optimal_segments", "GT_*.jpg", true);
            //Program.moveImages(trainImageFolderPath, solutionsFolder, "s*.jpg", true);

            //Program.runEvaluator();


            Bitmap image = Program.loadImage(imagePath);

            Individual individual = new Individual(image, 150);

            individual.initializeSegmentsGrid(10, 10);

            Bitmap segmentedImage = individual.getBitmap();

            Program.saveSolution(segmentedImage, solutionsFolder + "\\grid.png");


        }
    }


}




