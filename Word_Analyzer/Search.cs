using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Word_Analyzer
{
	public struct Node : IComparable<Node>
	{
		public long heur;
		public List<short> offset;	//Saves the word based on positions in letterPos

		public int CompareTo(Node other)
		{
			int comp = heur.CompareTo(other.heur);
			if (comp != 0)
				return comp;

			if (offset.SequenceEqual(other.offset))
				return 0;

			return 1;	//We don't care which offset is less or greater, only if they are equal or not
		}
	}

	public class Search
	{
		public readonly string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;
		public readonly string tableString = ConfigurationManager.ConnectionStrings["table"].ConnectionString;
		public readonly string columnString = ConfigurationManager.ConnectionStrings["column"].ConnectionString;

		public List<List<(char letter, int sum)>> letterPos;	//Tracks the number of words containing each letter for each position
		public List<(char letter, int sum)> letterInc;   //Tracks the number of words containing each letter
		public int heurChoice;

		public SortedSet<Node> frontier;
		public SortedSet<Node> closed;

		public Search((List<List<(char, int)>> pos, List<(char, int)> inc) input)
		{
			heurChoice = 1;
			letterPos = input.pos;
			letterInc = input.inc;

			frontier = new SortedSet<Node>();
			closed = new SortedSet<Node>();

			Node first = new Node();
			first.offset = new List<short>();
			for (int i = 0; i < input.pos.Count; i++)
			{
				first.offset.Add(0);
			}
			Heuristic(ref first);
			frontier.Add(first);
		}
		public Search((List<List<(char, int)>> pos, List<(char, int)> inc) input, int heur) : this(input)
		{
			heurChoice = heur;
		}

		public Search((List<List<(char, int)>> pos, List<(char, int)> inc) input, string connectionString) : this(input)
		{
			this.connectionString = connectionString;
		}

		public Search((List<List<(char, int)>> pos, List<(char, int)> inc)  input, int heur, string connectionString) : this(input, heur)
		{
			this.connectionString = connectionString;
		}

		public Search((List<List<(char, int)>> pos, List<(char, int)> inc) input, string connectionString, string table, string column) : this(input, connectionString)
		{
			tableString = table;
			columnString = column;
		}

		public Search((List<List<(char, int)>> pos, List<(char, int)> inc)  input, int heur, string connectionString, string table, string column) : this(input, heur, connectionString)
		{
			tableString = table;
			columnString = column;
		}

		/// <summary>
		/// Normalizes the letter position and inclusion sums and scales them to the max value of 10,000,000 each
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public (long, long) NormalizeNode(Node node)
		{
			int scale = 10000000;

			//Scale letter position sums
			long max = 0;	//max is if every letter in the word is the most common for that position
			for (int i = 0; i < node.offset.Count; i++)
			{
				max += letterPos[i][0].sum;
			}
			long posHeur = (long)(((double)GetLetterPosHeur(node) / (double)max) * scale);


			//Scale letter inclusion sums
			max = 0;	//Max is if the word consists of the top n most common unique letters
			for (int i = 0; i < node.offset.Count; i++)
			{
				if (letterInc.Count <= i)	//If all letters have been included once, stop
					break;

				max += letterInc[i].sum;
			}

			long incHeur = (long)(((double)GetLetterIncHeur(node) / (double)max) * scale);

			return (posHeur, incHeur);
		}

		/// <summary>
		/// Sums the # of words each letter pos is in
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public long GetLetterPosHeur(Node node)
		{
			long heur = 0;
			for (int i = 0; i < node.offset.Count; i++)
			{
				heur += letterPos[i][node.offset[i]].sum;
			}
			return heur;
		}

		/// <summary>
		/// Adds the # of words each letter is in, 
		/// Duplicate letters are ignored
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public long GetLetterIncHeur(Node node)
		{
			long heur = 0;
			string used = "";
			for (int i = 0; i < node.offset.Count; i++)
			{
				char letter = letterPos[i][node.offset[i]].letter;
				if (false == used.Contains(letter))
				{
					used += letter;
					foreach ((char letterTest, int sum) in letterInc)
					{
						if (letterTest == letter)
						{
							heur += sum;
							break;
						}
					}
				}
			}
			return heur;
		}

		/// <summary>
		/// Assigns heuristic based on the global heuristic choice
		/// </summary>
		/// <param name="node"></param>
		public void Heuristic(ref Node node)
		{
			switch(heurChoice)
			{
				case 0:
					node.heur = HeurPos(node);
					break;
				default:
				case 1:
					node.heur = HeurWordsIncLetter(node);
					break;
				case 2:
					node.heur = HeurInc(node);
					break;
				case 3:
					node.heur = HeurNormalized(node);
					break;
				case 4:
					node.heur = Heur2to1(node);
					break;
				case 5:
					node.heur = Heur1to2(node);
					break;
				case 6:
					node.heur = Heur1to4(node);
					break;
				case 7:
					node.heur = Heur1to8(node);
					break;
			}
		}

		/// <summary>
		/// Only use letter position
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public long HeurPos(Node node)
		{
			return GetLetterPosHeur(node);
		}

		/// <summary>
		/// Only use letter inclusion
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public long HeurInc(Node node)
		{
			return GetLetterIncHeur(node);
		}

		public long HeurNormalized(Node node)
		{
			var heurs = NormalizeNode(node);
			return heurs.Item1 + heurs.Item2;
		}

		public long Heur2to1(Node node)
		{
			var heurs = NormalizeNode(node);
			return heurs.Item1 * 2 + heurs.Item2;
		}

		public long Heur1to2(Node node)
		{
			var heurs = NormalizeNode(node);
			return heurs.Item1 + heurs.Item2 * 2;
		}

		public long Heur1to4(Node node)
		{
			var heurs = NormalizeNode(node);
			return heurs.Item1 + heurs.Item2 * 4;
		}

		public long Heur1to8(Node node)
		{
			var heurs = NormalizeNode(node);
			return heurs.Item1 + heurs.Item2 * 8;
		}

		/// <summary>
		/// Use letter position and letter inclusion
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public long HeurWordsIncLetter(Node node)
		{
			return GetLetterPosHeur(node) + GetLetterIncHeur(node);
		}

		public bool IsWord(Node node)
		{
			string word = GetWord(node);
			string query = "SELECT * FROM " + tableString + " WHERE " + columnString + " = '" + word + "';";
			SqlConnection conn = new SqlConnection(connectionString);
			SqlCommand cmd = new SqlCommand(query, conn);

			conn.Open();
			SqlDataReader reader = cmd.ExecuteReader();
			reader.Read();
			bool isWord = reader.HasRows;
			reader.Close();
			conn.Close();

			return isWord;
		}

		public string GetWord(Node node)
		{
			string word = "";
			short i = 0;
			foreach(short pos in node.offset)
			{
				word += letterPos[i][pos].letter;
				i++;
			}

			return word;
		}

		public void Expand(Node node)
		{
			for(int i = 0; i < node.offset.Count; i++)
			{
				Node newNode = new Node();
				newNode.offset = node.offset.ToList();
				newNode.offset[i]++;
				if (letterPos[i].Count > newNode.offset[i])
				{
					Heuristic(ref newNode);
					if (false == closed.Contains(newNode) && false == frontier.Contains(newNode))
						frontier.Add(newNode);
				}
			}
		}

		public string Run()
		{
			while (true)
			{
				if(false == frontier.Any())
				{
					return null;
				}
				if(true == IsWord(frontier.Max()))
				{
					return GetWord(frontier.Max());
				}
				else
				{
					Node max = frontier.Max();
					closed.Add(max);
					frontier.Remove(max);
					Expand(max);
				}
			}
		}

	}
}
