using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Word_Analyzer;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Configuration;

namespace Wordle_Helper
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		(int row, int col, int colorCol) position = (0, 0, 0);
		readonly List<List<Button>> buttons = new List<List<Button>>();
		readonly int maxRow = 6;
		readonly int maxCol = 5;
		Analyzer an;
		string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;
		string tableString = ConfigurationManager.ConnectionStrings["table"].ConnectionString;
		string columnString = ConfigurationManager.ConnectionStrings["column"].ConnectionString;
		List<Button> SubmitButtons = new List<Button>();

		Color correct = (Color)ColorConverter.ConvertFromString("#6aaa64");
		Color partial = (Color)ColorConverter.ConvertFromString("#c9b458");
		Color wrong = (Color)ColorConverter.ConvertFromString("#787c7e");

		List<List<short>> states = new List<List<short>>();

		public MainWindow()
		{
			InitializeComponent();
			for (int row = 0; row < 6; row++)
			{
				List<Button> buttonRow = new List<Button>();
				List<short> stateRow = new List<short>();
				for (int col = 0; col < 5; col++)
				{
					Button tmp = new Button();
					tmp.Background = new SolidColorBrush(Colors.Transparent);
					tmp.Content = "";
					tmp.Focusable = false;
					tmp.Margin = new Thickness(3);
					tmp.HorizontalAlignment = HorizontalAlignment.Center;
					tmp.VerticalAlignment = VerticalAlignment.Center;
					tmp.Width = 70;
					tmp.FontSize = 35;
					tmp.FontWeight = FontWeights.DemiBold;
					tmp.Height = 70;
					tmp.Name = "b" + row.ToString() + (col).ToString();
					Grid.SetRow(tmp, row);
					Grid.SetColumn(tmp, col);
					tmp.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ColorUp);
					tmp.PreviewMouseRightButtonDown += new MouseButtonEventHandler(ColorDown);
					tmp.PreviewMouseWheel += new MouseWheelEventHandler(MouseWheelChange);
					buttonRow.Add(tmp);
					Layout.Children.Add(tmp);
					stateRow.Add(0);
				}
				buttons.Add(buttonRow);
				states.Add(stateRow);
			}

			SubmitButtons.Add(b0);
			SubmitButtons.Add(b1);
			SubmitButtons.Add(b2);
			SubmitButtons.Add(b3);
			SubmitButtons.Add(b4);
			SubmitButtons.Add(b5);
			try
			{
				an = new Analyzer(5, connectionString, tableString, columnString);
			}
			catch
			{
				ConnectionFailed();
			}
		}

		private void ConnectionFailed()
		{
			var errorBox = new ConnFailMessage();
			errorBox.ShowDialog();
			BaseWindow.Close();
		}

		private void MouseWheelChange(object sender, MouseWheelEventArgs e)
		{
			if(0 < e.Delta)
			{
				ToggleButton((Button)sender, 1);
			}
			else if (0 > e.Delta)
			{
				ToggleButton((Button)sender, -1);
			}
		}

		private void UpdateButtonColor(Button btn, short state)
		{
			switch (state)
			{
				case 0:
					btn.Background = new SolidColorBrush(Colors.Transparent);
					btn.Foreground = new SolidColorBrush(Colors.Black);
					btn.BorderBrush = new SolidColorBrush(Colors.Black);
					break;
				case 1:
					btn.Background = new SolidColorBrush(correct);
					btn.Foreground = new SolidColorBrush(Colors.White);
					btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
					break;
				case 2:
					btn.Background = new SolidColorBrush(partial);
					btn.Foreground = new SolidColorBrush(Colors.White);
					btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
					break;
				case 3:
					btn.Background = new SolidColorBrush(wrong);
					btn.Foreground = new SolidColorBrush(Colors.White);
					btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
					break;
			}
		}

		private void ToggleButton(Button btn, short inc)
		{
			//Don't toggle buttons without letters
			if (false == btn.Content.ToString().Any())
				return;

			//Find button for states	|	button name is b[Row][Col]
			int row = btn.Name[1] - '0';
			int col = btn.Name[2] - '0';
			if (row != position.row)
				return;

			//Update state	|	Won't go to 0
			short tmp = (short)(states[row][col] + inc);
			if (tmp < 1)    //Ensure 1 <= tmp
				tmp = 3;
			tmp--;      //Rescale to 0 <= tmp
			tmp %= 3;   //Ensure 0 <= tmp <= 2
			tmp++;      //Rescale to 1 <= tmp <= 3
			states[row][col] = tmp;

			//Update color
			UpdateButtonColor(btn, states[row][col]);
			TestEnable();
		}

		private void ColorUp(object sender, EventArgs e)
		{
			ToggleButton((Button)sender, 1);
			RemoveColorHighlight();
		}

		private void ColorDown(object sender, EventArgs e)
		{
			ToggleButton((Button)sender, -1);
			RemoveColorHighlight();
		}

		private void MoveBack()
		{
			if (position.col > 0)
			{
				position.col--;
				if (position.colorCol >= position.col && position.colorCol > 0)  //keep colorCol behind col	|	matching at 0 is fine and handled
				{
					position.colorCol--;
				}
			}
			states[position.row][position.col] = 0;
			UpdateButtonColor(buttons[position.row][position.col], 0);
			buttons[position.row][position.col].BorderThickness = new Thickness(1);
			buttons[position.row][position.col].Content = "";
			TestEnable();
		}

		private bool StatesAssigned()
		{
			foreach (short state in states[position.row])
			{
				if (0 == state)
					return false; //At least one letter does not have a state
			}
			return true;
		}

		private void GrayOut(Button btn)
		{
			btn.Foreground = new SolidColorBrush(Colors.LightGray);
			btn.Background = new SolidColorBrush(Colors.WhiteSmoke);
			btn.BorderBrush = new SolidColorBrush(Colors.LightGray);
		}

		private void DisableSubmit()
		{
			Button btn = SubmitButtons[position.row];

			//Set text to submit
			btn.Content = "Submit";

			//Grey out current submit button
			GrayOut(btn);
		}

		private void EnableSubmit()
		{
			Button btn = SubmitButtons[position.row];

			//Set Text to submit
			btn.Content = "Submit";

			//Recolor current submit button
			btn.Foreground = new SolidColorBrush(Colors.BlanchedAlmond);
			btn.Background = new SolidColorBrush(Colors.Sienna);
			btn.BorderBrush = new SolidColorBrush(Colors.BurlyWood);
		}

		private void EnableSuggest()
		{
			Button btn = SubmitButtons[position.row];

			//Set text to suggest
			btn.Content = "Suggest";

			//Recolor current submit button
			btn.Foreground = new SolidColorBrush(Colors.LightCyan);
			btn.Background = new SolidColorBrush(Colors.CadetBlue);
			btn.BorderBrush = new SolidColorBrush(Colors.LightBlue);
		}

		private void EnableReset()
		{
			Button btn = SubmitButtons[position.row];

			//Set text to Reset
			btn.Content = "Reset";

			//Recolor current submit button
			btn.Foreground = new SolidColorBrush(Colors.PeachPuff);
			btn.Background = new SolidColorBrush(Colors.Sienna);
			btn.BorderBrush = new SolidColorBrush(Colors.SandyBrown);
		}

		private void TestEnable()
		{
			if (maxRow <= position.row)
				return;
			else if(maxRow == position.row + 1)
			{
				EnableReset();
				return;
			}

			//Determine if sumbit or suggest
			if (0 == position.col)
			{
				EnableSuggest();
			}
			else if (maxCol == position.col)
			{
				//Only enable if all letters have states
				if (false == StatesAssigned())
				{
					DisableSubmit();
					return;
				}

				EnableSubmit();
			}
			else
			{
				DisableSubmit();
			}
		}

		private void HideBorders()
		{
			for (int i = 0; i < maxCol; i++)
			{
				buttons[position.row][i].BorderBrush = new SolidColorBrush(Colors.Transparent);
			}
		}

		private void NextSubmit()
		{
			//Hide previous button
			Button btn = SubmitButtons[position.row - 1];
			btn.Visibility = Visibility.Hidden;

			//Make new button visible
			if (position.row < maxRow)
				SubmitButtons[position.row].Visibility = Visibility.Visible;

			if (position.row + 1 < maxRow)
			{
				//Make sure button is colored correctly
				TestEnable();
			}
			else if (position.row < maxRow)     //Last row doesn't need a button
			{
				GrayOut(btn);
				RunRow();
				return;
			}
		}

		private void MoveForward()
		{
			if (position.col < maxCol)
			{
				position.col++;
			}
			TestEnable();
		}

		private void NextRow()
		{
			if (position.row < maxRow)
			{
				position.row++;
				position.col = 0;
				position.colorCol = 0;
				NextSubmit();   //Needs to be run after position.row is updated
			}
		}

		private void HighlightColorPos()
		{
			if (0 < buttons[position.row][position.colorCol].Content.ToString().Count())  //Don't highlight if no letter is present
			{
				Button btn = buttons[position.row][position.colorCol];
				btn.BorderBrush = new SolidColorBrush(Colors.Black);
				btn.BorderThickness = new Thickness(2);
			}
		}

		private void RemoveColorHighlight()
		{
			if (maxRow == position.row)
				return;
			buttons[position.row][position.colorCol].BorderThickness = new Thickness(1);
			if(0 != states[position.row][position.colorCol])
			{
				buttons[position.row][position.colorCol].BorderBrush = new SolidColorBrush(Colors.Transparent);
			}
		}

		private void MoveColorMarker(int row, int prevCol)
		{
			var borderColor = new SolidColorBrush(Colors.Transparent);

			//Only hide border when button is colored
			if (0 == states[row][prevCol])
				borderColor = new SolidColorBrush(Colors.Black);

			Button btn = buttons[position.row][prevCol];
			btn.BorderBrush = borderColor;
			btn.BorderThickness = new Thickness(1);

			HighlightColorPos();
		}

		private void ColorPosLeft()
		{
			if (0 < position.colorCol)
			{
				position.colorCol--;
				MoveColorMarker(position.row, position.colorCol + 1);
			}
		}

		private void ColorPosRight()
		{
			if (position.colorCol < position.col && position.colorCol + 1 < maxCol)
			{
				if (buttons[position.row][position.colorCol + 1].Content.ToString().Any())
				{
					position.colorCol++;
					MoveColorMarker(position.row, position.colorCol - 1);
				}
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var window = Window.GetWindow(this);
			window.KeyDown += HandleKeyPress;
		}

		/// <summary>
		/// No buttons in the row can have null content
		/// </summary>
		/// <returns>The arguments for Analyzer.Run(args)</returns>
		private List<(char, byte)> GetRunArgs()
		{
			List<(char, byte)> runArgs = new List<(char, byte)>();

			int row = position.row;
			for (int col = 0; col < maxCol; col++)
			{
				byte state = 0;
				switch (states[row][col])
				{
					case 1: //Correct Pos
						state = 2;
						break;
					case 2: //Correct Letter Incorrect Pos
						state = 1;
						break;
					case 3: //Incorrect Letter
						state = 0;
						break;
					default:
						return null;
				}
				runArgs.Add((buttons[row][col].Content.ToString()[0], state));
			}

			return runArgs;
		}

		private void SessionFailed()
		{
			position.row = maxRow - 1;
			string failed = "0WORD";
			int pos = 0;
			foreach(Button btn in buttons[position.row])
			{
				btn.Background = new SolidColorBrush(Colors.Sienna);
				btn.Foreground = new SolidColorBrush(Colors.AntiqueWhite);
				btn.Content = failed[pos++];
			}
			NextRow();
		}

		private bool IsRunnable ((List<List<(char letter, int sum)>> letterPos, List<(char, int)>) results)
		{
			//Check if there are any possible words
			for (int i = 0; i < results.letterPos.Count; i++)
			{
				if (0 == results.letterPos[i][0].sum)	//If the first letter has no options, no words can be made
				{
					SessionFailed();
					return false;
				}
			}

			return true;
		}

		private void RunRow(object sender, RoutedEventArgs e)
		{
			RunRow();
		}

		private async void RunRow()
		{
			if (0 != states[position.row][position.colorCol])   //Advance colorCol if current letter has been evaluated
			{
				ColorPosRight();
			}

			if (maxCol == position.col)     //Submit word
			{
				//Ensure every letter in row has a state
				if (false == StatesAssigned())
					return;

				//Gray out submit button
				GrayOut(SubmitButtons[position.row]);
				await Task.Run(() => Task.Delay(1));
				HideBorders();

				//update wordle analyzer with row word and values
				var args = GetRunArgs();
				var result = an.Run(args);
				if (IsRunnable(result))
					NextRow();
			}
			else if (0 == position.col)     //Suggest word
			{
				//Gray out suggest button
				GrayOut(SubmitButtons[position.row]);
				await Task.Run(() => Task.Delay(1));

				//get word suggestion by running analyzer and search
				var result = an.Run(null);
				if (IsRunnable(result))
				{
					string suggestion = new Search(result, connectionString, tableString, columnString).Run();
					for (int i = 0; i < maxCol; i++)
					{
						buttons[position.row][i].Content = char.ToUpper(suggestion[i]);
					}
					position.col = maxCol;

					TestEnable();
				}
			}
			else
				return;
		}

		private void HandleKeyPress(object sender, KeyEventArgs e)
		{
			string key = e.Key.ToString();

			//If all words are entered don't run key events
			if (maxRow <= position.row + 1)
				return;

			//handle enter and backspace
			if ("Return" == key)
			{
				RunRow();
			}
			if ("Back" == key)
			{
				MoveBack();
				return;
			}
			//handle up and down arrows
			int col = position.col;
			if (col == maxCol)
				col--;
			if ("" == buttons[position.row][col].Content.ToString())
				col--;

			if("Escape" == key)
			{
				RemoveColorHighlight();
				return;
			}

			if ("Up" == key)
			{
				ToggleButton(buttons[position.row][position.colorCol], 1);
				HighlightColorPos();
				return;
			}
			if ("Down" == key)
			{
				ToggleButton(buttons[position.row][position.colorCol], -1);
				HighlightColorPos();
				return;
			}
			if("Left" == key)
			{
				ColorPosLeft();
			}
			if("Right" == key)
			{
				ColorPosRight();
			}

			if (position.col >= maxCol)
				return;

			if (key.Count() != 1)
				return;
			if (!Char.IsLetter(key[0]))
				return;

			//Don't let the user type in the last row
			if (position.row + 1 < maxRow)
			{
				buttons[position.row][position.col].Content = key;
				MoveForward();
			}
		}

		private void ResetAll(object sender, RoutedEventArgs e)
		{
			//Reset buttons
			for(int row = 0; row < states.Count; row++)
			{
				for(int col = 0; col < states[0].Count; col++)
				{
					//Reset states
					states[row][col] = 0;
					//Reset colors
					UpdateButtonColor(buttons[row][col], states[row][col]);
					//Reset borders
					buttons[row][col].BorderThickness = new Thickness(1);
					//Reset text
					buttons[row][col].Content = "";
				}
			}

			//Reset position tracking
			position = (0, 0, 0);

			//Reset Submit buttons
			foreach(Button btn in SubmitButtons)
			{
				btn.Visibility = Visibility.Collapsed;
			}
			SubmitButtons[0].Visibility = Visibility.Visible;
			TestEnable();

			//Reset wordle_analyzer
			an.Dispose();
			an = new Analyzer(5, connectionString, tableString, columnString);
		}
	}
}