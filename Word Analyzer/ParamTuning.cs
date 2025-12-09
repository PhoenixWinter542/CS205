using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Analyzer
{
	public class ParamTuning
	{
		private (Weights weights, double avgTries) best;
		private Random rand = new Random(DateTime.Now.Millisecond);
		double percent;
		Analyzer an;
		RunPercent run;
		
		private Weights createWeight()
		{
			Weights weight = new Weights();
			weight.incWeights = new List<double>();
			weight.posWeights = new List<double>();
			weight.baseIncWeights = new List<double>();
			weight.basePosWeights = new List<double>();
			return weight;
		}

		public Weights GetWeightDescendant()
		{
			return new Weights()
			{
				incWeights = new List<double>() { rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble() },
				posWeights = new List<double>() { rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble() },
				baseIncWeights = new List<double>() { rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble() },
				basePosWeights = new List<double>() { rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble() }
			};
		}

		public ParamTuning()
		{
			percent = 1;
			best.weights = new Weights()
			{
				incWeights = new List<double>() { 1, 1, 1, 1, 1 },
				posWeights = new List<double>() { 1, 1, 1, 1, 1 },
				baseIncWeights = new List<double>() { 1, 1, 1, 1, 1 },
				basePosWeights = new List<double>() { 1, 1, 1, 1, 1 }
			};
			run = new RunPercent();
		}

		public ParamTuning(double percent) : this()
		{
			this.percent = percent;
		}

		public (Weights, double) Run(int totalRuns)
		{
			best.avgTries = run.RunInverseSQLOnPercent(percent, best.weights);
			totalRuns--;
			for (int i = 0; i < totalRuns; i++)
			{
				Weights tmpWeights = GetWeightDescendant();
				double avgTries = run.RunInverseSQLOnPercent(percent, tmpWeights);
				if(avgTries < best.avgTries)
				{
					best.avgTries = avgTries;
					best.weights = tmpWeights;
				}
			}

			return best;
		}

	}
}
