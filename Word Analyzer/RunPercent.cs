using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace Word_Analyzer
{
	public class RunPercent
	{
		private readonly int WORDCOUNT;
		Analyzer an;
		public RunPercent()
		{
			this.an = new Analyzer(5);
			WORDCOUNT = an.GetWordCount();
		}

		public double RunInverseSQLOnPercent(double percent, Weights weights)
		{
			an.ChangeWeights(weights);
			if (percent <= 0)
				return 0;
			if (percent > 100)
				percent = 100;
			int runs = (int)Math.Ceiling(WORDCOUNT * percent * 0.01);
			int totalGuesses = 0;
			for (int i = 0; i < runs; i++)
			{
				totalGuesses += WordleProcessor.InverseWordleSQL(WordleProcessor.GetWordAt(i, runs), an).Count;
			}
			return (double)totalGuesses / (double)runs;
		}

		public double RunRandInverseSQLOnPercent(double percent, Weights weights)
		{
			an.ChangeWeights(weights);
			if (percent <= 0)
				return 0;
			if (percent > 100)
				percent = 100;
			int runs = (int)Math.Ceiling(WORDCOUNT * percent * 0.01);
			int totalGuesses = 0;
			for (int i = 0; i < runs; i++)
			{
				an.Reset();
				totalGuesses += WordleProcessor.InverseWordleSQL(WordleProcessor.GetRandWord()).Count;
			}
			return (double)totalGuesses / (double)runs;
		}
	}
}
