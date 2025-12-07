using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Analyzer
{
	internal class Program
	{

		static void Main(string[] args)
		{
			Analyzer analyzer = new Analyzer(5);
			AnalysisResult initial = analyzer.AnalyzeWithCandidates(null);
			Search search = new Search((initial.letterPos, initial.letterInc));
			Console.WriteLine("Heuristic suggestion: " + search.Run());

			BayesianSearch bayes = new BayesianSearch(initial.candidates);
			Console.WriteLine("Bayesian suggestion: " + bayes.Run());

			List<(char, byte)> feedback = new List<(char, byte)> { ('s', 0), ('a', 1), ('n', 0), ('e', 1), ('s', 0) };
			AnalysisResult updated = analyzer.AnalyzeWithCandidates(feedback);
			search = new Search((updated.letterPos, updated.letterInc));
			Console.WriteLine("Heuristic suggestion after feedback: " + search.Run());

			bayes = new BayesianSearch(updated.candidates);
			Console.WriteLine("Bayesian suggestion after feedback: " + bayes.Run());
		}
	}
}
