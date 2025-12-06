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
		public void ComputePosAndSortTest()
		{
			Analyzer an = new Analyzer(5);
			List<List<(char letter, int num)>> results = an.ComputePosAndSort(new List<char> { 'a', 'e' });
			
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

		[TestMethod]
		public void GetCreateWordTableCommandTest()
		{
			Analyzer analyzer = new Analyzer(5);

			string expected =
				"\nDROP TABLE IF EXISTS editWordTable;\n" + 
				
				"\nSELECT words INTO editWordTable" +
				"\nFROM english.dbo.words;\n" +
				
				"\nALTER TABLE editWordTable" +
				"\nADD Score int NOT NULL DEFAULT(0);\n";

			string result = analyzer.GetCreateWordTableCommand();

			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void GetCreateLetterTableCommandTest()
		{
			List<char> alphabet = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'o', 'p', 'q', 'r', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Analyzer analyzer = new Analyzer(5);


			string expected =
				"\nIF NOT EXISTS (" +
				"\n\tSELECT *" +
				"\n\tFROM INFORMATION_SCHEMA.TABLES" +
				"\n\tWHERE TABLE_NAME = N'editLetterTable'" +
				"\n)" +
				"\nBEGIN" +
				"\nCREATE TABLE editLetterTable (" +
				"\n\tLetter varchar(1)," +
				"\n\tScoreInc int," +
				"\n\tScorePos0 int," +
				"\n\tScorePos1 int," +
				"\n\tScorePos2 int," +
				"\n\tScorePos3 int," +
				"\n\tScorePos4 int" +
				"\n)" +
				"\nEND;\n" +
				"\nTRUNCATE TABLE editLetterTable;\n" +
				"\nINSERT INTO editLetterTable" +
				"\nVALUES" +
				"\n\t('a', 7248, 1174, 2871, 1481, 1585, 1282)," +
				"\n\t('b', 1937, 1141, 109, 446, 297, 97)," +
				"\n\t('c', 2588, 1196, 254, 531, 542, 221)," +
				"\n\t('d', 2641, 801, 136, 514, 545, 817)," +
				"\n\t('e', 6730, 421, 1971, 1027, 2510, 1873)," +
				"\n\t('f', 1115, 684, 40, 198, 215, 101)," +
				"\n\t('g', 1867, 737, 102, 461, 477, 194)," +
				"\n\t('h', 2223, 571, 720, 208, 288, 497)," +
				"\n\t('j', 372, 260, 19, 57, 38, 2)," +
				"\n\t('k', 1663, 473, 101, 309, 484, 376)," +
				"\n\t('l', 3924, 679, 866, 1061, 923, 718)," +
				"\n\t('m', 2361, 849, 233, 649, 466, 297)," +
				"\n\t('o', 4613, 334, 2281, 1154, 903, 547)," +
				"\n\t('p', 2148, 944, 283, 434, 424, 214)," +
				"\n\t('q', 139, 85, 21, 27, 3, 3)," +
				"\n\t('r', 4865, 681, 1151, 1545, 872, 895)," +
				"\n\t('t', 3866, 981, 316, 783, 1019, 1090)," +
				"\n\t('u', 3241, 328, 1403, 787, 686, 157)," +
				"\n\t('v', 853, 287, 81, 287, 200, 23)," +
				"\n\t('w', 1160, 468, 174, 276, 159, 94)," +
				"\n\t('x', 357, 27, 74, 126, 18, 116)," +
				"\n\t('y', 2477, 167, 279, 229, 161, 1686)," +
				"\n\t('z', 435, 112, 36, 143, 129, 54)" +
				"\n;\n";

			analyzer.CreateWordTable();
			string result = analyzer.GetCreateLetterTableCommand(alphabet, "editLetterTable", "editWordTable");

			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void GetProcessWordTableCommandTest()
		{
			List<char> alphabet = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'o', 'p', 'q', 'r', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Analyzer analyzer = new Analyzer(5);

			string expected =  "";

			analyzer.CreateWordTable();
			analyzer.CreateLetterTable(alphabet, "editLetterTable", "editWordTable");

			string result = analyzer.GetCreateWordTableCommand() + analyzer.GetCreateLetterTableCommand(alphabet, "editLetterTable", "editWordTable") + analyzer.GetProcessWordTableCommand("editLetterTable");

			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void CreateWordTableTest()
		{
			Analyzer analyzer = new Analyzer(5);
			analyzer.CreateWordTable();

			string query =
				"SELECT count(*)\n" +
				"FROM editWordTable\n" +
				"WHERE NOT Score = 0;";
			SqlDataReader reader = analyzer.getReader(query);
			reader.Read();
			int result = reader.GetInt32(0);
			reader.Close();
			Assert.AreEqual(0, result);

			analyzer.Dispose();
		}

		[TestMethod]
		public void WIPCreateLetterTableTest()
		{
			//Not Implemented currently, the query works when run
		}

		[TestMethod]
		public void CreateLetterTableTest()
		{
			List<char> alphabet = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'o', 'p', 'q', 'r', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			Analyzer analyzer = new Analyzer(5);
			string query =
				"SELECT count(*)" +
				"FROM words" +
				"WHERE words LIKE '%a%';";
			SqlDataReader reader = analyzer.getReader(query);
			reader.Read();
			int expected = reader.GetInt32(0);
			reader.Close();

			analyzer.CreateLetterTable(alphabet, "words", "words");

			query =
				"SELECT Score" +
				"FROM editWordTable" +
				"WHERE editWordTable.Letter = 'a';";
			reader = analyzer.getReader(query);
			reader.Read();
			int result = reader.GetInt32(0);
			reader.Close();
			Assert.AreEqual(expected, result);

			analyzer.Dispose();
		}

		[TestMethod]
		public void ProcessWordTableTest()
		{
			Analyzer analyzer = new Analyzer(5);
			string query =
				"SELECT count(*)" +
				"FROM words" +
				"WHERE words LIKE '%a%';";
			SqlDataReader reader = analyzer.getReader(query);
			reader.Read();
			int expected = reader.GetInt32(0);
			reader.Close();

			analyzer.ProcessWordTable("words");

			query =
				"SELECT Score" +
				"FROM editWordTable" +
				"WHERE editWordTable.Letter = 'a';";
			reader = analyzer.getReader(query);
			reader.Read();
			int result = reader.GetInt32(0);
			reader.Close();
			Assert.AreEqual(expected, result);

			analyzer.Dispose();
		}
	}
}
