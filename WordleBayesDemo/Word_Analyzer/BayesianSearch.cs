#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Word_Analyzer
{
	/// <summary>
	/// Ranks guesses by expected information gain (Shannon entropy) over the remaining candidate answers.l
	/// </summary>
	public class BayesianSearch
	{
		private readonly List<string> answers;
		private readonly List<string> guesses;
		private readonly Dictionary<string, Dictionary<string, string>> patternCache;

		public BayesianSearch(List<string> answers, List<string> guesses = null)
		{
			this.answers = answers ?? new List<string>();
			this.guesses = (guesses == null || !guesses.Any()) ? this.answers : new List<string>(guesses);
			patternCache = new Dictionary<string, Dictionary<string, string>>();
		}

		private string EncodePattern(string guess, string answer)
		{
			if (patternCache.TryGetValue(guess, out Dictionary<string, string> answerCache) &&
				answerCache.TryGetValue(answer, out string cached))
				return cached;

			var feedback = WordleProcessor.Process(guess, answer);
			StringBuilder sb = new StringBuilder(feedback.Count);
			foreach ((char letter, byte state) in feedback)
			{
				sb.Append(state);
			}

			string pattern = sb.ToString();
			if (false == patternCache.TryGetValue(guess, out answerCache))
			{
				answerCache = new Dictionary<string, string>();
				patternCache[guess] = answerCache;
			}
			answerCache[answer] = pattern;
			return pattern;
		}

		private double ScoreGuess(string guess)
		{
			if (answers.Count == 0)
				return double.NegativeInfinity;

			Dictionary<string, int> buckets = new Dictionary<string, int>();
			foreach (string answer in answers)
			{
				string pattern = EncodePattern(guess, answer);
				if (buckets.ContainsKey(pattern))
					buckets[pattern]++;
				else
					buckets[pattern] = 1;
			}

			double total = answers.Count;
			double entropy = 0;
			foreach (int count in buckets.Values)
			{
				double p = count / total;
				entropy -= p * Math.Log(p, 2);
			}
			return entropy;
		}

		public string Run()
		{
			string best = guesses.FirstOrDefault() ?? string.Empty;
			double bestScore = double.NegativeInfinity;

			foreach (string guess in guesses)
			{
				double score = ScoreGuess(guess);
				if (score > bestScore || (Math.Abs(score - bestScore) < 1e-9 && string.CompareOrdinal(guess, best) < 0))
				{
					bestScore = score;
					best = guess;
				}
			}

			return best;
		}
	}
}
