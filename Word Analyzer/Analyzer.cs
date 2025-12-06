using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Configuration;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using System.Data.Common;
using System.Net.Mail;

/*
		 * SQL calculation steps
		 
			* startup
				
				* check for word table, fail if not present
				* Create letter table if doesn't exist
				* compute letter table for full word set if doesn't exist
				* compute word scores for full word set if doesn't exist
				* clone word table
		  

			* state info
			
				* Delete state table if exists
				* Create state table
				* Create letter table
				* compute letter table for current word set 
				* compute word scores for full word set
		 

			* cleanup
			
				* delete current word set word table
				* delete current word set letter table
		 

		 * Subdetails
		 
			* letter tables
			
				* column for # of words with letter included
				* column for each position in the word for # of words with this letter in this position


			* word tables
			
				* column for words
				* column for overall word score


			* compute letter table
			
				* compute letters included

					* count(*) WHERE letter IN words


				* compute letter position included
				
					* count(*) WHERE word LIKE [any][letter in position][any]
			
			* compute word scores
			
			
				* Create tmp table with columns letter and score
				* initialize all to 0
					
					
					* Get score for letters in word
						
						* WHERE letter IN word, word.score += letterscore * scoreweight		//scoreweight allows us to prioritize the different scoring elements (included, included at pos, etc) differently
						
							* get the letterscores for the current word
							
								* Sort tmp by score
								* score *= scoreweight		//allows us to prioritize the second most common letter differently than the third most common etc.
									
									* create the list of scores
									
										* TRUNCATE TABLE tmp	//clear data from tmp
										* WHILE < length of words  //constant
										
											* add SELECT letter, letterscore FROM letterscore WHERE currentword[i] == letter
						
					* Get score for letters in word at given position 
					
						WHERE word[pos] == letter, word.score += letterScore * scoreweight
				
				* delete tmp table

*/

namespace Word_Analyzer
{
	public struct Weights
	{
		/// <summary>
		/// These weights will be multiplied with the letterInc scores in descending order of the letter scores
		/// eg. incWeights[0] will apply to the letter with the highest score, incWeights[1] will apply to the letter with the second highest score, and so on
		/// if a word contains a letter twice, this letter will be included in the scores twice and take up two neighboring positions
		/// </summary>
		public List<double> incAny;

		/// <summary>
		/// these weights will be multiplied to the score of the letter in the relevant position
		/// </summary>
		public List<double> incPos;
	}

	public class Analyzer : IDisposable
	{

		public readonly string connectionString;
		public readonly string tableString;
		public readonly string columnString;
		public readonly string stateWordTable;
		public readonly string stateLetterTable;
		SqlConnection conn;
		SqlTransaction transaction;
		SqlCommand cmd;
		byte length;
		Weights weights;

		public readonly List<char> fullAlphabet;
		public List<char> bannedLetters;
		public List<char> reqLetters;
		public List<(char, byte)> bannedPos;
		public List<(char letter, byte pos)> reqPos;

		//Added for tests to use, no internal purpose
		public SqlDataReader getReader(string query)
		{
			cmd.CommandText = query;
			return cmd.ExecuteReader();
		}

		public Analyzer(byte length) : this(length, ConfigurationManager.ConnectionStrings["connection"].ConnectionString) { }

		public Analyzer(byte length, string connection) : this(length, connection, ConfigurationManager.ConnectionStrings["table"].ConnectionString, ConfigurationManager.ConnectionStrings["column"].ConnectionString) { }
		
		public Analyzer(byte length, string connection, string table, string column) : this(length, connection, table, column, ConfigurationManager.ConnectionStrings["tmpTablesRootName"].ConnectionString) { }

		public Analyzer(byte length, string connection, string table, string column, string tmpRootName) : this(length, connection, table, column, tmpRootName, new Weights()) { }

