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
        public int numSegments;
        private Random random;
        public Fitness fitness; 


        public Individual(Bitmap image, int numSegments, Random random, bool coloredSegments = true)
        {
            this.random = random;
            this.fitness = new Fitness();
            this.numSegments = numSegments;
            this.image = image;

            segmentView = new Segment[numSegments];

            for (int i = 0; i < numSegments; i++)
            {
                if (coloredSegments)
                {
                    //Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
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

        public void initializeSegmentsGrid(int numRows, int numCols)
        {
            int rowSize = image.Height / numRows;
            int colSize = image.Width / numCols;

            //if(((image.Height / numRows) * numCols + (image.Width / numCols)) >= numSegments)
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

                    Pixel pixel = new Pixel(new Tuple<int, int>(x, y), segment, segment.color);// image.color);

                    addPixel(segment, pixel);

                }
                continue;
            }
            return;
            
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

        public void mutate_splashCirlce()
        {
            /*
             * select e.g. cirlce of (connected) pixels
             * get list of all segments these pixels belong to
             * change pixels to all have same segment
             * variable radius?
             */
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
                    for (int j = 0; j < 8; j++)
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
                            /* 

                            A is to the east of B       B A
                                                        i j
                            F_B(A) = 2
                            F_left(right) = 2;
                            F_current(neighbor) = 2;

                            // Connectivity
                            1 / (j+1)

                            */
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
                    Pixel pixel = new Pixel(Tuple.Create(x, y), writeSegment);
                    addPixel(writeSegment, pixel);

                    // Add neighboring pixels/coordinates to queue
                    for(int i = 0; i < Program.NEIGHBOR_ORDER.Length; i++)
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

                    if (x / coordinateView.GetLength(0) >= 0.5)
                        directionTowardsCentre[0] = -1;

                    if (y / coordinateView.GetLength(1) >= 0.5)
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
                                    Tuple.Create(scanX, scanY),
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

        public Bitmap getBitmap()
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    bitmap.SetPixel(x, y, coordinateView[x, y].color);
                }
            }

            return bitmap;
        }

        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;
            return (int)(((Individual)obj).fitness.edgeValue - fitness.edgeValue);
        }


    }

}
