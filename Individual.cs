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
    internal class Individual
    {
        public Segment[] segmentView;
        public LinkedList<Segment>[,] coordinateView;
        public Bitmap image;

        public int numSegments;
        private Random random;


        public Individual(Bitmap image, int numSegments)
        {
            this.random = new Random(1);
            this.numSegments = numSegments;
            this.image = image;

            segmentView = new Segment[numSegments];

            for (int i = 0; i < numSegments; i++)
            {
                segmentView[i] = new Segment(Color.FromArgb((30 + i) % 255, (60 + 3 * i) % 255, (10 + 2 * i) % 255));
                //segmentView[i] = new Segment(Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255)));
            }

            coordinateView = new LinkedList<Segment>[image.Width, image.Height];

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++) 
                {
                    coordinateView[i, j] = new LinkedList<Segment>();
                }
            }
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
            segment.pixels.Add(pixel.coordinate, pixel);
            coordinateView[pixel.coordinate.Item1, pixel.coordinate.Item2].AddLast(segment);
        }

        public void removePixel(Segment segment, Pixel pixel)
        {
            segment.pixels.Remove(pixel.coordinate);
            coordinateView[pixel.coordinate.Item1, pixel.coordinate.Item2].Remove(segment);
        }

        public Bitmap getBitmap()
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    bitmap.SetPixel(x, y, coordinateView[x, y].First.Value.color);
                }
            }

            return bitmap;
        }

    }

}
