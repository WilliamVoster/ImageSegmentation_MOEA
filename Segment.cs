﻿using System;
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

        public Dictionary<Tuple<int, int>, Pixel> pixels;
        public Color color;
        public int[] centre; // Pixel coordinate of segmentation centre
        public int index;

        public Segment(int index, Color color) 
        {
            this.index = index;
            pixels = new Dictionary<Tuple<int, int>, Pixel>();
            this.color = color;
            centre = new int[2];
        }

        public int[] calcCentre()
        {
            double xSum = 0;
            double ySum = 0;
            foreach (var (x, y) in pixels.Keys)
            {
                xSum += x;
                ySum += y;
            }
            centre[0] = (int) xSum / pixels.Count;
            centre[1] = (int) ySum / pixels.Count;
            return centre;
        }


    }
}
