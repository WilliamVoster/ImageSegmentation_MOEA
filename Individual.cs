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


        public Individual(Bitmap image, int numSegments)
        {
            this.random = new Random(1);
            this.fitness = new Fitness();
            this.numSegments = numSegments;
            this.image = image;

            segmentView = new Segment[numSegments];

            for (int i = 0; i < numSegments; i++)
            {
                segmentView[i] = new Segment(Color.FromArgb((30 + i) % 255, (60 + 3 * i) % 255, (10 + 2 * i) % 255));
                //segmentView[i] = new Segment(Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255)));
            }

            coordinateView = new Pixel[image.Width, image.Height];

            //for (int i = 0; i < image.Width; i++)
            //{
            //    for (int j = 0; j < image.Height; j++) 
            //    {
            //        coordinateView[i, j] = new LinkedList<Segment>();
            //    }
            //}
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
            //coordinateView[pixel.coordinate.Item1, pixel.coordinate.Item2] = null;
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

        public void crossover()
        {
            /*
             * 
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
