using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
        public Image image;

        public int numSegments;


        public Individual(Image image, int numSegments)
        {
            this.numSegments = numSegments;
            this.image = image;

            segmentView = new Segment[numSegments];

            for (int i = 0; i < numSegments; i++)
            {
                segmentView[i] = new Segment();
            }

            coordinateView = new LinkedList<Segment>[image.width, image.height];
        }



        public void initializeSegmentsGrid(int numRows, int numCols)
        {

            if (numRows * numCols >= numSegments)
                throw new Exception("Too few segments");
            
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {

                    int row = y / numRows;
                    int col = x / numCols;

                    Segment segment = segmentView[row * numCols + col];

                    Pixel pixel = new Pixel(new Tuple<int, int>(x, y), segment, image.color);

                    addPixel(segment, pixel);

                }
            }
            
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

    }

}
