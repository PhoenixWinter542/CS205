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
			(List<List<(char, int)>>, List<(char, int)>) results = analyzer.Run(null);
			Search search = new Search(results);
			Console.WriteLine(search.Run());

			List<(char, byte)> feedback = new List<(char, byte)> { ('s', 0), ('a', 1), ('n', 0), ('e', 1), ('s', 0) };
			results = analyzer.Run(feedback);
			search = new Search(results);
			Console.WriteLine(search.Run());
		}
	}
}
