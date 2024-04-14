﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ImageSegmentation_MOEA
{
    internal class Pixel
    {
        //public static int CONNECTEDNESS = 8; // i.e. consider the neigboring 4 or 8 connected pxiels

        public Tuple<int, int> coordinate;
        public Segment segment;
        public Color color; // RGB, could use CIE lab

        
        public Pixel(Tuple<int, int> coordinate, Segment segment)
        {
            this.coordinate = coordinate;
            this.segment = segment;
        }
        public Pixel(Tuple<int, int> coordinate, Segment segment, Color color) : this(coordinate, segment)
        {
            this.color = color;
        }

    }
}
