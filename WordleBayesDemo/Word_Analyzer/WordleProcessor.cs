using System;
using System.Collections.Generic;
using System.Linq;

namespace Word_Analyzer
{
	public static class WordleProcessor
	{
		/// <summary>
		/// 0 = letter not in word, 1 = letter in word wrong position, 2 = correct position.
		/// </summary>
		public static List<(char, byte)> Process(string guess, string answer)
		{
			if (guess.Length != answer.Length)
				throw new ArgumentException("Guess and answer must have the same length.");

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
	}
}
