﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation_MOEA
{
    internal class Fitness
    {

        public double edgeValue;
        public double connectivity;
        public double overallDeviation;
        public double? weightedFitness;

        public Fitness() 
        {
            
        }

        public double getWeightedFitness()
        {
            weightedFitness =  -edgeValue + connectivity + overallDeviation;
            return (double)weightedFitness;
        }

    }
}
