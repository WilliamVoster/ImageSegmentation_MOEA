using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation_MOEA
{
    internal class Segment
    {

        public Dictionary<Tuple<int, int>, Pixel> pixels;  //check if segment is used with segment.pixels.Count > 0
        //public LinkedList<Segment> neighbors;  // relplaced by coordinateView in individual
        public Color color;

        public Segment(Color color) 
        {
            pixels = new Dictionary<Tuple<int, int>, Pixel>();
            this.color = color;
        }



    }
}
