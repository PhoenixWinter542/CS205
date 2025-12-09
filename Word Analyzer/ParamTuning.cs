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
		int sigFig;
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
			sigFig = 1;
			best.weights = new Weights()
			{
				incWeights = new List<double>() { 0.5, 0.5, 0.5, 0.5, 0.5 },
				posWeights = new List<double>() { 0.5, 0.5, 0.5, 0.5, 0.5 },
				baseIncWeights = new List<double>() { -0.5, -0.5, -0.5, -0.5, -0.5 },
				basePosWeights = new List<double>() { -0.5, -0.5, -0.5, -0.5, -0.5 }
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

		private List<List<double>> StructToList(Weights weights)
		{
			return new List<List<double>>()
			{
				weights.incWeights,
				weights.posWeights,
				weights.baseIncWeights,
				weights.basePosWeights
			};
		}

		private Weights ListToStruct(List<List<double>> weightsAsList)
		{
			Weights converted = new Weights();
			converted.incWeights = weightsAsList[0];
			converted.posWeights = weightsAsList[1];
			converted.baseIncWeights = weightsAsList[2];
			converted.basePosWeights = weightsAsList[3];
			return converted;
		}

		private (double, double) RunHillWeight(List<List<double>> weights, int row, int col, double startWeight, double startTries)
		{
			double curWeight = weights[row][col];
			double move;
			if (curWeight < startWeight)
				move = (startWeight - curWeight) / 2;
			else if (curWeight > startWeight)
				move = (curWeight - startWeight) / 2;
			else
				move = Math.Min(1 - startWeight, startWeight) / 2;

			//Left
			List<List<double>> leftWeights = weights;
			leftWeights[row][col] = curWeight - move;
			double avgLeft = run.RunInverseSQLOnPercent(percent, ListToStruct(leftWeights));

			//Right
			List<List<double>> rightWeights = weights;
			rightWeights[row][col] = curWeight + move;
			double avgRight = run.RunInverseSQLOnPercent(percent, ListToStruct(rightWeights));

			if(Math.Round(startTries, sigFig) <= Math.Round(avgLeft, sigFig) && Math.Round(startTries, sigFig) <= Math.Round(avgRight, sigFig))
			{
				return (double.MaxValue, double.MaxValue);	//branch attemtps were worse than base, don't care about their values
			}
			if (Math.Round(avgLeft, sigFig) < Math.Round(avgRight, sigFig))	//branch left
			{
				if(curWeight < startWeight)	//came from left
					startWeight = curWeight;
				return RunHillWeight(leftWeights, row, col, startWeight, startTries);
			}
			else if (Math.Round(avgLeft, sigFig) > Math.Round(avgRight, sigFig))	//branch right
			{
				if (curWeight > startWeight)	//came from right
					startWeight = curWeight;
				return RunHillWeight(rightWeights, row, col, startWeight, startTries);
			}
			else	//Stop branching
			{
				return (leftWeights[row][col], avgLeft);
			}
		}

		public (Weights, double) RunHillSearch(int maxPasses, int sigFig)
		{
			int tmp = this.sigFig;
			this.sigFig = sigFig;
			(Weights, double) results = RunHillSearch(maxPasses);
			this.sigFig = tmp;
			return results;
		}


		public (Weights, double) RunHillSearch(int maxPasses)
		{
			double avgTries = run.RunInverseSQLOnPercent(percent, best.weights);
			List<List<double>> weightsAsList = StructToList(best.weights);
			for (int pass = 0; pass < maxPasses; pass++)
			{
				bool better = false;
				for (int row = 0;  row < weightsAsList.Count; row++)
				{
					for (int col = 0; col < weightsAsList[row].Count; col++)
					{
						double weight = weightsAsList[row][col];
						if (weight < 0)	//account for not having implemented the baseWeights
							break;
						(double weight, double avgTries) result = RunHillWeight(weightsAsList, row, col, weight, avgTries);

						if(result.avgTries < avgTries)
						{
							better = true;
							avgTries = result.avgTries;
							weightsAsList[row][col] = result.weight;
						}
					}
				}
				if (!better)	//No improvement, call it a day
					break;
			}

			return (ListToStruct(weightsAsList), avgTries);
		}

	}
}
