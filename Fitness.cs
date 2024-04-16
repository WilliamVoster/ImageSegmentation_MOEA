using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ImageSegmentation_MOEA
{
    internal class Fitness
    {

        public double edgeValue;
        public double connectivity;
        public double overallDeviation;

        public static double edgeValueWeight = -0.03;
        public static double connectivityWeight = 0.05;
        public static double overallDeviationWeight = 0.001;

        public double? weightedFitness;


        public Fitness() 
        {
            
        }

        public double getWeightedFitness()
        {
            weightedFitness = 0;
            weightedFitness += edgeValue * Fitness.edgeValueWeight;
            weightedFitness += connectivity * Fitness.connectivityWeight;
            weightedFitness += overallDeviation * Fitness.overallDeviationWeight;
            return (double)weightedFitness;
        }

        public bool isDominatedBy(Fitness other)
        {
            // 'Other' not worse than 'this' in all objectives
            if (!(
                other.edgeValue < edgeValue ||
                other.connectivity > connectivity ||
                other.overallDeviation > overallDeviation
            )) 
            {
                // 'Other' better than 'this' in one or more objectives
                if (
                    other.edgeValue > edgeValue ||
                    other.connectivity < connectivity ||
                    other.overallDeviation < overallDeviation
                )
                {
                    return true;
                }
            }

            return false;
        }

    }

    
}
