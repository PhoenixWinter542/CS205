using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.SymbolStore;

namespace Word_Analyzer
{
	public class ParamTuning
	{
		private (Weights weights, double avgTries) best;
		private Random rand = new Random(DateTime.Now.Millisecond);
		double percent;
		int sigFig;
		string log = "";
		double scale;
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
			scale = 100;
			sigFig = 1;
			best.weights = new Weights()
			{
				incWeights = new List<double>() { scale / 2, scale / 2, scale / 2, scale / 2, scale / 2 },
				posWeights = new List<double>() { scale / 2, scale / 2, scale / 2, scale / 2, scale / 2 },
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

		private double GetNeighborWeight(bool movingRight, double startWeight, double move)
		{
			move *= rand.NextDouble();	//moves randomly within range
			if(movingRight)
				return startWeight + move;
			else
				return startWeight - move;
		}

		private List<List<double>> GetBranchScore(List<List<double>> weights, double startWeight, double move, bool movingRight, int row, int col, ref double newAvg, ref (double, double) result)
		{
			List<List<double>> newWeights = weights;
			newWeights[row][col] = GetNeighborWeight(true, startWeight, move);
			newAvg = run.RunInverseSQLOnPercent(percent, ListToStruct(newWeights));
			result = (newWeights[row][col], newAvg);
			return newWeights;
		}

		private (double, double) RunAnnealingWeight(List<List<double>> weights, int row, int col, double startWeight, double startTries, int depth, ref bool better, int temp)
		{
			double curWeight = weights[row][col];
			double move;
			if (curWeight < startWeight)
				move = startWeight;
			else if (curWeight > startWeight)
				move = (scale - startWeight);
			else
				move = Math.Min(scale - startWeight, startWeight);  //Move = distance to the closest side

			//move = move / Math.Pow(2, depth);   //reduce movement with recursion

			(double, double avgTries) result = (-1, -1);

			double newAvg = -1;

			if (curWeight < startWeight)   //came from left
			{
				//Left
				List<List<double>> newWeights = GetBranchScore(weights, startWeight, move, false, row, col, ref newAvg, ref result);

				if (startTries < newAvg)
					return RunAnnealingWeight(newWeights, row, col, startWeight, newAvg, depth + 1, ref better, Math.Max(temp - 1, 0));
			}
			else if(curWeight > startWeight) //came from right
			{
				//Right
				List<List<double>> newWeights = GetBranchScore(weights, startWeight, move, false, row, col, ref newAvg, ref result);

				if (startTries < newAvg)
					return RunAnnealingWeight(newWeights, row, col, startWeight, newAvg, depth + 1, ref better, Math.Max(temp - 1, 0));
			}
			else //just starting
			{
				//Left
				double leftAvg = -1;
				List<List<double>> leftWeights = GetBranchScore(weights, startWeight, move, false, row, col, ref leftAvg, ref result);

				if (startTries < newAvg)
					return RunAnnealingWeight(leftWeights, row, col, startWeight, newAvg, depth + 1, ref better, Math.Max(temp - 1, 0));

				//Right
				double rightAvg = -1;
				List<List<double>> rightWeights = GetBranchScore(weights, startWeight, move, false, row, col, ref rightAvg, ref result);

				List<List<double>> newWeights;
				if (leftAvg < rightAvg)
				{
					newWeights = leftWeights;
				}
				else
				{
					newWeights = rightWeights;
				}

				if (startTries < newAvg)
					return RunAnnealingWeight(newWeights, row, col, startWeight, newAvg, depth + 1, ref better, Math.Max(temp - 1, 0));
			}

			//Getting here means the new avgTries was worse
			bool takeWorse = false;
			(double, double) tmpResult = AnnealChoice((weights[row][col], startTries), result, ref takeWorse, temp);
			if (takeWorse)
			{
				List<List<double>> newWeights = GetBranchScore(weights, startWeight, move, false, row, col, ref newAvg, ref result);

				return RunAnnealingWeight(newWeights, row, col, startWeight, newAvg, depth + 1, ref better, Math.Max(temp - 1, 0));
			}
			else
			{
				return (startWeight, startTries);
			}
		}

		public (Weights, double) RunAnnealingSearch(int maxPasses, int sigFig)
		{
			int tmp = this.sigFig;
			this.sigFig = sigFig;
			(Weights, double) results = RunAnnealingSearch(maxPasses);
			this.sigFig = tmp;
			return results;
		}
		private string printWeights(List<double> weights)
		{
			string log = "\n\t";
			bool first = true;
			foreach (double weight in weights)
			{
				if (first)
					first = false;
				else
					log += ",";
				log += " " + weight;
			}
			log += "\n";
			return log;
		}

		private string writeLog(string passLog, DateTime start, int pass, int maxPasses)
		{
			TimeSpan runtime = DateTime.Now - start;
			string begin =
				"\n\n\n------------------------------------------------------------------------------------------------\n\n\n" +
				"Pass " + (pass + 1) + "\n" +
				"Duration: " + runtime.Hours + " hours  " + runtime.Minutes + " minutes  " + runtime.Seconds + " seconds\n" +
				"Run on " + percent + "% of words\n" +
				"Max " + maxPasses + " Passes\n" +
				"Rounding to  " + sigFig + " decimal digits\n";

			File.WriteAllText(@ConfigurationManager.ConnectionStrings["logFile"].ToString(),  begin + passLog + log);

			return begin + passLog;
		}

		public bool TakeNew(double change, int temp)
		{
			change = Math.Abs(change) / scale;	//change as a percent of the range
			double beatToTakeNew = (double)Math.Pow(change, (double)temp);	//change is always 0 <= change < 1, higher values of temp make it easer to take the new (worse) value
			double roll = rand.NextDouble();
			return roll > beatToTakeNew;
		}

		public (double, double) AnnealChoice((double weight, double score) oldWeight, (double weight, double score) newWeight, ref bool better, int temp)	//only called when new choice is not better
		{
			if (TakeNew(oldWeight.score - newWeight.score, temp))
			{
				better = true;
				return newWeight;
			}
			else
			{
				return oldWeight;
			}
		}

		public (Weights, double) RunAnnealingSearch(int maxPasses)
		{
			double avgTries = run.RunInverseSQLOnPercent(percent, best.weights);
			List<List<double>> weightsAsList = StructToList(best.weights);
			for (int pass = 0; pass < maxPasses; pass++)
			{
				DateTime start = DateTime.Now;
				string passLog = "";

				bool better = false;
				for (int row = 0;  row < weightsAsList.Count; row++)
				{
					if(0 == row)
					{
						passLog += "\nincWeights\n\n\t";
					}
					else if (1 == row)
					{
						passLog += "\n\n\nposWeights\n\n\t";
					}
					for (int col = 0; col < weightsAsList[row].Count; col++)
					{
						double weight = weightsAsList[row][col];
						if (weight < 0)	//account for not having implemented the baseWeights
							break;
						int temp = (maxPasses - pass) / 2;
						bool betterCol = false;
						(double weight, double avgTries) result = RunAnnealingWeight(weightsAsList, row, col, weight, avgTries, pass + 1, ref betterCol, temp);

						if(betterCol)
						{
							better = true;
							avgTries = result.avgTries;
							weightsAsList[row][col] = result.weight;
						}
						if (0 != col)
							passLog += ",";
						passLog += " " + weightsAsList[row][col];
						writeLog(passLog, start, pass, maxPasses);
					}
				}
				log = writeLog(passLog, start, pass, maxPasses) + log;
				if (!better)	//No improvement, call it a day
					break;
			}
			
			return (ListToStruct(weightsAsList), avgTries);
		}

	}
}
