using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation_MOEA
{
    internal class Individual : IComparable
    {
        public Segment[] segmentView;
        public Pixel[,] coordinateView;
        public Bitmap image;
        public double[,] imageGradientMagnitudes;
        public Bitmap segmentBorders;
        public Bitmap overlayedSegmentation;
        public int numSegments;
        public int numUsedSegments;
        private Random random;
        public Fitness fitness; 
        public int front;
        public double? crowdingDistance;


        public Individual(Bitmap image, int numSegments, Random random, bool coloredSegments = true)
        {
            this.random = random;
            this.fitness = new Fitness();
            this.numSegments = numSegments;
            this.image = image;
            this.front = int.MaxValue;

            segmentView = new Segment[numSegments];

            for (int i = 0; i < numSegments; i++)
            {
                if (coloredSegments)
                {
                    segmentView[i] = new Segment(
                        i,
                        Color.FromArgb(
                            (30 + i) % 255, 
                            (60 + 3 * i) % 255, 
                            (10 + 2 * i) % 255));
                }
                else
                {
                    segmentView[i] = new Segment(
                        i,
                        Color.FromArgb(0, 0, 0));
                }
            }

            coordinateView = new Pixel[image.Width, image.Height];

        }

        public void initializeSegmentsGrid()
        {
            int numRows = (int)Math.Floor(Math.Sqrt(numSegments));
            int numCols = numRows;

            int rowSize = (image.Height-1) / numRows;
            int colSize = (image.Width-1) / numCols;

            if (numRows * numCols >= numSegments)
            {
                throw new Exception("Too few segments");
            }
            
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {

                    int row = y / rowSize;
                    int col = x / colSize;

                    Segment segment = segmentView[row * numCols + col];

                    Pixel pixel = new Pixel(new Tuple<int, int>(x, y), segment, image.GetPixel(x, y));

                    addPixel(segment, pixel);

                }
                continue;
            }
            return;
            
        }

        public static double[,] calcImageGradients(Bitmap image)
        {
            // Calc greyscale image
            Bitmap greyscale = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int pixelGrey = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    greyscale.SetPixel(x, y, Color.FromArgb(pixelGrey, pixelGrey, pixelGrey));
                }
            }

            // Calc gaussian blurred image
            Bitmap blurred = new Bitmap(image.Width, image.Height);
            double[,] kernel = { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };
            double sumKernel = 16;
            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    double sumPixel = 0;
                    for (int i = -1; i < 2; i++)
                    {
                        for (int j = -1; j < 2; j++)
                        {
                            sumPixel += kernel[i + 1, j + 1] * greyscale.GetPixel(x + i, y + j).R;
                        }
                    }

                    int value = (int)(sumPixel / sumKernel);
                    if (value < 0) value = 0;
                    if (value > 255) value = 255;

                    blurred.SetPixel(x, y, Color.FromArgb(value, value, value));
                }
            }

            // Calc image gradient magnitudes
            double[,] imageGradientMagnitudes = new double[image.Width, image.Height];

            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    double horizontalGradient = 0;
                    horizontalGradient += blurred.GetPixel(x - 1, y - 1).R;
                    horizontalGradient += 2 * blurred.GetPixel(x - 1, y).R;
                    horizontalGradient += blurred.GetPixel(x - 1, y + 1).R;
                    horizontalGradient -= blurred.GetPixel(x + 1, y - 1).R;
                    horizontalGradient -= 2 * blurred.GetPixel(x + 1, y).R;
                    horizontalGradient -= blurred.GetPixel(x + 1, y + 1).R;


                    double verticalGradient = 0;
                    verticalGradient += blurred.GetPixel(x - 1, y - 1).R;
                    verticalGradient += 2 * blurred.GetPixel(x, y - 1).R;
                    verticalGradient += blurred.GetPixel(x + 1, y - 1).R;
                    verticalGradient -= blurred.GetPixel(x - 1, y + 1).R;
                    verticalGradient -= 2 * blurred.GetPixel(x, y + 1).R;
                    verticalGradient -= blurred.GetPixel(x + 1, y + 1).R;

                    imageGradientMagnitudes[x, y] = Math.Sqrt(
                        horizontalGradient * horizontalGradient + verticalGradient * verticalGradient);
                }
            }

            return imageGradientMagnitudes;
        }

        public void initializeSegmentsSobel(int threshholdMin, int thresholdMax)
        {
            /* 
             * Requires imageGradientMagnitudes to be calculated first.
             */

            double threshold = (random.NextDouble() * (thresholdMax - threshholdMin)) + threshholdMin;
            Bitmap imageSobelEdges = new Bitmap(image.Width, image.Height);
            bool[,] visited = new bool[image.Width, image.Height];
            int segmentIndex = 0;

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (imageGradientMagnitudes[x, y] < threshold && !visited[x, y])
                    {
                        floodFillSegmentSobel(
                            threshold, 
                            segmentView[segmentIndex], 
                            new Tuple<int, int>(x, y), 
                            visited
                        );

                        segmentIndex++;

                        if (segmentIndex >= segmentView.Length)
                            segmentIndex = segmentView.Length - 1;

                    }
                }
            }

            floodFillEmptyPixels();
        }

        private void floodFillSegmentSobel(
            double threshold, 
            Segment writeSegment, 
            Tuple<int, int> startPoint, 
            bool[,] visited
        )
        {
            Queue<Tuple<int, int>> toFill = new Queue<Tuple<int, int>>();

            toFill.Enqueue(startPoint);

            while (toFill.Count > 0)
            {
                (int x, int y) = toFill.Dequeue();

                if (
                    x >= 0 &&
                    x < coordinateView.GetLength(0) &&
                    y >= 0 &&
                    y < coordinateView.GetLength(1) &&

                    !visited[x, y] &&
                    imageGradientMagnitudes[x, y] < threshold
                )
                {
                    Pixel pixel = new Pixel(new Tuple<int, int>(x, y), writeSegment, image.GetPixel(x, y));
                    addPixel(writeSegment, pixel);
                    visited[x, y] = true;

                    // Add neighboring pixels/coordinates to queue
                    for (int i = 0; i < 4; i++)
                    //for (int i = 0; i < Program.NEIGHBOR_ORDER.GetLength(0); i++)
                    {
                        toFill.Enqueue(
                            Tuple.Create(
                                x + Program.NEIGHBOR_ORDER[i, 0],
                                y + Program.NEIGHBOR_ORDER[i, 1]));

                    }
                }
                
            }
        }

        public void addPixel(Segment segment, Pixel pixel)
        {
            pixel.segment = segment;
            segment.pixels.Add(pixel.coordinate, pixel);
            coordinateView[pixel.coordinate.Item1, pixel.coordinate.Item2] = pixel;
        }

        public void removePixel(Segment segment, Pixel pixel)
        {
            segment.pixels.Remove(pixel.coordinate);
            //pixel.segment = null;
            //coordinateView[pixel.coordinate.Item1, pixel.coordinate.Item2] = null; //needed for flood fill?
        }

        public int getNumUsedSegments()
        {
            /* 
             * Returns the number of used segments of the fixed ones available to it
             */

            numUsedSegments = 0;
            foreach (Segment segment in segmentView)
            {
                if (segment.pixels.Count > 0)
                    numUsedSegments++;
            }
            return numUsedSegments;
        }


        public void mutate_growBorder()
        {
            /*
             * given a segment (% chance to happen for each segment)
             * 
             */
        }

        public Fitness calcFitness()
        {
            // Calculate all segment centres
            for (int i = 0; i < segmentView.Length; i++)
            {
                if (segmentView[i].pixels.Count <= 0) continue;
                segmentView[i].calcCentre();
            }

            int neighbor_x;
            int neighbor_y;
            Pixel neighbor;
            Pixel pixel;
            double radicand;
            fitness.edgeValue = 0;
            fitness.connectivity = 0;
            fitness.overallDeviation = 0;
            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    for (int j = 0; j < Program.NEIGHBOR_ORDER.GetLength(0); j++)
                    {
                        neighbor_x = Program.NEIGHBOR_ORDER[j, 0];
                        neighbor_y = Program.NEIGHBOR_ORDER[j, 1];
                        neighbor = coordinateView[x + neighbor_x, y + neighbor_y];
                        pixel = coordinateView[x, y];

                        if (pixel.segment != neighbor.segment)
                        {
                            // Edge value
                            radicand = Math.Pow(pixel.color.R - neighbor.color.R, 2);
                            radicand += Math.Pow(pixel.color.G - neighbor.color.G, 2);
                            radicand += Math.Pow(pixel.color.B - neighbor.color.B, 2);
                            fitness.edgeValue += Math.Sqrt(radicand);

                            // Connectivity
                            fitness.connectivity += 0.125; //i.e. 1/8
                        }


                        // Overall deviation
                        radicand = Math.Pow(pixel.segment.centre[0] - pixel.coordinate.Item1, 2);
                        radicand += Math.Pow(pixel.segment.centre[1] - pixel.coordinate.Item2, 2);
                        fitness.overallDeviation += Math.Sqrt(radicand);

                    }
                }
            }
            return fitness;

        }

        public void floodFillSegment(
            Segment writeSegment, 
            Individual readIndividual, 
            Tuple<int, int>? startPoint = null, 
            bool ignoreFillWithinSegmentRule = false
        )
        {
            /* 
             * Starting from writeSegment centre
             * floodfill and create pixels belonging to writeSegment
             * untill hit pixel belonging to another segment. i.e. if pixel already exists dont overwrite
             * 
             * Params:
             *      writeSegment    Reference to this individual's segment to fill. Instead of having to
             *                      search for it in segmentview
             *      readIndividual  other individual to read segment info from
             *      ignore...rule   false: only fills within readIndividual's given segment
             *                      true: lets algorithm floodfill outside the segment
             */
            if(readIndividual.segmentView[writeSegment.index].pixels.Count <= 0 && !ignoreFillWithinSegmentRule) return;

            Queue<Tuple<int, int>> toFill = new Queue<Tuple<int, int>>();

            if (startPoint == null)
                startPoint = Tuple.Create(writeSegment.centre[0], writeSegment.centre[1]);

            toFill.Enqueue(startPoint);

            while(toFill.Count > 0)
            {
                (int x, int y) = toFill.Dequeue();

                if
                (
                    x >= 0 &&
                    x < coordinateView.GetLength(0) &&
                    y >= 0 &&
                    y < coordinateView.GetLength(1) &&

                    coordinateView[x, y] == null &&

                    (ignoreFillWithinSegmentRule || 
                    writeSegment.index == readIndividual.coordinateView[x, y].segment.index)
                    
                )
                {
                    Pixel pixel = new Pixel(Tuple.Create(x, y), writeSegment, image.GetPixel(x, y));
                    addPixel(writeSegment, pixel);

                    // Add neighboring pixels/coordinates to queue
                    for(int i = 0; i < Program.NEIGHBOR_ORDER.GetLength(0); i++)
                    {
                        toFill.Enqueue(
                            Tuple.Create(
                                x + Program.NEIGHBOR_ORDER[i, 0], 
                                y + Program.NEIGHBOR_ORDER[i, 1]));

                    }
                    
                }
            }
        }

        public void floodFillEmptyPixels()
        {
            /* 
             * Look within coordinateView for pixels without a segment
             * go in one direction (towards center of image?) until hit a segment
             *      Fallback if can not find a segment with scanning: 
             *      look to see if we have an empty segment in segmentview and fill with that segment
             * fill from that border with said segment
             */

            int[] directionTowardsCentre = new int[2];
            int scanX;
            int scanY;
            for (int x = 0; x < coordinateView.GetLength(0); x++)
            {
                for (int y = 0; y < coordinateView.GetLength(1); y++)
                {
                    if (coordinateView[x, y] != null) continue;

                    directionTowardsCentre[0] = 1;
                    directionTowardsCentre[1] = 1;

                    if (x / (double) coordinateView.GetLength(0) >= 0.5)
                        directionTowardsCentre[0] = -1;

                    if (y / (double) coordinateView.GetLength(1) >= 0.5)
                        directionTowardsCentre[1] = -1;

                    try
                    {
                        for (int i = 1; i < coordinateView.GetLength(0) * coordinateView.GetLength(1); i++)
                        {
                            scanX = x + (i * directionTowardsCentre[0]);
                            scanY = y + (i * directionTowardsCentre[1]);


                            if (coordinateView[scanX, scanY] != null)
                            {
                                floodFillSegment(
                                    coordinateView[scanX, scanY].segment,
                                    this,
                                    Tuple.Create(x, y),
                                    true);
                                break;
                            }

                            if (i >= coordinateView.GetLength(0))
                                Console.WriteLine("floodfillempty() Should not run for this long!");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Catch when above loop goes outside the image frame
                        // and does not hit another segment before that

                        Console.WriteLine("this is hopefully a rare occurrance: " + ex.ToString());

                        for (int j = 0; j < segmentView.Length; j++)
                        {
                            if (segmentView[j].pixels.Count == 0)
                            {
                                floodFillSegment(
                                        segmentView[j],
                                        this,
                                        Tuple.Create(x, y),
                                        true);
                                segmentView[j].centre = [x, y]; // probably unnecessary since calcFitness
                            }
                        }

                        if (coordinateView[x, y] == null) { Console.WriteLine("how the fuck?"); }

                    }
                }
            }
        }

        public Bitmap getBitmap_fill()
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    bitmap.SetPixel(x, y, coordinateView[x, y].segment.color);
                }
            }

            return bitmap;
        }

        public Bitmap getBitmap_border()
        {

            Bitmap bitmap = new Bitmap(image.Width, image.Height);
            Pixel neighbor;

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, 0, 0, bitmap.Width, bitmap.Height);
                }
            }
            

            for (int i = 0; i < numSegments; i++)
            {
                foreach (Pixel pixel in segmentView[i].pixels.Values)
                {
                    // Don't check the border pixels
                    if (
                        pixel.coordinate.Item1 <= 0 ||
                        pixel.coordinate.Item1 >= coordinateView.GetLength(0) - 1 ||
                        pixel.coordinate.Item2 <= 0 ||
                        pixel.coordinate.Item2 >= coordinateView.GetLength(1) - 1
                    ) { continue; }

                    for(int j = 0; j < 4; j++) //first 4 of NEIGHBOR_ORDER = 4-connectedness
                    {
                        neighbor = coordinateView[
                            pixel.coordinate.Item1 + Program.NEIGHBOR_ORDER[j, 0],
                            pixel.coordinate.Item2 + Program.NEIGHBOR_ORDER[j, 1]];


                        if (neighbor.segment == pixel.segment)
                        {
                            continue;
                        }

                        Color test = bitmap.GetPixel(neighbor.coordinate.Item1, neighbor.coordinate.Item2);
                        if (bitmap.GetPixel(neighbor.coordinate.Item1, neighbor.coordinate.Item2).Name != "ffffffff")
                        {
                            continue;
                        }

                        bitmap.SetPixel(pixel.coordinate.Item1, pixel.coordinate.Item2, Color.Black);

                    }
                }
            }

            // Fill border
            for (int x = 0; x < coordinateView.GetLength(0); x++) 
            {

                bitmap.SetPixel(x, 0, Color.Black);
                bitmap.SetPixel(x, coordinateView.GetLength(1) - 1, Color.Black);
            }
            for (int y = 0; y < coordinateView.GetLength(1); y++)
            {
                bitmap.SetPixel(0, y, Color.Black);
                bitmap.SetPixel(coordinateView.GetLength(0) - 1, y, Color.Black);
            }

            segmentBorders = bitmap;
            return bitmap;
        }

        public Bitmap getBitmap_overlayImage()
        {
            overlayedSegmentation = (Bitmap)image.Clone();


            for (int x = 0; x < coordinateView.GetLength(0); x++)
            {
                for (int y = 0; y < coordinateView.GetLength(1); y++)
                {
                    if (segmentBorders.GetPixel(x, y).Name != "ffffffff")
                    {
                        overlayedSegmentation.SetPixel(x, y, Color.Green);

                    }
                }
            }

            return overlayedSegmentation;
        }

        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;
            if (ReferenceEquals(this, obj)) return 0;

            Individual other = obj as Individual;

            if (fitness.weightedFitness == null && other.fitness.weightedFitness == null)
            {
                if (front < other.front) return -1;
                if (front > other.front) return 1;
                if (crowdingDistance == null || other.crowdingDistance == null) return 0;

                return (int)(other.crowdingDistance - crowdingDistance);
                
            }
            return (int)(fitness.weightedFitness - other.fitness.weightedFitness);

        }


    }
    internal class FitnessComparerEdgeValue : IComparer<Individual>
    {
        public int Compare(Individual x, Individual y)
        {
            return (int)(y.fitness.edgeValue - x.fitness.edgeValue);
        }
    }

    internal class FitnessComparerConnectivity : IComparer<Individual>
    {
        public int Compare(Individual x, Individual y)
        {
            return (int)(x.fitness.connectivity - y.fitness.connectivity);
        }
    }

    internal class FitnessComparerDeviation : IComparer<Individual>
    {
        public int Compare(Individual x, Individual y)
        {
            return (int)(x.fitness.overallDeviation - y.fitness.overallDeviation);
        }
    }
}
