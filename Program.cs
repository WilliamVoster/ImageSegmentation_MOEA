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
        public static int[,] NEIGHBOR_ORDER = {
            { 1, 0 },   // East
            { -1, 0 },  // West
            { 0, -1 },  // North
            { 0, 1 },   // South
            { 1, -1 },  // Northeast
            { 1, 1 },   // Southeast
            { -1, -1 }, // Northwest
            { -1, 1 }   // Southwest
        };

        Bitmap image;
        int popSize;
        int numGenerations;
        Individual[] population;

        Program(int popSize, int numGenerations) 
        {
            this.popSize = popSize;
            this.numGenerations = numGenerations;
            population = new Individual[popSize];
        }

        public void run()
        {
            // Init population
            for (int i = 0; i < popSize; i++)
            {
                population[i] = new Individual(image, 150);
                population[i].initializeSegmentsGrid(10, 10);
                population[i].calcFitness();
            }


            for (int i = 0; i < numGenerations; i++)
            {

                //Selection
                Array.Sort(population);
                Individual[] parents = selection(popSize);

                //Crossover

                //Mutation

                //Offspring Selection

            }

        }

        public Individual[] selection(int numParents)
        {
            Individual[] parents = new Individual[numParents];

            for (int i = 0; i < numParents; i++)
            {
                parents[i] = population[i];
            }

            return parents;
        }

        //public void mutate()
        //{

        //}



        public void loadImage(string filepath)
        {
            image = null;
            try
            {
                image = new Bitmap(filepath);
            }
            catch { Console.WriteLine("could not load/find file: " + filepath); }

        }

        public static void runPythonEvaluator()
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

            /*
            TODO:

            how to implement fitness?
            comparable and sortable individuals, by which fitness type?
            
            selection
            crossover
                coordinateView should keep track of other possible segments to fall back to?
                or instead:
                select one parent to be dominant for each segment (segment-id?)
                for every pixel in that segment 
                define 
            mutation
            offspring selection

            Things to try:
            - CIE Lab colour instead of RGB
            - 1/F(j) instead of just 1/8 equal weight for all neighbors
            - does skipping fitness eval on image frame matter?
            - skip calculating segment centre if no pixels are added/removed
                - hashmap maybe?

            */

            Program program = new Program(50, 100);

            string solutionDir = Directory.GetCurrentDirectory() + "\\..\\..\\..\\";
            String[] images = { "86016", "118035", "147091", "176035", "176039", "353013" };
            string trainImageFolderPath = solutionDir + "Project_3_training_images\\" + images[1];
            string evaluatorPath = solutionDir + "Project_3_evaluator\\";
            string imagePath = trainImageFolderPath + "\\Test image.jpg";
            string solutionsFolder = evaluatorPath + "student_segments";


            //Program.moveImages(trainImageFolderPath, evaluatorPath + "optimal_segments", "GT_*.jpg", true);
            //Program.moveImages(trainImageFolderPath, solutionsFolder, "s*.jpg", true);

            //Program.runPythonEvaluator();


            program.loadImage(imagePath);
            program.run();


            //Individual individual = new Individual(program.image, 150);
            //individual.initializeSegmentsGrid(10, 10);

            //Bitmap segmentedImage = individual.getBitmap();
            //Program.saveSolution(segmentedImage, solutionsFolder + "\\grid.png");


        }
    }


}




