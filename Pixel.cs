using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ImageSegmentation_MOEA
{
    internal class Pixel
    {

        public Tuple<int, int> coordinate;
        public Segment segment;
        public Color color; // RGB, could use CIE lab

        public Pixel(Tuple<int, int> coordinate, Segment segment, Color color)
        {
            this.coordinate = coordinate;
            this.segment = segment;
            this.color = color;
        }

    }
}
