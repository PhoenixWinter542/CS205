using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Word_Analyzer;


namespace WordleTests
{
	[TestClass]
	public class AnalyzerTests
	{
		List<(char, byte)> guess1 = new List<(char, byte)> { ('s', 0), ('a', 1), ('i', 0), ('n', 0), ('t', 1) };
		List<(char, byte)> guess2 = new List<(char, byte)> { ('t', 1), ('r', 0), ('a', 2), ('p', 0), ('e', 1) };
		List<(char, byte)> guess3 = new List<(char, byte)> { ('d', 0), ('e', 2), ('a', 2), ('t', 2), ('h', 0) };
		List<(char, byte)> guess4 = new List<(char, byte)> { ('m', 2), ('e', 2), ('a', 2), ('t', 2), ('y', 2) };

		[TestMethod]
		public void CreateRegexTest()
		{
			Analyzer an = new Analyzer(5);
			Assert.AreEqual("'a____'", an.CreateRegex(0, 'a'));
			Assert.AreEqual("'_a___'", an.CreateRegex(1, 'a'));
			Assert.AreEqual("'__a__'", an.CreateRegex(2, 'a'));
			Assert.AreEqual("'___a_'", an.CreateRegex(3, 'a'));
			Assert.AreEqual("'____a'", an.CreateRegex(4, 'a'));
			an.Dispose();
		}

		[TestMethod]
		public void CapitalizationTest()
		{
			Analyzer an = new Analyzer(5);
			an.bannedLetters = new List<char> { 'S', 'I', 'N', 'R', 'P', 'D', 'H' };
			Assert.AreEqual(14470, an.RemBanned());
			an.Dispose();
		}
		
