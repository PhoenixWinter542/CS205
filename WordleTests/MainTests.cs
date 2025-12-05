using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Analyzer;

namespace WordleTests
{
	[TestClass]
	public class MainTests
	{
		[TestMethod]
		public void MeatyBasic()
		{
			Analyzer analyzer = new Analyzer(5);
			(List<List<(char, int)>>, List<(char, int)>) results = analyzer.Run(null);
			Search search = new Search(results, 0);
			Assert.AreEqual("sanes", search.Run());

			List<(char, byte)> feedback = new List<(char, byte)> { ('s', 0), ('a', 1), ('n', 0), ('e', 1), ('s', 0) };
			results = analyzer.Run(feedback);
			search = new Search(results, 0);
			Assert.AreEqual("areae", search.Run());

			feedback = new List<(char, byte)> { ('a', 1), ('r', 0), ('e', 1), ('a', 1), ('e', 1) };
			results = analyzer.Run(feedback);
			search = new Search(results, 0);
			Assert.AreEqual("beata", search.Run());

			feedback = new List<(char, byte)> { ('b', 0), ('e', 2), ('a', 2), ('t', 2), ('a', 1) };
			results = analyzer.Run(feedback);
			search = new Search(results, 0);
			Assert.AreEqual("meath", search.Run());

			feedback = new List<(char, byte)> { ('m', 2), ('e', 2), ('a', 2), ('t', 2), ('h', 0) };
			results = analyzer.Run(feedback);
			search = new Search(results, 0);
			Assert.AreEqual("meaty", search.Run());
		}
		[TestMethod]
		public void MeatyWordsIncLetter()
		{
			Analyzer analyzer = new Analyzer(5);
			(List<List<(char, int)>>, List<(char, int)>) results = analyzer.Run(null);
			Search search = new Search(results);
			Assert.AreEqual("cares", search.Run());

			List<(char, byte)> feedback = new List<(char, byte)> { ('c', 0), ('a', 1), ('r', 0), ('e', 1), ('s', 0) };
			results = analyzer.Run(feedback);
			search = new Search(results);
			Assert.AreEqual("alane", search.Run());

			feedback = new List<(char, byte)> { ('a', 1), ('l', 0), ('a', 2), ('n', 0), ('e', 1) };
			results = analyzer.Run(feedback);
			search = new Search(results);
			Assert.AreEqual("peaty", search.Run());

			feedback = new List<(char, byte)> { ('p', 0), ('e', 2), ('a', 2), ('t', 2), ('y', 2) };
			results = analyzer.Run(feedback);
			search = new Search(results);
			Assert.AreEqual("featy", search.Run());

			feedback = new List<(char, byte)> { ('f', 0), ('e', 2), ('a', 2), ('t', 2), ('y', 2) };
			results = analyzer.Run(feedback);
			search = new Search(results);
			Assert.AreEqual("meaty", search.Run());
		}

		[TestMethod]
		public void AnalyzerSavedTest()
		{
			Analyzer analyzer = new Analyzer(5);
			(List<List<(char, int)>>, List<(char, int)>) results = analyzer.Run(new List<(char, byte)> { ('s',2),('a',0),('i',2),('n',2),('t',2) });
			Search search = new Search(results);
			string first = search.Run();
			var result2 = analyzer.Run(null);
			search = new Search(result2);
			Assert.AreEqual(first, search.Run());
		}
	}
}
