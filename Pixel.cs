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
        //public static int CONNECTEDNESS = 8; // i.e. consider the neigboring 4 or 8 connected pxiels

        public Tuple<int, int> coordinate;
        //public LinkedList<Pixel> children; // could be array
        //public Pixel? parent;
        //public int depth; // i.e. distance from node centre
        public Segment segment;
        public Color color; // RGB, could use CIE lab

        
        public Pixel(Tuple<int, int> coordinate, Segment segment, Color color)
        {
            this.coordinate = coordinate;
            this.segment = segment;
            this.color = color;
        }
        //public Pixel(Tuple<int, int> coordinate, Segment segment, int r, int g, int b) : this(coordinate, segment, Color.FromArgb(r, g, b))
        //{

        //}

        //private Pixel(Pixel parent, int depth, Segment segment)
        //{
        //    this.parent = parent;
        //    this.depth = depth;
        //    this.segment = segment;
        //    this.children = new LinkedList<Pixel>();
        //}
        //public Pixel(Tuple<int, int> coordinate, Pixel parent, int depth, Segment segment) : this(parent, depth, segment)
        //{
        //    this.coordinate = coordinate;
        //}
        //public Pixel(int x, int y, Pixel parent, int depth, Segment segment) : this(parent, depth, segment)
        //{
        //    this.coordinate = new Tuple<int, int>(x,y);
        //}


        //public void addChild(int x, int y)
        //{
        //    Pixel child = new Pixel(x, y, this, depth + 1, segment);
        //    children.AddLast(child);
        //}
        //public void addChild(Pixel child)
        //{
        //    children.Count >= Pixel.CONNECTEDNESS;

        //    child.parent = this;
        //    child.depth = depth + 1;//??
        //    child.segment = segment;


        //}

        //public void removeChild(Pixel child)
        //{

        //}

    }
}
