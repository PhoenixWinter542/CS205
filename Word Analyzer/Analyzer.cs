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

namespace Word_Analyzer
{
	public class Analyzer : IDisposable
	{
		public readonly string connectionString;
		public readonly string tableString;
		public readonly string columnString;
		SqlConnection conn;
		SqlTransaction transaction;
		SqlCommand cmd;
		byte length;

		public readonly List<char> fullAlphabet;
		public List<char> bannedLetters;
		public List<char> reqLetters;
		public List<(char, byte)> bannedPos;
		public List<(char letter, byte pos)> reqPos;

		public Analyzer(byte length) : this(length, ConfigurationManager.ConnectionStrings["connection"].ConnectionString) { }

		public Analyzer(byte length, string connection) : this(length, connection, ConfigurationManager.ConnectionStrings["table"].ConnectionString, ConfigurationManager.ConnectionStrings["column"].ConnectionString) { }

		public Analyzer(byte length, string connection, string table, string column)
		{
			this.length = length;
			connectionString= connection;
			tableString = table;
			columnString = column;

			conn = new SqlConnection(connectionString);
			if (false == startConnection() || false == TestConnection())
				throw (new Exception("Connection Failed"));
			fullAlphabet = new List<char>() { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
			bannedLetters = new List<char>();
			bannedPos = new List<(char, byte)>();
			reqPos = new List<(char, byte)>();
			reqLetters = new List<char>();
		}

		//Sql Command Generators

		private string getRemReqPosCommand(byte charPos, char letter)
		{
			return "DELETE FROM " + tableString + " WHERE " + columnString + " NOT LIKE " + CreateRegex(charPos, letter) + ";";
		}

		private string getRemReqNPosCommand(char letter)
		{
			return "DELETE FROM " + tableString + " WHERE " + columnString + " NOT LIKE '%" + letter + "%';";
		}

		private string getRemBannedCommand(char letter)
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
			return "DELETE FROM " + tableString + " WHERE " + columnString + " NOT LIKE '" + regex + "';";
		}

		private string getRemInvalPosCommand(byte charPos, char letter)
		{
			return "DELETE FROM " + tableString + " WHERE " + columnString + " LIKE " + CreateRegex(charPos, letter) + ";";
		}

		private string getComputePosCommand(byte charPos, char letter)
		{
			return "SELECT count(*) FROM " + tableString + " WHERE " + columnString + " like " + CreateRegex(charPos, letter) + ";";
		}

		private string getComputeIncCommand(char letter)
		{
			return  "SELECT count(*) FROM " + tableString + " WHERE " + columnString + " like '%" + letter + "%';";
		}


		//General Functions

		public bool startConnection()
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

		public void endConnection()
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
				SqlDataReader reader = cmd.ExecuteReader();
				reader.Read();
				int result = reader.GetInt32(0);
				reader.Close();

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
				cmd.CommandText = getRemReqPosCommand(pos, letter);
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

		///Compute
		public List<List<(char, int)>> ComputePos(List<char> alphabet)
		{
			List<List<(char, int)>> results = new List<List<(char, int)>>();
			for (byte i = 0; i < length; i++)
			{
				List<(char, int)> column = new List<(char, int)>();
				foreach (char letter in alphabet)
				{
					cmd.CommandText = getComputePosCommand(i, letter);
					SqlDataReader reader = cmd.ExecuteReader();
					reader.Read();
					column.Add((letter, reader.GetInt32(0)));
					reader.Close();
				}
				column.Sort((x, y) => y.Item2.CompareTo(x.Item2));
				results.Add(column);
			}

			return results;
		}

		public List<(char, int)> ComputeInc(List<char> alphabet)
		{
			List<(char, int)> results = new List<(char, int)>();
			foreach (char letter in alphabet)
			{
				cmd.CommandText = getComputeIncCommand(letter);
				SqlDataReader reader = cmd.ExecuteReader();
				reader.Read();
				results.Add((letter, reader.GetInt32(0)));
				reader.Close();
			}

			return results;
		}

		public bool ProcessTable(string table, List<char> alphabet)
		{
			try
			{
				foreach (char letter in fullAlphabet)
				{

				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 0 - char not in word	|	1 - char in word, not in position	|	2 - char in word, in correct position
		/// </summary>
		/// <param name="feedback"></param>
		/// <returns></returns>
		public (List<List<(char, int)>>, List<(char, int)>) Run(List<(char, byte)> feedback)
		{
			startConnection();

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
			(List<List<(char, int)>>, List<(char, int)>) results = (ComputePos(alphabet), ComputeInc(alphabet));
			endConnection();
			return results;
		}


	}
}