		[TestMethod]
		public void UpdateLettersTest()
		{
			Analyzer an = new Analyzer(5);
			
			an.UpdateLetters(guess1);
			List<char> bannedLetters = new List<char>				{ 's', 'i', 'n' };
			List<char> reqLetters = new List<char>					{ 'a', 't' };
			List<(char, byte)> bannedPos = new List<(char, byte)> { ('a', 1), ('t', 4) };
			List<(char, byte)> reqPos = new List<(char, byte)>	{ };
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedLetters, bannedLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqLetters, reqLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedPos, bannedPos));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqPos, reqPos));

			an.UpdateLetters(guess2);
			bannedLetters = new List<char>		{ 's', 'i', 'n', 'r', 'p' };
			reqLetters = new List<char>			{ 'a', 't', 'e' };
			bannedPos = new List<(char, byte)> { ('a', 1), ('t', 4), ('t', 0), ('e', 4) };
			reqPos = new List<(char, byte)>	{ ('a', 2) };
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedLetters, bannedLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqLetters, reqLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedPos, bannedPos));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqPos, reqPos));

			an.UpdateLetters(guess3);
			bannedLetters = new List<char>		{ 's', 'i', 'n', 'r', 'p', 'd', 'h' };
			reqLetters = new List<char>			{ 'a', 't', 'e' };
			bannedPos = new List<(char, byte)> { ('a', 1), ('t', 4), ('t', 0), ('e', 4) };
			reqPos = new List<(char, byte)>	{ ('a', 2), ('e', 1), ('t', 3) };
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedLetters, bannedLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqLetters, reqLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedPos, bannedPos));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqPos, reqPos));

			an.UpdateLetters(guess4);
			bannedLetters = new List<char>		{ 's', 'i', 'n', 'r', 'p', 'd', 'h' };
			reqLetters = new List<char>			{ 'a', 't', 'e', 'm', 'y' };
			bannedPos = new List<(char, byte)> { ('a', 1), ('t', 4), ('t', 0), ('e', 4) };
			reqPos = new List<(char, byte)>	{ ('a', 2), ('e', 1), ('t', 3), ('m', 0), ('y', 4) };
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedLetters, bannedLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqLetters, reqLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedPos, bannedPos));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqPos, reqPos));
			an.Dispose();

			List<(char, byte)> feedback = new List<(char, byte)> { ('s', 0), ('a', 1), ('r', 0), ('e', 1), ('e', 1) };
			an = new Analyzer(5);
			an.UpdateLetters(feedback);
			bannedLetters = new List<char> { 's', 'r',};
			reqLetters = new List<char> { 'a', 'e' };
			bannedPos = new List<(char, byte)> { ('a', 1), ('e', 3), ('e', 4) };
			reqPos = new List<(char, byte)> { };
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedLetters, bannedLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqLetters, reqLetters));
			Assert.IsTrue(Enumerable.SequenceEqual(an.bannedPos, bannedPos));
			Assert.IsTrue(Enumerable.SequenceEqual(an.reqPos, reqPos));
			an.Dispose();
		}

		[TestMethod]
		public void GetAllowedTest()
		{
			Analyzer an = new Analyzer(5);

			an.bannedLetters = new List<char> { 's', 'i', 'n' };
			List <char> compare = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'o', 'p', 'q', 'r', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Assert.IsTrue(Enumerable.SequenceEqual(an.GetAllowed(), compare));

			an.bannedLetters = new List<char> { 's', 'i', 'n', 'r', 'p' };
			compare = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'o', 'q', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Assert.IsTrue(Enumerable.SequenceEqual(an.GetAllowed(), compare));

			an.bannedLetters = new List<char> { 's', 'i', 'n', 'r', 'p', 'd', 'h' };
			compare = new List<char> { 'a', 'b', 'c', 'e', 'f', 'g', 'j', 'k', 'l', 'm', 'o', 'q', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Assert.IsTrue(Enumerable.SequenceEqual(an.GetAllowed(), compare));

			an.bannedLetters = new List<char> { 's', 'r' };
			compare = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Assert.IsTrue(Enumerable.SequenceEqual(an.GetAllowed(), compare));
			an.Dispose();
		}

		[TestMethod]
		public void ReturnOldResults()
		{
			Analyzer an = new Analyzer(5);

			var run1 = an.Run(new List<(char, byte)> { ('s', 2), ('a', 0), ('i', 2), ('n', 2), ('t', 2) });
			var run2 = an.Run(null);

			Assert.AreEqual(run1.Item2.Count, run2.Item2.Count);

			an.Dispose();
		}

		[TestMethod]
		public void RemReqPosTest()
		{
			Analyzer an = new Analyzer(5);
			an.reqPos = new List<(char, byte)> { ('e', 1), ('a', 2), ('t', 3) };
			Assert.AreEqual(15898, an.RemReqPos());
			an.Dispose();
		}


		[TestMethod]
		public void RemReqNPosTest()
		{
			Analyzer an = new Analyzer(5);
			an.reqLetters = new List<char> { 'a', 't', 'e' };
			Assert.AreEqual(15429, an.RemReqNPos());
			an.Dispose();
		}

		[TestMethod]
		public void RemBannedTest()
		{
			Analyzer an = new Analyzer(5);
			an.bannedLetters = new List<char> { 's', 'i', 'n', 'r', 'p', 'd', 'h' };
			Assert.AreEqual(14470, an.RemBanned());
			an.Dispose();
		}

		[TestMethod]
		public void RemInvalTest()
		{
			Analyzer an = new Analyzer(5);
			an.bannedPos = new List<(char, byte)> { ('t', 0), ('a', 1), ('t', 4), ('e', 4) };
			Assert.AreEqual(6045, an.RemInvalPos());
			an.Dispose();

			an = new Analyzer(5);
			an.bannedPos = new List<(char, byte)> { ('a', 1), ('e', 3), ('e', 4) };
			Assert.AreEqual(6362, an.RemInvalPos());
			an.Dispose();
		}

		[TestMethod]
		public void ComputePosTest()
		{
			Analyzer an = new Analyzer(5);
			List<List<(char letter, int num)>> results = an.ComputePos(new List<char> { 'a', 'e' });
			
			//e
			Assert.AreEqual(421, results[0][1].num);
			Assert.AreEqual(1971, results[1][1].num);
			Assert.AreEqual(1027, results[2][1].num);
			Assert.AreEqual(2510, results[3][0].num);
			Assert.AreEqual(1873, results[4][0].num);

			//a
			Assert.AreEqual(1174, results[0][0].num);
			Assert.AreEqual(2871, results[1][0].num);
			Assert.AreEqual(1481, results[2][0].num);
			Assert.AreEqual(1585, results[3][1].num);
			Assert.AreEqual(1282, results[4][1].num);
			an.Dispose();
		}

		[TestMethod]
		public void ComputeIncTest()
		{
			Analyzer an = new Analyzer(5);
			List<(char letter, int num)> results = an.ComputeInc(new List<char> { 'a', 'e' });

			Assert.AreEqual(7248, results[0].num);	//a
			Assert.AreEqual(6730, results[1].num);	//e
			an.Dispose();
		}

		[TestMethod]
		public void TestConnectionTest()
		{
			Analyzer an = new Analyzer(5);
			Assert.IsTrue(an.TestConnection());

			Assert.ThrowsException<Exception>(() => new Analyzer(5, "server=DESKTOP-SV6S000;trusted_connection=Yes"));

			Assert.ThrowsException<ArgumentException>(() => new Analyzer(5, "NotAConnection"));

			Assert.ThrowsException<Exception>(() => new Analyzer(5, an.connectionString, "NotATable", "words"));

			Assert.ThrowsException<Exception>(() => new Analyzer(5, an.connectionString, "english.dbo.words", "NotAColumn"));
			an.Dispose();
		}

		[TestMethod]
		public void UvulaTest()
		{
			Analyzer analyzer = new Analyzer(5);
			List<(char, byte)> word1 = new List<(char, byte)> { ('s', 0), ('a', 1), ('i', 0), ('n', 0), ('t', 0) };
			List<(char, byte)> word2 = new List<(char, byte)> { ('f', 0), ('l', 1), ('o', 0), ('u', 1), ('r', 0) };
			List<(char, byte)> word3 = new List<(char, byte)> { ('b', 0), ('u', 1), ('l', 0), ('l', 2), ('a', 2) };
			analyzer.Run(word1);
			analyzer.Run(word2);
			analyzer.Run(word3);

			analyzer.Dispose();
		}
	}
}
