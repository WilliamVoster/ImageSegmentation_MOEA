using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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


        public string trainImageFolderPath;
        public string evaluatorPath;
        public string imagePath;
        public string solutionsFolder;


        Bitmap image;
        Random random;
        int popSize;
        int numGenerations;
        int numSegments;
        //Individual[] population;
        List<Individual> population;

        Program(int popSize, int numGenerations, int numSegments) 
        {
            this.random = new Random(1);
            this.popSize = popSize;
            this.numGenerations = numGenerations;
            this.numSegments = numSegments;
            //population = new Individual[popSize];
            population = new LinkedList<Individual>();
        }

        public void setPaths(
            string trainImageFolderPath,
            string evaluatorPath,
            string imagePath,
            string solutionsFolder
        )
        {
            this.trainImageFolderPath = trainImageFolderPath;
            this.evaluatorPath = evaluatorPath;
            this.imagePath = imagePath;
            this.solutionsFolder = solutionsFolder;

        }

        public void run()
        {
            // Init population
            for (int i = 0; i < popSize; i++)
            {
                Individual individual = new Individual(image, numSegments, random);
                individual.initializeSegmentsGrid(10, 10);
                individual.calcFitness();
                population.Add(individual);
            }


            for (int i = 0; i < numGenerations; i++)
            {
                // Logging
                Console.WriteLine("Generation: " + i);



                // Selection
                Individual[] parents = selectionParents(popSize);

                // Crossover
                Individual[] children = crossover(parents);

                // Mutation
                children = mutateSplashCircle(children);

                // Add children
                for (int j = 0; j < popSize; j++)
                    population.Add(children[j]);

                // Sort
                sortPopulation(popSize);




            }

            // Displaying/saving
            Bitmap segmentedImage = population[0].getBitmap_border();
            Program.saveSolution(segmentedImage, solutionsFolder + "\\test_child.png");

            Bitmap overlayedImage = population[0].getBitmap_overlayImage();
            Program.saveSolution(overlayedImage, solutionsFolder + "\\test_child_overlay.png");
        }

        public Individual[] selectionParents(int numParents)
        {
            Individual[] parents = new Individual[numParents];

            LinkedListNode< Individual> iter = population.First;
            for (int i = 0; i < numParents; i++)
            {
                parents[i] = iter.Value;
                iter = iter.Next;
            }

            return parents;
        }

        //public void selectionCutOff(int numToKeep)
        //{
        //    /* 
        //     * Expects the population to already be sorted
        //     * operates on population
        //     * deletes numToKeep from end of list
        //     */
            
        //    int numToDelete = population.Count - numToKeep;

        //    for (int i = 0; i < numToDelete; i++)
        //    {
        //        population.RemoveLast();
        //    }
        //}

        public void sortPopulation(int cutOffPoint)
        {
            /* 
             * Sorts population first based on fronts and then by crowding distance
             * if SGA - just call Sort()
             * else set front number and calc crowding distance for each individual first
             *      since required in Individual.CompareTo()
             * cut off excess individuals to return population to length = cutOffPoint/popSize
             */

            // If pareto-front sorting
            if (population[0].fitness.weightedFitness == null)
            {
                int frontNumber = 0;
                List<Individual> checkedIndividuals = new List<Individual>(population.Count * 2);
                while(checkedIndividuals.Count <= cutOffPoint)
                {
                    frontNumber++;
                    for (int i = 0; i < population.Count; i++)
                    {
                        bool inFront = true;
                        for (int j = 0; j < population.Count; j++)
                        {
                            if (!checkedIndividuals.Contains(population[i]) && 
                                population[i].fitness.isDominatedBy(population[j].fitness))
                            {
                                inFront = false;
                                break;
                            }
                        }

                        if (inFront)
                        {
                            population[i].front = frontNumber;
                            checkedIndividuals.Add(population[i]);
                        }
                    }
                }

                // Individuals in last front are subject to further sorting and need crowding distance
                for (int i = checkedIndividuals.Count - 1; i >= 0; i--)
                {
                    if (checkedIndividuals[i].front < frontNumber) break;
                    checkedIndividuals[i].fitness.calcCrowdingDistance();
                }

                population = checkedIndividuals;
            }

            population.Sort();

            // Cut off excess 
            if (population.Count > popSize)
                population.RemoveRange(popSize, popSize - population.Count);
        }

        public Individual[] crossover(Individual[] parents)
        {
            /*
             * split num ~%50 segments from each parent (make more random somehow, not just first x)
             * take one half from each. parent 1 is dominant for child 1, partent 2 for child 2
             *      dominant in terms of their segment has priority i.e. added first to the new child
             * add to child with empty pixels (how to generate efficiantly?)  
             * for every segment, flood fill (within segment) from each centroid
             *      until hitting other segment, starting with highest priority
             *      randomize segment priorities somehow?
             * loop over cooridnateview/pixels for empty pixels
             *      choose (randomly? (or just toward center of image) one of the 4 directions 
             *      go that same direction until hit a segment
             *      flood fill from border with said segment
             *      now, still in same loop looking for empty pixels --> continue
             * re-calc for children:
             *      fitness (which includes centroid)
             *      or should i wait until after mutation
             *      calc fitness in offspring selection instead!
             */

            int splitIndex;
            Segment child1Segment;
            Segment child2Segment;

            Individual[] children = new Individual[parents.Length];
            
            for (int i = 0; i < parents.Length; i += 2)
            {
                splitIndex = random.Next(numSegments - 2) + 1; //makes sure not all segments from one parent


                children[i] = new Individual(image, numSegments, random, true);
                children[i+1] = new Individual(image, numSegments, random, true);

                // Each segment is flood filled within segments from parent 1 and 2, split on splitIndex
                for (int j = 0; j < numSegments; j++)
                {
                    child1Segment = children[i].segmentView[j];
                    child2Segment = children[i+1].segmentView[j];

                    if (j < splitIndex)
                    {
                        // Child 1 gets first part of parent 1
                        child1Segment.centre[0] = parents[i].segmentView[j].centre[0];
                        child1Segment.centre[1] = parents[i].segmentView[j].centre[1];
                        children[i].floodFillSegment(child1Segment, parents[i]);

                        // Child 2 gets first part of parent 2
                        child2Segment.centre[0] = parents[i+1].segmentView[j].centre[0];
                        child2Segment.centre[1] = parents[i+1].segmentView[j].centre[1];
                        children[i+1].floodFillSegment(child2Segment, parents[i+1]);

                    }
                    else
                    {
                        // Child 1 gets second part of parent 2
                        child1Segment.centre[0] = parents[i+1].segmentView[j].centre[0];
                        child1Segment.centre[1] = parents[i+1].segmentView[j].centre[1];
                        children[i].floodFillSegment(child1Segment, parents[i+1]);

                        // Child 2 gets second part of parent 1
                        child2Segment.centre[0] = parents[i].segmentView[j].centre[0];
                        child2Segment.centre[1] = parents[i].segmentView[j].centre[1];
                        children[i+1].floodFillSegment(child2Segment, parents[i]);
                    }
                }

                // Find segmentless pixels
                children[i].floodFillEmptyPixels();
                children[i+1].floodFillEmptyPixels();

            }

            return children;
        }

        public Individual[] mutateSplashCircle(Individual[] children)
        {
            /* 
             * Generate random cirlce (or other shape) collection of pixels
             * look at segments they belong to and set them to one of the segments (at random?)
             * Varying radius/size
             */

            int radius = 3;
            int numCircles = 3;
            Individual child;
            Pixel pixel;
            List<Segment> circleSegments = new List<Segment>();

            // Could be static - i.e. moved to main string args..
            List<(int, int)> circleCoordinates = new List<(int, int)>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x*x + y*y <= radius * radius)
                    {
                        circleCoordinates.Add((x, y));
                    }
                }
            }

            for (int i = 0; i < children.Length; i++)
            {
                for (int j = 0; j < numCircles; j++)
                {
                    child = children[i];
                    int x = random.Next(child.coordinateView.GetLength(0) - 2 * radius) + radius;
                    int y = random.Next(child.coordinateView.GetLength(1) - 2 * radius) + radius;

                    circleSegments.Clear();

                    foreach (var (x_offset, y_offset) in circleCoordinates)
                    {
                        pixel = child.coordinateView[x + x_offset, y + y_offset];
                        if (!circleSegments.Contains(pixel.segment))
                        {
                            circleSegments.Add(pixel.segment);
                        }
                    }

                    Segment chosenSegment = circleSegments[random.Next(circleSegments.Count)];

                    foreach (var (x_offset, y_offset) in circleCoordinates)
                    {
                        pixel = child.coordinateView[x + x_offset, y + y_offset];
                        child.removePixel(pixel.segment, pixel);
                        child.addPixel(chosenSegment, pixel);
                    }
                }
            }

            return children;
        }

        public void mutateGrow()
        {
            /* 
             * Choose a segment, take pixels along border from neighboring segments
             */
            
        }

        public void mutateErode()
        {
            /* 
             * Choose a segment, move pixels along border to neighboring segments
             */

        }

        public void mutateMerge()
        {
            /* 
             * Choose a segment, find a neighboring/bordering segment
             * merge the two segments
             */

        }

        public void mutateSplit()
        {
            /* 
             * If exists a segment with no pixels
             * Choose a segment, split the segment into two
             */

        }

        //public void selection(Individual[] parents, Individual[] children)
        //{
        //    for(int i = 0; i < popSize; i++)
        //    {
        //        Fitness childFitness = children[i].calcFitness();
        //        if(childFitness.getWeightedFitness() > parents[i].fitness.getWeightedFitness() && random.NextDouble() < 0.8)
        //        {
        //            population[i] = children[i];

        //        }
        //    }
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

            unit test each method, show how image look like after e.g. mutation

            Things to try:
            - pre-process image before GA
                    e.g. edge detection
            - CIE Lab colour instead of RGB
            - 1/F(j) instead of just 1/8 equal weight for all neighbors
            - does skipping fitness eval on image frame matter?
            - skip calculating segment centre if no pixels are added/removed
                - hashmap maybe?
            - remove special color generation in individual constructor
            - when flood filling - multiple of same pixels are checked and added to queue
                    possible to add checked coordinates to a dictionary/hashmap to not add explored?
            - move circle coordinate calculation to main string args
            - compute time of each function to find issues
            - crossover from two parents doesnt include y=160
            */

            Program program = new Program(50, 100, 150);

            string solutionDir = Directory.GetCurrentDirectory() + "\\..\\..\\..\\";
            String[] images = { "86016", "118035", "147091", "176035", "176039", "353013" };
            // dimensions of fourth photo - 176039 had other dimensions than the ground truth photos.
            string trainImageFolderPath = solutionDir + "Project_3_training_images\\" + images[1];
            string evaluatorPath = solutionDir + "Project_3_evaluator\\";
            string imagePath = trainImageFolderPath + "\\Test image.jpg";
            string solutionsFolder = evaluatorPath + "student_segments";

            program.setPaths(trainImageFolderPath, evaluatorPath, imagePath, solutionsFolder);


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




