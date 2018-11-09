using Ini;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TaskbarReader
{
    public class Tools
	{
		private static readonly string iniPath = Application.StartupPath + "\\bookmarks.ini";
		private IniFile ini = new IniFile(iniPath);
        string bookName;
		int totalLineNum;

		public Tools(Color c0, Color c1, Color c2, string s)
		{
            ThemeColor = c0;
            ForeColor = c1;
            BackColor = c2;
			Lang = s;
		}

		public string Name
		{
			set { bookName = value; }
		}

		public int LineNum
		{
			set { totalLineNum = value; }
		}

        public string Lang { get; }

        public Color ThemeColor { get; }

        public Color ForeColor { get; }

        public Color BackColor { get; }

        public Image Img
		{
			get { return TaskbarReader.Properties.Resources.clock; }
		}

        public Icon Icon
        {
            get { return TaskbarReader.Properties.Resources.clock_icon; }
        }

		public string GetString(string input)
		{
			return TaskbarReader.Properties.Resources.ResourceManager.GetString(Lang + input);
		}

		public bool WriteCurLoc(int lineNum, int offset)
		{
			try
			{
				WriteCurLocHelper(lineNum, offset);
				return true;
			}
			catch
			{
				return false;
			}
		}

        private bool WriteCurLocHelper(int lineNum, int offset)
        {
            try
            {
                ini.IniWriteValue(bookName, "Cur", lineNum + "," + offset);
                return true;
            }
            catch
            {
                return false;
            }
        }

		public Tuple<int, int> LoadCurLoc()
		{
			try
			{
				string s = ini.IniReadValue(bookName, "Cur");
                int comma_idx_1 = s.IndexOf(',');
                int lineNum = Convert.ToInt32(s.Substring(0, comma_idx_1));
                int offset = Convert.ToInt32(s.Substring(comma_idx_1 + 1));
                if (lineNum > 0 && lineNum <= totalLineNum)
				{
					return new Tuple<int, int>(lineNum, offset);
				}
				else
				{
					return new Tuple<int, int>(1, 0);
				}
			}
			catch
			{
				//return -1;		// Can't be -1 because it will make curLineNum -1.
				return new Tuple<int, int>(1, 0);
			}
		}

        public List<string> LoadHistory()
        {
            List<string> history = new List<string>();
            try
            {
                history = ini.IniGetSectionNames();
                foreach (string s in history)
                {
                    Console.WriteLine(s);
                }

                return history;
            }
            catch
            {
                return history;
            }
        }

		public bool WriteBookMark(int lineNum, int offset)
		{
			List<Tuple<int, int>> bookmarks = LoadBookMarks();

			if (bookmarks == null)
				bookmarks = new List<Tuple<int, int>>();

			if (bookmarks.Contains(new Tuple<int, int>(lineNum, offset)))
				return false;
			else
			{
				int bookmark_idx = bookmarks.Count;
				return WriteBookMarkHelper(bookmark_idx, lineNum, offset);
			}
		}

		private bool WriteBookMarkHelper(int bookmark_idx, int lineNum, int offset)
		{
			try
			{
				ini.IniWriteValue(bookName, "Loc" + bookmark_idx, lineNum + "," + offset);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public List<Tuple<int, int>> LoadBookMarks()
		{
			try
			{
				ini.IniReadValue(bookName, "Loc0");
			}
			catch
			{
				return null;
			}
			
			List<Tuple<int, int>> result = new List<Tuple<int, int>>();
			int i = 0;
			while (true)
			{
				try
				{
					string s = ini.IniReadValue(bookName, "Loc" + i.ToString());
					
					int comma_idx_1 = s.IndexOf(',');
					int lineNum = Convert.ToInt32(s.Substring(0, comma_idx_1));
					int offset = Convert.ToInt32(s.Substring(comma_idx_1 + 1));

					if (lineNum > 0 && lineNum <= totalLineNum)
					{
						result.Add(new Tuple<int, int>(lineNum, offset));
						i++;
					}
					else
					{
						DeleteBookMark(lineNum, offset, i);
					}
				}
				catch
				{
					break;
				}
			}
			return result;
		}

		public bool DeleteBookMark(int lineNum, int offset)
		{
			try
			{
				DeleteBookMarkHelper(ini.path, FindLineToDelete(lineNum, offset));
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool DeleteBookMark(int lineNum, int offset, int idx)
		{
			try
			{
				DeleteBookMarkHelper(ini.path, "Loc" + idx + "=" + lineNum + "," + offset);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void DeleteBookMarkHelper(string path, string line_to_delete)
		{
			string[] lines = File.ReadAllLines(path, Encoding.Default);
			string[] newLines = new string[lines.Count() - 1];

			int delete_idx = -1;
			bool stopModify = false;
			for (int i = 0; i < lines.Count(); i++)
			{
				string curLine = lines[i];

				if (string.Compare(curLine, line_to_delete) == 0)
				{
					delete_idx = i;
				}
				else
				{
					if (delete_idx == -1)
					{
						newLines[i] = curLine;
					}
					else if (delete_idx > -1 && i > delete_idx)
					{
						if (curLine.StartsWith("Loc") && !stopModify)
						{
							int numStart_idx = 3;
							int numEnd_idx = curLine.IndexOf("=");
							int curLocNum = Convert.ToInt32(curLine.Substring(numStart_idx, numEnd_idx - numStart_idx));
							int newLocNum = curLocNum - 1;
							newLines[i - 1] = "Loc" + newLocNum + curLine.Substring(numEnd_idx);
						}
						else
						{
							if (curLine.StartsWith("["))		// Another book
							{
								stopModify = true;
							}
							newLines[i - 1] = curLine;
						}
					}
				}
			}

			File.WriteAllLines(path, newLines, Encoding.Default);
		}

		private string FindLineToDelete(int lineNum, int offset)
		{
			List<Tuple<int, int>> bookmarks = LoadBookMarks();
			int index = bookmarks.IndexOf(new Tuple<int, int>(lineNum, offset));
			return "Loc" + index + "=" + lineNum + "," + offset;
		}

        public bool DeleteBook(string name)
        {
            try
            {
                ini.IniRemoveSection(name);
                return true;
            }
            catch
            {
                return false;
            }
        }

	}
}
