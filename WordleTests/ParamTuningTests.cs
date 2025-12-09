using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Word_Analyzer;
using System.Data;
using System.Data.SqlClient;


namespace WordleTests
{
	[TestClass]
	public class ParamTuningTests
	{
		private void printWeights(List<double> weights)
		{
			foreach (double weight in weights)
			{
				Console.Write("\t" + weight);
			}
			Console.WriteLine("\n");
		}

		[TestMethod]
		public void ParamTuningTest()
		{
			double percent = 0.1;
			int generations = 100;
			ParamTuning param = new ParamTuning(percent);
			(Weights weights, double avgTries) best = param.Run(generations);
			Console.WriteLine(
				"Ran on " + percent + "% of words\n" +
				"Tested " + generations + " generations\n"
				);
			Console.WriteLine("incWeights");
			printWeights(best.weights.incWeights);
			Console.WriteLine("posWeights");
			printWeights(best.weights.posWeights);
			Console.WriteLine("avgTries:\t" + best.avgTries);
		}
	}
}