		public Analyzer(byte length, string connection, string table, string column, string tmpRootName, Weights weights)
		{
			this.length = length;
			connectionString = connection;
			tableString = table;
			columnString = column;
			stateWordTable = tmpRootName + "WordTable";
			stateLetterTable = tmpRootName + "LetterTable";

			conn = new SqlConnection(connectionString);
			if (false == StartConnection() || false == TestConnection())
				throw (new Exception("Connection Failed"));
			fullAlphabet = new List<char>() { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			bannedLetters = new List<char>();
			bannedPos = new List<(char, byte)>();
			reqPos = new List<(char, byte)>();
			reqLetters = new List<char>();

			if (null == weights.incAny)
				weights.incAny = new List<double>();
			if(null == weights.incPos)
				weights.incPos = new List<double>();

			if (weights.incAny.Count < length)
			{
				weights.incAny.Clear();
				for(int i = 0; i < length; i++)
				{
					weights.incAny.Add(0);
				}
			}
			if (weights.incPos.Count < length)
			{
				weights.incPos.Clear();
				for (int i = 0; i < length; i++)
				{
					weights.incPos.Add(0);
				}
			}
			this.weights = weights;

			//CreateLetterTable(fullAlphabet, tableString);
			//ProcessWordTable(tableString);
			//CreateWordTable();
		}

		private int GetIntFromCommand(int pos)
		{
			SqlDataReader reader = cmd.ExecuteReader();
			reader.Read();
			int result = reader.GetInt32(pos);
			reader.Close();
			return result;
		}


		//Sql Command Generators

		private string GetRemReqPosCommand(byte charPos, char letter)
		{
			string command = 
				"\nDELETE FROM " + tableString + 
				"\nWHERE " + columnString + " NOT LIKE " + CreateRegex(charPos, letter) + ";\n";
			return command;
		}

		private string GetRemReqNPosCommand(char letter)
		{
			string command =
				"\nDELETE FROM " + tableString + 
				"\nWHERE " + columnString + " NOT LIKE '%" + letter + "%';\n";
			return command;
		}

		private string GetRemBannedCommand(char letter)
		{
			//Find any required positions
			var reqs = reqPos.Where(x => x.letter == letter).ToList();
			reqs.Sort((x, y) => x.pos.CompareTo(y.pos));
			int pos = 0;
			string regex = "";
			for (int i = 0; i < length; i++)
			{
				if (pos >= reqs.Count || i != reqs[pos].pos)
				{
					regex += "[^" + letter + "]";
				}
				else
				{
					regex += letter;
					pos++;
				}
			}
			string command =
				"\nDELETE FROM " + tableString + 
				"\nWHERE " + columnString + " NOT LIKE '" + regex + "';\n";
			return command;
		}

		private string GetRemInvalPosCommand(byte charPos, char letter)
		{
			string command =
				"\nDELETE FROM " + tableString + 
				"\nWHERE " + columnString + " LIKE " + CreateRegex(charPos, letter) + ";\n";
			return command;
		}

		private string GetComputePosCommand(byte charPos, char letter, string table)
		{
			string command = 
				"\nSELECT count(*)" +
				"\nFROM " + table + 
				"\nWHERE " + columnString + " like " + CreateRegex(charPos, letter) + ";\n";
			return command;
		}

		private string GetComputeIncCommand(char letter, string table)
		{
			string command =
				"\nSELECT count(*)" +
				"\nFROM " + table + 
				"\nWHERE " + columnString + " like '%" + letter + "%';\n";
			return command;
		}


		//General Functions

		public bool StartConnection()
		{
			try
			{
				if (ConnectionState.Closed != conn.State)
					return true;
				conn.Open();
				transaction = conn.BeginTransaction();
				cmd = conn.CreateCommand();
				cmd.Transaction = transaction;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void EndConnection()
		{
			if (conn.State == ConnectionState.Closed)
				return;
			transaction.Rollback();
			conn.Close();
		}

		public bool TestConnection()
		{
			try
			{
				bool startedOpen = true;
				if (ConnectionState.Closed == conn.State)
				{
					startedOpen = false;
					conn.Open();
				}

				cmd.CommandText = "SELECT count(" + columnString + ") FROM " + tableString + ";";
				int result = GetIntFromCommand(0);

				if (false == startedOpen)
				{
					conn.Close();
				}
				
				if (result <= 0)
					return false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void Dispose()
		{
			try
			{
				if (null != transaction.Connection)
					transaction.Rollback();
				else
				{
					//delete the program created tables here
				}
				conn.Close();
			}
			catch { };
		}

		public string CreateRegex(int pos, char letter)
		{
			string regex = "'";

			for (int i = 0; i < pos; i++) { regex += "_"; }
			regex += letter;
			for (int i = pos + 1; i < length; i++) { regex += "_"; }

			return regex + "'";
		}

		public void AddReqLetter(char letter)
		{
			if (false == reqLetters.Contains(letter))
				reqLetters.Add(letter);
		}

		public void UpdateLetters(List<(char, byte)> feedback)
		{
			if (null == feedback)
				feedback = new List<(char, byte)>();
			for (byte i = 0; i < feedback.Count; i++)
			{
				(char letter, byte status) pos = feedback[i];
				switch (pos.status)
				{
					case 0:
						if (false == bannedLetters.Contains(pos.letter))
							bannedLetters.Add(pos.letter);
						break;
					case 1:
						if (false == bannedPos.Contains((pos.letter, i)))
						{
							bannedPos.Add((pos.letter, i));
							AddReqLetter(pos.letter);
						}
						break;
					case 2:
						if (false == reqPos.Contains((pos.letter, i)))
						{
							reqPos.Add((pos.letter, i));
							AddReqLetter(pos.letter);
						}
						break;
				}
			}
		}

		public List<char> GetAllowed()
		{
			List<char> alphabet = new List<char>();
			foreach (char letter in fullAlphabet)
			{
				if (false == bannedLetters.Contains(letter))
					alphabet.Add(letter);
			}
			return alphabet;
		}

		/// <summary>
		/// Remove words without letters in required positions
		/// </summary>
		/// <returns></returns>
		public int RemReqPos()
		{
			int sum = 0;
			foreach ((char letter, byte pos) in reqPos)
			{
				cmd.CommandText = GetRemReqPosCommand(pos, letter);
				sum += cmd.ExecuteNonQuery();
			}
			return sum;
		}

		/// <summary>
		/// Remove words without required letters, no position reqs
		/// </summary>
		public int RemReqNPos()
		{
			int sum = 0;
			foreach (char letter in reqLetters)
			{
				cmd.CommandText = "DELETE FROM " + tableString + " WHERE " + columnString + " NOT LIKE '%" + letter + "%';";
				sum += cmd.ExecuteNonQuery();
			}
			return sum;
		}

		/// <summary>
		/// Remove words with banned letters
		/// </summary>
		/// <returns></returns>
		public int RemBanned()
		{
			int sum = 0;
			foreach (char letter in bannedLetters)
			{
				//Find any required positions
				var reqs = reqPos.Where(x => x.letter == letter).ToList();
				reqs.Sort((x, y) => x.pos.CompareTo(y.pos));
				int pos = 0;
				string regex = "";
				for (int i = 0; i < length; i++)
				{
					if (pos >= reqs.Count || i != reqs[pos].pos)
					{
						regex += "[^" + letter + "]";
					}
					else
					{
						regex += letter;
						pos++;
					}
				}
				cmd.CommandText = "DELETE FROM " + tableString + " WHERE " + columnString + " NOT LIKE '" + regex + "';";
				sum += cmd.ExecuteNonQuery();
			}
			return sum;
		}

		/// <summary>
		/// Remove words with letters in invalid positions
		/// </summary>
		/// <returns></returns>
		public int RemInvalPos()
		{
			int sum = 0;
			foreach ((char letter, byte pos) in bannedPos)
			{
				cmd.CommandText = "DELETE FROM " + tableString + " WHERE " + columnString + " LIKE " + CreateRegex(pos, letter) + ";";
				sum += cmd.ExecuteNonQuery();
			}
			return sum;
		}

		//Compute
		public List<List<(char, int)>> ComputePos(List<char> alphabet, string table)
		{
			List<List<(char, int)>> results = new List<List<(char, int)>>();
			for (byte i = 0; i < length; i++)
			{
				List<(char, int)> column = new List<(char, int)>();
				foreach (char letter in alphabet)
				{
					cmd.CommandText = GetComputePosCommand(i, letter, table);
					column.Add((letter, GetIntFromCommand(0)));
				}
				results.Add(column);
			}

			return results;
		}

		public List<List<(char, int)>> ComputePos(List<char> alphabet)
		{
			return ComputePos(alphabet, tableString);
		}


		public List<List<(char, int)>> ComputePosAndSort(List<char> alphabet)
		{
			List<List<(char, int)>> results = ComputePos(alphabet);
			foreach(List<(char, int)> entry in results)
			{
				entry.Sort((x, y) => y.Item2.CompareTo(x.Item2));
			}
			return results;
		}

		public List<(char, int)> ComputeInc(List<char> alphabet)
		{
			return ComputeInc(alphabet, tableString);
		}

		public List<(char, int)> ComputeInc(List<char> alphabet, string table)
		{
			List<(char, int)> results = new List<(char, int)>();
			foreach (char letter in alphabet)
			{
				cmd.CommandText = GetComputeIncCommand(letter, table);
				results.Add((letter, GetIntFromCommand(0)));
			}

			return results;
		}

		public string GetCreateWordTableCommand()
		{
			//check if state word table exists, create a copy from the main word list table if it doesn't
			//Table needs two columns, word and score
			//initialize the scores to 0
			string command =
				"\nDROP TABLE IF EXISTS " + stateWordTable + ";\n" +

				"\nSELECT " + columnString + " INTO " + stateWordTable + "" +
				"\nFROM " + tableString + ";\n" +

				"\nALTER TABLE " + stateWordTable +
				"\nADD Score int NOT NULL DEFAULT(0);\n";
			return command;
		}

		public string GetCreateLetterTableCommand(List<char> alphabet, string table, string wordTable)
		{
			string command =
				"\nIF NOT EXISTS (" +
				"\n\tSELECT *" +
				"\n\tFROM INFORMATION_SCHEMA.TABLES" +
				"\n\tWHERE TABLE_NAME = N'" + table + "'" +
				"\n)" +
				"\nBEGIN" +
				"\nCREATE TABLE " + table + " (" +
				"\n\tLetter varchar(1)," +
				"\n\tScoreInc int";
				
			for (int i = 0; i < length; i++)
			{
				command += ",\n\tScorePos" + i + " int";
			}
			command += "\n)" +
				"\nEND;\n";

			//truncate table
			command +=
				"\nTRUNCATE TABLE " + table + ";\n";

			//Calculate the letter scores
			List<List<(char, int)>> posList = ComputePos(alphabet, wordTable);
			List<(char, int)> incList = ComputeInc(alphabet, wordTable);
			command +=
				"\nINSERT INTO " + stateLetterTable +
				"\nVALUES";

			bool first = true;
			for (int i = 0; i < alphabet.Count; i++)
			{
				char letter = alphabet[i];
				int incScore = incList[i].Item2;
				if (first)
				{
					command += "\n\t('" + letter + "', " + incScore;
					first = false;
				}
				else
					command += ",\n\t('" + letter + "', " + incScore;
				for (int pos = 0; pos < length; pos++)
				{
					command += ", " + posList[pos][i].Item2;
				}

				command += ")";
			}
			command += "\n;\n";


			return command;
		}

		public string GetProcessWordTableCommand(string table)
		{
			string command =
				"ALTER TABLE";

			return command;
		}

		public void CreateWordTable()
		{
			cmd.CommandText = GetCreateWordTableCommand();
			cmd.ExecuteNonQuery();
		}

		public void CreateLetterTable(List<char> alphabet, string table, string wordTable)
		{
			cmd.CommandText = GetCreateLetterTableCommand(alphabet, table, wordTable);
			cmd.ExecuteNonQuery();
		}

		public void ProcessWordTable(string table)
		{
			cmd.CommandText = GetProcessWordTableCommand(table);
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// 0 - char not in word	|	1 - char in word, not in position	|	2 - char in word, in correct position
		/// </summary>
		/// <param name="feedback"></param>
		/// <returns></returns>
		public (List<List<(char, int)>>, List<(char, int)>) Run(List<(char, byte)> feedback)
		{
			StartConnection();

			//Create list of allowed letters
			UpdateLetters(feedback);
			List<char> alphabet = GetAllowed();


			//Remove invalid words from dataset

			//Remove words without letters in required positions
			RemReqPos();

			//Remove words without required letters, no position reqs
			RemReqNPos();

			//Remove words with banned letters
			RemBanned();

			//Remove words with letters in invalid positions
			RemInvalPos();


			//Compute
			(List<List<(char, int)>>, List<(char, int)>) results = (ComputePosAndSort(alphabet), ComputeInc(alphabet));
			EndConnection();
			return results;
		}


	}
}
