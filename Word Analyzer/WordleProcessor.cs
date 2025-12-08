using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Analyzer
{
	public static class WordleProcessor
	{
		public static List<(char, byte)> Process(string guess, string answer)
		{
			List<(char letter, byte state)> results = new List<(char letter, byte state)>();
			for (int i = 0; i < answer.Length; i++)
			{
				if (answer[i] == guess[i])
					results.Add((guess[i], 2));
				else if (answer.Contains(guess[i]))
					results.Add((guess[i], 1));
				else
					results.Add((guess[i], 0));
			}
			return results;
		}

		///<summary>
		///The first guess of search is always the same for each heuristic, this allows for different starts
		///</summary>
		public static List<string> InverseWordle(string firstGuess, string answer, int heur)
		{
			Analyzer an = new Analyzer(5);
			string result = firstGuess;
			List<string> guesses = new List<string> { result };

			while (result != answer)
			{
				Search search = new Search(an.Run(Process(result, answer)), heur);
				result = search.Run();
				guesses.Add(result);
				Debug.WriteLine(result);
			}

			an.Dispose();
			return guesses;
		}

		public static List<string> InverseWordleSQL(string answer)
		{
			Analyzer an = new Analyzer(5);
			string result = "saint";
			List<string> guesses = new List<string> { result };

			while (result != answer)
			{
				result = an.GetTopWord(Process(result, answer)).Item1;
				guesses.Add(result);
				Debug.WriteLine(result);
			}

			an.Dispose();
			return guesses;
		}

		public static string GetRandWord(string connectionString)
		{
			SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM english.dbo.words;", connectionString);
			DataSet set = new DataSet();
			adapter.Fill(set);
			DataTable words = set.Tables[0];
			Random rand = new Random(DateTime.Now.Millisecond);
			int pos =  (int)Math.Round((rand.NextDouble() * words.Rows.Count), 0);

			return words.Rows[pos][0].ToString();
		}

		public static string GetWordAt(int run, int totalRuns, string connectionString)
		{
			SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM english.dbo.words;", connectionString);
			DataSet set = new DataSet();
			adapter.Fill(set);
			DataTable words = set.Tables[0];
			int pos = (int)Math.Round((((double)run / totalRuns) * words.Rows.Count), 0);
			if(pos >= words.Rows.Count)
				pos = words.Rows.Count - 1;
			return words.Rows[pos][0].ToString();
		}
	}
}
