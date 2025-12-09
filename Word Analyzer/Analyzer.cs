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
using System.Collections.ObjectModel;

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
		public List<double> incWeights;

		/// <summary>
		/// these weights will be multiplied to the score of the letter in the relevant position
		/// </summary>
		public List<double> posWeights;

		public List<double> baseIncWeights;

		public List<double> basePosWeights;
	}

	public class Analyzer : IDisposable
	{

		public readonly string connectionString;
		public readonly string tableString;
		public readonly string columnString;
		public readonly string stateWordTable;
		public readonly string stateLetterTable;
		public readonly string rootName;
		SqlConnection conn;
		SqlTransaction transaction;
		SqlCommand cmd;
		byte length;
		Weights weights;
		public string bigQuery = "";

		public readonly List<char> fullAlphabet;
		public List<char> bannedLetters;
		public List<char> reqLetters;
		public List<(char, byte)> bannedPos;
		public List<(char letter, byte pos)> reqPos;

		//Added for tests to use, no internal purpose
		public SqlDataReader getReader(string query)
		{
			cmd.CommandText = query;
			bigQuery += cmd.CommandText;
			return cmd.ExecuteReader();
		}


		private void ClearLetterReqs()
		{
			bannedLetters.Clear();
			bannedPos.Clear();
			reqPos.Clear();
			reqLetters.Clear();
		}

		public Analyzer(byte length) : this(length, ConfigurationManager.ConnectionStrings["connection"].ConnectionString) { }

		public Analyzer(byte length, Weights weight) : this(length, ConfigurationManager.ConnectionStrings["connection"].ConnectionString, ConfigurationManager.ConnectionStrings["table"].ConnectionString, ConfigurationManager.ConnectionStrings["column"].ConnectionString, ConfigurationManager.ConnectionStrings["tmpTablesRootName"].ConnectionString, weight) { }

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
			rootName = tmpRootName;

			conn = new SqlConnection(connectionString);
			if (false == StartConnection() || false == TestConnection())
				throw (new Exception("Connection Failed"));
			fullAlphabet = new List<char>() { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			bannedLetters = new List<char>();
			bannedPos = new List<(char, byte)>();
			reqPos = new List<(char, byte)>();
			reqLetters = new List<char>();

			if (null == weights.incWeights)
				weights.incWeights = new List<double>();
			if(null == weights.posWeights)
				weights.posWeights = new List<double>();
			if (null == weights.baseIncWeights)
				weights.baseIncWeights = new List<double>();
			if (null == weights.basePosWeights)
				weights.basePosWeights = new List<double>();

			if (weights.incWeights.Count < length)
			{
				weights.incWeights.Clear();
				for(int i = 0; i < length; i++)
				{
					weights.incWeights.Add(1);
				}
			}
			if (weights.posWeights.Count < length)
			{
				weights.posWeights.Clear();
				for (int i = 0; i < length; i++)
				{
					weights.posWeights.Add(1);
				}
			}
			if (weights.baseIncWeights.Count < length)
			{
				weights.baseIncWeights.Clear();
				for (int i = 0; i < length; i++)
				{
					weights.baseIncWeights.Add(1);
				}
			}
			if (weights.basePosWeights.Count < length)
			{
				weights.basePosWeights.Clear();
				for (int i = 0; i < length; i++)
				{
					weights.basePosWeights.Add(1);
				}
			}
			this.weights = weights;

			cmd.CommandText = GetDropsCommand(stateWordTable) + GetStartBatch(weights.incWeights, weights.posWeights, stateWordTable, fullAlphabet);
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();

			cmd.CommandText = GetProcessLettersCommand(stateWordTable);
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();

			cmd.CommandText = GetProcessWordCommand();
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();

			cmd.CommandText = GetProcessScoresCommand(stateWordTable);
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();
		}

		private int GetIntFromCommand(int pos)
		{
			bigQuery += cmd.CommandText;
			SqlDataReader reader = cmd.ExecuteReader();
			reader.Read();
			int result = reader.GetInt32(pos);
			reader.Close();
			return result;
		}

		public void ChangeWeights(Weights weights)
		{
			this.weights = weights;
		}


		//Sql Command Generators

		public string TruncateIfExists(string table)
		{
			return "\nIF OBJECT_ID('" + table + "', 'U') IS NOT NULL" +
				"\nBEGIN" +
				"\n\tTRUNCATE TABLE " + table +
				"\nEND;\n";
		}

		public string GetProcessLettersCommand(string wordTable)
		{
			string command = 
				"\nCREATE VIEW " + rootName + "LetterView" +
				"\nAS" +
				"\nSELECT" +
				"\n\tletter," +
				"\n\tCOUNT(*) AS ScoreInc,";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ",";
				command += "\n\tCOUNT(CASE WHEN SUBSTRING(word, " + (i + 1) + ", 1) = letter THEN 1 END) AS ScorePos" + i;
			}
			command +=
				"\nFROM " + wordTable + ", " + rootName + "Alphabet" +
				"\nWHERE word LIKE '%' + letter + '%'" +
				"\nGROUP BY letter;\n";

			return command;
		}

		public string GetUpdateAlphabetCommand(List<char> alphabet)
		{
			string command =
				"\nDELETE FROM " + rootName + "Alphabet WHERE NOT letter IN (";
			bool first = true;
			foreach(char letter in alphabet)
			{
				if (first)
					first = false;
				else
					command += ", ";
				command += "'" + letter + "'";
			}
			command += ");\n";
			return command;
		}

		public string GetProcessWordCommand()
		{
			string command =
				"\nCREATE FUNCTION " + rootName + "ProcessWord(";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ", ";
				command += "@char" + i + " varchar(1)";
			}
			command +=
				")" +
				"\nRETURNS TABLE" +
				"\nAS" +
				"\nRETURN" +
				"\n(" +
				"\n\tSELECT ScoreInc AS score,";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += " +";
				command += "\n\t\tCASE WHEN @char" + i + " = a.letter THEN ScorePos" + i + " * (SELECT posWeights FROM " + rootName + "Weights ORDER BY pos OFFSET " + i + " ROWS FETCH NEXT 1 ROWS ONLY) ELSE 0 END";
			}
			command +=
				"\n\t\tAS PosScore" +
				"\n\tFROM" +
				"\n\t\t(VALUES ";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ", ";
				command += "(@char" + i + ")";
			}
			command += ") AS a(letter)" +
				"\n\t\t\tINNER JOIN" +
				"\n\t\teditLetterView AS b" +
				"\n\tON a.letter = b.letter\n)";

			return command;
		}

		public string GetProcessScoresCommand(string wordTable)
		{
			string command =
				"\nCREATE FUNCTION " + rootName + "ProcessScores(";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ", ";
				command += "@char" + i + " varchar(1)";
			}
			command +=
				")" +
				"\nRETURNS TABLE" +
				"\nAS" +
				"\nRETURN" +
				"\n(" +
				"\n\tSELECT " + rootName + "Weights.incWeights * l.score + PosScore AS score" +
				"\n\tFROM " + rootName + "Weights, (SELECT ROW_NUMBER() OVER (PARTITION BY score ORDER BY score DESC)  AS pos, score, PosScore FROM dbo." + rootName + "ProcessWord(";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ", ";
				command += "@char" + i;
			}
			command += ")) AS l" +
				"\n\tWHERE l.pos = " + rootName + "Weights.pos" +
				"\n)";
			return command;
		}
		public string GetUpdateScoreCommand(string wordTable)
		{
			string command =
				"\nUPDATE " + wordTable + "" +
				"\nSET Score = (SELECT SUM(score) FROM dbo." + rootName + "ProcessScores(";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ", ";
				command += "SUBSTRING(word, " + (i + 1) + ", 1)";
			}
			command += "))\nFROM " + wordTable + ";\n";

			return command;
		}

		public string GetCreateWeightsTableCommand()
		{
			return
				"\nDROP TABLE IF EXISTS " + rootName + "Weights;" +
				"\nCREATE TABLE " + rootName + "Weights(pos int, incWeights float(53), posWeights float(53));\n";
		}

		public string GetUpdateWeightsTableCommand(List<double>inc, List<double>pos)
		{
			string command =
				TruncateIfExists(rootName + "Weights") +

				"\nINSERT INTO " + rootName + "Weights" +
				"\nVALUES";
			for (int i = 0; i < length; i++)
			{
				if (i != 0)
					command += ",\n\t(";
				else
					command += "\n\t(";
				command +=
					i + ", " +
					inc[i] + ", " +
					pos[i] + ")";
			}
			command += ";\n";

			return command;
		}

		public string GetCreateWordTableCommand(string wordTable)
		{
			string command =
				"\nDROP TABLE IF EXISTS " + wordTable + ";" +
				"\nCREATE TABLE " + wordTable + "(" +
				"\n\tWord varchar(" + length + ")," +
				"\n\tScore int NOT NULL DEFAULT(0)," +
				"\n\tPRIMARY KEY(Word) WITH (IGNORE_DUP_KEY = ON)" +
				"\n);\n";

			return command;
		}

			public string GetResetWordTableCommand(string wordTable)
		{
			string command =
				"\nINSERT INTO " + wordTable + "(word)" +
				"\nSELECT " + columnString + "" +
				"\nFROM " + tableString + ";\n";

			return command;
		}

		public string GetCreateAlphabetCommand()
		{
			string command =
				"\nDROP TABLE IF EXISTS " + rootName + "Alphabet;" +
				"\nCREATE TABLE " + rootName + "Alphabet(" +
				"\n\tletter varchar(1)," +
				"\n\tPRIMARY KEY (letter) WITH (IGNORE_DUP_KEY = ON)" +
				"\n);\n";

			return command;
		}

		public string GetResetAlphabetCommand()
		{
			string command =
				"\nINSERT INTO " + rootName + "Alphabet" +
				"\nSELECT letter" +
				"\nFROM (Values";
			bool first = true;
			foreach(char letter in fullAlphabet)
			{
				if (first)
					first = false;
				else
					command += ",";
				command += "\n\t('" + letter + "')";
			}
			command += 
				") AS a(letter)" +
				"\n;\n";

			return command;
		}

		public string GetReturnsCommand(string wordTable)
		{
			string command = "\nSELECT * FROM " + wordTable + " ORDER BY score DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;\n";

			return command;
		}

		public string GetDropsCommand(string wordTable)
		{
			string command =
				"\nDROP TABLE IF EXISTS " + wordTable + ";" +
				"\nDROP VIEW IF EXISTS " + rootName + "LetterView;" +
				"\nDROP TABLE IF EXISTS " + rootName + "Weights;" +
				"\nDROP TABLE IF EXISTS " + rootName + "Alphabet;" +
				"\nDROP FUNCTION IF EXISTS " + rootName + "ProcessScores;" +
				"\nDROP FUNCTION IF EXISTS " + rootName + "ProcessWord;\n";

			return command;
		}

		public string GetStartBatch(List<double> inc, List<double> pos, string wordTable, List<char> alphabet)
		{
			return
				GetCreateWeightsTableCommand() +
				GetCreateAlphabetCommand() +
				GetCreateWordTableCommand(stateWordTable) +
				GetResetAlphabetCommand() +
				GetUpdateWeightsTableCommand(weights.incWeights, weights.posWeights) +
				GetResetWordTableCommand(stateWordTable);
		}

		public string GetBlankSlateCommand()
		{
			return
				GetUpdateWeightsTableCommand(weights.incWeights, weights.posWeights) +
				GetResetWordTableCommand(stateWordTable) +
				GetResetAlphabetCommand();
		}

		private string GetRemReqPosCommand()
		{
			string command = "";
			foreach ((char letter, byte pos) in reqPos)
			{
				command +=
				"\nDELETE FROM " + stateWordTable +
				"\nWHERE word NOT LIKE " + CreateRegex(pos, letter) + ";\n";
			}
			return command;

		}

		private string GetRemReqNPosCommand()
		{
			string command = "";
			foreach (char letter in reqLetters)
			{
				 command += "\nDELETE FROM " + stateWordTable + " WHERE word NOT LIKE '%" + letter + "%';";
			}
			return command;
		}

		private string GetRemBannedCommand()
		{
			string command = "";
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
				command += "\nDELETE FROM " + stateWordTable + " WHERE word NOT LIKE '" + regex + "';";

			}
			return command;
		}

		private string GetRemInvalPosCommand()
		{
			string command = "";
			foreach ((char letter, byte pos) in bannedPos)
			{
				command += 
					"\nDELETE FROM " + stateWordTable +
					"\nWHERE word LIKE " + CreateRegex(pos, letter) + ";\n";
			}
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

		public void Reset()
		{
			ClearLetterReqs();
			ResumeConnection();
			cmd.CommandText = GetBlankSlateCommand();
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();
			PauseConnection();
		}

		public void Reset(Weights weight)
		{
			ChangeWeights(weight);
			Reset();
		}

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

		//Don't deal with transactions
		public void ResumeConnection()
		{
			if (conn.State != ConnectionState.Closed)
				return;
			conn.Open();
			cmd = conn.CreateCommand();
		}

		public void PauseConnection()
		{
			if (conn.State == ConnectionState.Closed)
				return;
			if(null != cmd.Transaction)
				transaction.Commit();
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
			cmd.CommandText = GetRemReqPosCommand();
			if (cmd.CommandText != "")
			{
				bigQuery += cmd.CommandText;
				return cmd.ExecuteNonQuery();
			}
			else
				return 0;
		}

		/// <summary>
		/// Remove words without required letters, no position reqs
		/// </summary>
		public int RemReqNPos()
		{
			cmd.CommandText = GetRemReqNPosCommand();
			if (cmd.CommandText != "")
			{
				bigQuery += cmd.CommandText;
				return cmd.ExecuteNonQuery();
			}
			else
				return 0;
		}

		/// <summary>
		/// Remove words with banned letters
		/// </summary>
		/// <returns></returns>
		public int RemBanned()
		{
			cmd.CommandText = GetRemBannedCommand();
			if (cmd.CommandText != "")
			{
				bigQuery += cmd.CommandText;
				return cmd.ExecuteNonQuery();
			}
			else
				return 0;
		}

		/// <summary>
		/// Remove words with letters in invalid positions
		/// </summary>
		/// <returns></returns>
		public int RemInvalPos()
		{
			cmd.CommandText = GetRemInvalPosCommand();
			if (cmd.CommandText != "")
			{
				bigQuery += cmd.CommandText;
				return cmd.ExecuteNonQuery();
			}
			else
				return 0;
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

		public int GetWordCount()
		{
			cmd.CommandText = "SELECT  COUNT(*) FROM " + tableString + ";";
			return GetIntFromCommand(0);
		}

		/// <summary>
		/// 0 - char not in word	|	1 - char in word, not in position	|	2 - char in word, in correct position
		/// </summary>
		/// <param name="feedback"></param>
		/// <returns></returns>
		public (string, int) GetTopWord(List<(char, byte)> feedback)
		{
			//Create list of allowed letters
			UpdateLetters(feedback);
			List<char> alphabet = GetAllowed();

			string command = GetUpdateAlphabetCommand(alphabet);
				
			command += GetRemReqPosCommand();

			//Remove words without required letters, no position reqs
			command += GetRemReqNPosCommand();

			//Remove words with banned letters
			command += GetRemBannedCommand();

			//Remove words with letters in invalid positions
			command += GetRemInvalPosCommand();

			ResumeConnection();
			cmd.CommandText = command;
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();

			cmd.CommandText = GetUpdateScoreCommand(stateWordTable);
			bigQuery += cmd.CommandText;
			cmd.ExecuteNonQuery();

			cmd.CommandText = GetReturnsCommand(stateWordTable);
			bigQuery += cmd.CommandText;
			SqlDataReader reader = cmd.ExecuteReader();
			reader.Read();
			string word = reader.GetString(0);
			int score = reader.GetInt32(1);
			reader.Close();

			PauseConnection();

			return (word, score);
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
