using AutoUpdate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using TaskbarHook;

namespace TaskbarReader
{
    public partial class TaskbarForm : Form, AutoUpdatable
    {
        #region Variables

        private static Taskbar taskbar;
        private static int width = 0;
        private static int height = 0;

        private static FontFamily fontFamily = new FontFamily("Segoe UI");
        private static Font font = new Font(fontFamily, 8, FontStyle.Regular, GraphicsUnit.Pixel);

        #region AutoUpdate

        private AutoUpdater updater;

        public String ApplicationName
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; }
        }

        public String ApplicationID
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; }
        }

        public Tools Tools
        {
            get { return tools; }
        }

        public Assembly ApplicationAssembly
        {
            get { return Assembly.GetExecutingAssembly(); }
        }

        public Icon ApplicationIcon
        {
            get { return this.Icon; }
        }

        public Uri UpdateXmlLocation
        {
            get { return new Uri("https://raw.githubusercontent.com/henryxrl/TaskbarReader/master/TaskbarReader_Update.xml"); }
        }

        public Form Context
        {
            get { return this; }
        }

        #endregion

        #region Window-related variables

        // Global hot key
        private KeyboardHook hook = new KeyboardHook();
        [DllImport("user32.dll")]
        private static extern Boolean UnregisterHotKey(IntPtr hWnd, Int32 id);

        #endregion

        #region Tools

        private static Tools tools = null;
        private String curLineText;     // Current text shown in windows' title bar
        private String curLineText_pre;
        private String curLineText_content;

        #endregion

        #region Auto page turn

        private static Int32 aptTime = 0;
        private static Int32 timerCount;
        private static Int32 timerFlag = -1;   // 0: auto read forward; 1: auto read backward; 2: go back to current; -1: default

        #endregion

        #region TXT

        private String txt_URL;
        private String TXTURL       // Properties. Triggers when value changed
        {
            get { return this.txt_URL; }
            set
            {
                if (txt_URL == value)
                    return;
                else
                {
                    SaveCurProgess();
                    txt_URL = value;
                }
            }
        }

        private String txt_name;
        private String[] txt_book;
        private Int32 totalLineNum;     // 1-based. Total line number
        private Int32 curLineNum;       // 1-based. Need to minus 1 to get current line index
        private Int32 lineOffset;       // 0-based. character index of a line
        private Int32 lineOffset_OLD;

        #endregion

        #region Bookmark

        private List<Tuple<Int32, Int32>> bookmarks;        // 1) curLineNum; 2) lineOffset
        private Int32 bookmark_idx = 0;
        private Int32 bookmark_count;
        private Boolean isBookmarkView = false;
        private Boolean BookmarkView
        {
            get { return this.isBookmarkView; }
            set
            {
                if (isBookmarkView == value)
                    return;
                else
                {
                    isBookmarkView = value;

                    if (isBookmarkView)     // Enter bookmark view
                    {
                        prevLineNum = curLineNum;
                        prevLineOffset = lineOffset_OLD;
                    }
                    else
                    {
                        prevLineNum = -1;
                        prevLineOffset = -1;
                    }
                }
            }
        }

        private Int32 prevLineNum = -1;
        private Int32 prevLineOffset = -1;

        #endregion

        #endregion

        #region Setup Taskbar UI

        public TaskbarForm()
        {
            InitializeComponent();

            SetTools();

            taskbar = TaskBarFactory.GetTaskbar();
            width = taskbar.Rectangle.Right - taskbar.Rectangle.Left;
            height = taskbar.Rectangle.Bottom - taskbar.Rectangle.Top;
            this.Size = new Size(width, height);
            Console.WriteLine(string.Format("Width: {0}; Height: {1}", width, height));

            Color c = GetPixelColor(taskbar.Handle, 0, 0);
            this.BackColor = c;
            Console.WriteLine("Background color: " + this.BackColor);
            this.ForeColor = Color.FromArgb(c.R > 127 ? 0 : 255, c.G > 127 ? 0 : 255, c.B > 127 ? 0 : 255);
            Console.WriteLine("Text color: " + this.ForeColor);

            
            content.TextAlign = ContentAlignment.MiddleLeft;
            font = new Font(font.FontFamily, GetProperFontSize(), font.Style, font.Unit);
            content.Font = font;

            //content.Dock = DockStyle.Fill;
            content.AutoSize = false;
            content.Size = new Size(width, height);
            content.MaximumSize = new Size(width, height);
            content.MinimumSize = new Size(width, height);
            content.Location = new Point(0, 0);

            content.Text = tools.getString("openFileDialog_title");

            LaunchInTaskBar();

            BuildContextMenuStrip();

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            openFileDialog.Multiselect = false;
            openFileDialog.FilterIndex = 1;

            updater = new AutoUpdater(this);
            updater.DoUpdate(true);

            #region Register HotKeys
            // register the event that is fired after the key press.
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(Hook_KeyPressed);

            try
            {
                // ctrl+shift+q = quit
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Q);

                // ctrl+shift+right = next line
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Right);

                // ctrl+shift+left = prev line
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Left);

                // ctrl+shift+up = bookmark cur loc
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Up);

                // ctrl+shift+down = switch to bookmark view
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Down);

                // ctrl+shift+delete = delete current bookmark
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Delete);

                // ctrl+shift+space = toggle hide/show
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.Space);

                // ctrl+shift+x = toggle bookmark view
                hook.RegisterHotKey(global::ModifierKeys.Control | global::ModifierKeys.Shift, Keys.X);
            }
            catch
            {
                MessageBox.Show(tools.getString("hotkey_registration_failed"));
            }
            #endregion
        }

        private void BuildContextMenuStrip()
        {
            contextMenuStrip.Items.Clear();
            contextMenuStrip.Items.Add(tools.getString("openFile"), null, OpenFileClick);
            var historyItem = new ToolStripMenuItem(tools.getString("history"));
            List<string> history = tools.loadHistory();
            if (history == null || history.Count == 0 || (history.Count == 1 && history[0].CompareTo("") == 0))
            {
                ToolStripMenuItem historySubItem = new ToolStripMenuItem(tools.getString("history_none"));
                historySubItem.Enabled = false;
                historyItem.DropDownItems.Add(historySubItem);
            }
            else
            {
                foreach (string s in history)
                {
                    ToolStripMenuItem historySubItem = new ToolStripMenuItem(s);
                    historySubItem.Click += HistorySubItemClick;
                    historyItem.DropDownItems.Add(historySubItem);
                }

                // clear history
                historyItem.DropDownItems.Add("-");
                ToolStripMenuItem clearHistorySubItem1 = new ToolStripMenuItem(tools.getString("history_clearinvalid"));
                clearHistorySubItem1.Click += HistoryClearInvalidClick;
                historyItem.DropDownItems.Add(clearHistorySubItem1);
                historyItem.DropDownItems.Add("-");
                ToolStripMenuItem clearHistorySubItem2 = new ToolStripMenuItem(tools.getString("history_clearall"));
                clearHistorySubItem2.Click += HistoryClearAllClick;
                historyItem.DropDownItems.Add(clearHistorySubItem2);
            }
            contextMenuStrip.Items.Add(historyItem);
            var bookmarksItem = new ToolStripMenuItem(tools.getString("bookmarks"));
            bookmarks = tools.loadBookMarks();
            if (bookmarks == null || bookmarks.Count == 0)
            {
                ToolStripMenuItem bookmarksSubItem = new ToolStripMenuItem(tools.getString("bookmarks_none"));
                bookmarksSubItem.Enabled = false;
                bookmarksItem.DropDownItems.Add(bookmarksSubItem);
            }
            else
            {
                for (int i = 0; i < bookmarks.Count; ++i)
                {
                    //Console.WriteLine(getBookmarkPreview(bookmarks[i].Item1, bookmarks[i].Item2));
                    string bm = string.Format("({0}/{1}) {2}", (i + 1).ToString(), bookmarks.Count, GetBookmarkPreview(bookmarks[i].Item1, bookmarks[i].Item2));
                    ToolStripMenuItem bookmarksSubItem = new ToolStripMenuItem(bm);
                    //bookmarksSubItem.Click += (sender, EventArgs) => { BookmarksSubItemClick(sender, EventArgs, i); };
                    bookmarksSubItem.Click += BookmarksSubItemClick;
                    bookmarksItem.DropDownItems.Add(bookmarksSubItem);
                }

                // clear bookmarks
                bookmarksItem.DropDownItems.Add("-");
                ToolStripMenuItem clearBookmarksSubItem = new ToolStripMenuItem(tools.getString("bookmarks_clearall"));
                clearBookmarksSubItem.Click += BookmarksClearAllClick;
                bookmarksItem.DropDownItems.Add(clearBookmarksSubItem);
            }
            contextMenuStrip.Items.Add(bookmarksItem);
            contextMenuStrip.Items.Add(tools.getString("apt_label"), null, APTClick);
            contextMenuStrip.Items.Add("-");
            contextMenuStrip.Items.Add(tools.getString("hotkey_label"), null, HotkeysClick);
            contextMenuStrip.Items.Add(tools.getString("about_label"), null, AboutClick);
            contextMenuStrip.Items.Add("-");
            contextMenuStrip.Items.Add(tools.getString("hideshow"), null, HideshowClick);
            contextMenuStrip.Items.Add(tools.getString("exit"), null, ExitClick);
        }

        private void SetTools()
        {
            // themeColor
            Color themeColor = Color.FromArgb(40, 100, 130);
            Color foreColor = Color.FromArgb(220, 220, 220);
            Color backColor = Color.FromArgb(36, 36, 36);

            // langCode
            String langCode;
            String curLang = System.Globalization.CultureInfo.CurrentCulture.ToString();
            Boolean isChinese = curLang.Contains("zh") ? true : false;
            if (isChinese)  //in chinese
            {
                langCode = "zh_";
            }
            else    //in english
            {
                langCode = "en_";
            }

            // set tools
            tools = new Tools(themeColor, foreColor, backColor, langCode);
        }

        private async void LaunchInTaskBar()
        {
            var process = await taskbar.AddToTaskbar(System.Diagnostics.Process.GetCurrentProcess());
            process.SetPosition(0, 0);
        }

        private float GetProperFontSize()
        {
            double idealPixelSize = Math.Floor(height * 0.8f);
            int ascent = fontFamily.GetCellAscent(font.Style);
            int descent = fontFamily.GetCellDescent(font.Style);

            float idealFontSize = (float)idealPixelSize / (ascent + descent) * fontFamily.GetEmHeight(font.Style) / this.CreateGraphics().DpiX * 72f;
            Console.WriteLine("idealFontSize: " + idealFontSize);

            return idealFontSize;
        }

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr srchDc, int srcX, int srcY, int srcW, int srcH, IntPtr desthDc, int destX, int destY, int op);
        private static Color GetPixelColor(IntPtr hwnd, int x, int y)
        {
            using (Bitmap screenPixel = new Bitmap(1, 1))
            {
                using (Graphics gdest = Graphics.FromImage(screenPixel))
                {
                    using (Graphics gsrc = Graphics.FromHwnd(hwnd))
                    {
                        IntPtr hsrcdc = gsrc.GetHdc();
                        IntPtr hdc = gdest.GetHdc();
                        BitBlt(hdc, 0, 0, 1, 1, hsrcdc, x, y, (int)CopyPixelOperation.SourceCopy);
                        gdest.ReleaseHdc();
                        gsrc.ReleaseHdc();
                    }
                }

                return screenPixel.GetPixel(0, 0);
            }
        }

        private static int ShowDialog(string text, string caption)
        {
            int result = 0;
            using (Form prompt = new Form())
            {
                prompt.Width = 500;
                prompt.Height = 150;
                prompt.FormBorderStyle = FormBorderStyle.None;
                prompt.BackColor = tools.BackColor;
                prompt.ForeColor = tools.ForeColor;
                prompt.StartPosition = FormStartPosition.Manual;
                int w = Screen.GetWorkingArea(prompt).Width;
                int h = Screen.GetWorkingArea(prompt).Height;
                prompt.Location = new Point(Cursor.Position.X + prompt.Width > w ? w - prompt.Width : Cursor.Position.X, Cursor.Position.Y + prompt.Height > h ? h - prompt.Height : Cursor.Position.Y);
                prompt.Text = caption;
                prompt.ShowIcon = false;
                Label textLabel = new Label() { Left = 50, Top = 20, Text = text, AutoSize = true };
                NumericUpDown inputBox = new NumericUpDown() { Left = 50, Top = 50, Width = 360 };
                inputBox.Value = aptTime;
                Label textLabel2 = new Label() { Left = 420, Top = 50, Text = tools.getString("apt_label_sec"), AutoSize = true };
                Button confirmation = new Button() { Text = tools.getString("button_ok"), Left = 350, Width = 100, Height = 30, Top = 100 };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                confirmation.FlatStyle = FlatStyle.Flat;
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(textLabel2);
                prompt.ShowDialog();
                result = (int)inputBox.Value;
            }
            return result;
        }

        #endregion

        #region Event & Action Listener

        private void Content_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //HideShow();
            }
            else if (e.Button == MouseButtons.Middle)
            {
                OpenFile();
            }
            else if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip.Show(this, new Point(e.X, e.Y));
            }
        }

        private void Content_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            HideShow();
        }

        private void OpenFileClick(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void HistorySubItemClick(object sender, EventArgs e)
        {
            ToolStripMenuItem subItem = (ToolStripMenuItem)sender;
            TXTURL = subItem.Text;
            //MessageBox.Show(txt_URL);
            ProcessBook();
        }

        private void HistoryClearInvalidClick(object sender, EventArgs e)
        {
            List<string> history = tools.loadHistory();
            foreach (string s in history)
            {
                if (!File.Exists(s))
                {
                    tools.deleteBook(s);
                }
            }
        }

        private void HistoryClearAllClick(object sender, EventArgs e)
        {
            List<string> history = tools.loadHistory();
            foreach (string s in history)
            {
                tools.deleteBook(s);
            }
        }

        private void BookmarksSubItemClick(object sender, EventArgs e)
        {
            BookmarkView = true;

            ToolStripMenuItem subItem = (ToolStripMenuItem)sender;
            int s1 = subItem.Text.IndexOf('(') + 1;
            int e1 = subItem.Text.IndexOf('/');
            int l1 = e1 - s1;
            int s2 = e1 + 1;
            int e2 = subItem.Text.IndexOf(')');
            int l2 = e2 - s2;
            //MessageBox.Show(string.Format("{0}, {1}", subItem.Text.Substring(s1, l1), subItem.Text.Substring(s2, l2)));
            bookmark_idx = Int32.Parse(subItem.Text.Substring(s1, l1)) - 1;
            bookmark_count = Int32.Parse(subItem.Text.Substring(s2, l2));
            curLineNum = bookmarks[bookmark_idx].Item1;
            lineOffset = bookmarks[bookmark_idx].Item2;
            lineOffset_OLD = lineOffset;
            JumpToLine(2);
        }

        private void BookmarksClearAllClick(object sender, EventArgs e)
        {
            while (bookmarks.Count > 0)
            {
                DeleteBookMark(bookmarks[0].Item1, bookmarks[0].Item2);
            }
            bookmarks.Clear();
        }

        private void APTClick(object sender, EventArgs e)
        {
            //aptTime = ShowDialog(tools.getString("apt_label2"), tools.getString("apt_label2"));
            //Console.WriteLine(string.Format("Auto page turn time: {0}", aptTime));

            //if (this.Visible && aptTime > 0)
            //{
            //    timer.Enabled = true;
            //    timerCount = aptTime;
            //    timerFlag = 0;  // auto read forward
            //}

            using (APTDialog apt = new APTDialog(tools))
            {
                timerCount = -1;    // pause

                apt.StartPosition = FormStartPosition.Manual;
                int w = Screen.GetWorkingArea(apt).Width;
                int h = Screen.GetWorkingArea(apt).Height;
                apt.Location = new Point(Cursor.Position.X + apt.Width > w ? w - apt.Width : Cursor.Position.X, Cursor.Position.Y + apt.Height > h ? h - apt.Height : Cursor.Position.Y);
                apt.ShowDialog();

                aptTime = apt.apt_time;
                Console.WriteLine(string.Format("Auto page turn time: {0}", aptTime));
            }

            if (this.Visible && aptTime > 0)
            {
                timer.Enabled = true;
                timerCount = aptTime;
                timerFlag = 0;  // auto read forward
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timerCount--;

            if (timerCount == 0)
            {
                if (timerFlag == 0)
                {
                    ReadForward();
                }
                else if (timerFlag == 1)
                {
                    // why do we need to auto read backwards? Lol...
                    // readBackward();
                }
                else if (timerFlag == 2)
                {
                    ReadCurrentLine();
                }
                else    // timerFlag == -1
                {
                    MessageBox.Show(tools.getString("timer_error"));
                }
            }
        }

        private void TaskbarForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                timerCount = aptTime;    // resume
            }
            else
            {
                timerCount = -1;    // pause
            }
        }

        private void HotkeysClick(object sender, EventArgs e)
        {
            using (HotKeys hky = new HotKeys(tools))
            {
                timerCount = -1;    // pause

                hky.StartPosition = FormStartPosition.Manual;
                int w = Screen.GetWorkingArea(hky).Width;
                int h = Screen.GetWorkingArea(hky).Height;
                hky.Location = new Point(Cursor.Position.X + hky.Width > w ? w - hky.Width : Cursor.Position.X, Cursor.Position.Y + hky.Height > h ? h - hky.Height : Cursor.Position.Y);
                hky.ShowDialog();

                timerCount = aptTime;    // resume
            }
        }

        private void AboutClick(object sender, EventArgs e)
        {
            using (About abt = new About(tools))
            {
                timerCount = -1;    // pause

                abt.StartPosition = FormStartPosition.Manual;
                int w = Screen.GetWorkingArea(abt).Width;
                int h = Screen.GetWorkingArea(abt).Height;
                abt.Location = new Point(Cursor.Position.X + abt.Width > w ? w - abt.Width : Cursor.Position.X, Cursor.Position.Y + abt.Height > h ? h - abt.Height : Cursor.Position.Y);
                abt.ShowDialog();

                timerCount = aptTime;    // resume
            }
        }

        private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            timerCount = -1;    // pause
            BuildContextMenuStrip();
        }

        private void ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            timerCount = aptTime;    // resume
        }

        private void HideshowClick(object sender, EventArgs e)
        {
            HideShow();
        }

        private void ExitClick(object sender, EventArgs e)
        {
            QuitTaskBarReader();
        }

        private void OpenFile()
        {
            timerCount = -1;    // pause

            openFileDialog.Title = tools.getString("openFileDialog_title");
            openFileDialog.Filter = tools.getString("openFileDialog_filter");

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                TXTURL = openFileDialog.FileName;
                //MessageBox.Show(txt_URL);
                ProcessBook();
                return;
            }

            timerCount = aptTime;    // resume
        }

        private void HideShow()
        {
            if (this.Visible == true) this.Hide();
            else this.Show();
        }

        private void QuitTaskBarReader()
        {
            SaveCurProgess();
            //restorePrevTitle();
            //StopListeningForWindowResize();
            //StopListeningForWindowSwitch();
            Application.Exit();
        }

        private void Hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            // Show the keys pressed in a label.
            String key = e.Key.ToString();
            //String keyComb = e.Modifier.ToString() + " + " + key;
            //MessageBox.Show(keyComb);

            switch (key)
            {
                case "Up":
                    if (!isBookmarkView)
                    {
                        AddBookMark();
                    }
                    else
                    {
                        JumpThroughBookMarks(-1);
                    }
                    break;
                case "Down":
                    if (!isBookmarkView)
                    {
                        ToggleBookmarkView();
                    }
                    else
                    {
                        JumpThroughBookMarks(1);
                    }
                    break;
                case "Left":
                    ReadBackward();
                    break;
                case "Right":
                    ReadForward();
                    break;
                case "Q":
                    QuitTaskBarReader();
                    break;
                case "Space":
                    HideShow();
                    break;
                case "X":
                    ToggleBookmarkView();
                    break;
                case "Delete":
                    DeleteCurBookMark();
                    break;
                default:
                    MessageBox.Show(tools.getString("hotkey_invalid_pressed"));
                    break;
            }
        }

        #endregion

        #region Reader functions

        // flag == -1: read backward; flag == 0: read again; flag == 1: read forward; flag == 2: bookmarks
        private void JumpToLine(Int32 flag)
        {
            Double progress = (Double)curLineNum / (Double)totalLineNum * 100;

            if (isBookmarkView || flag == 2)
            {
                BookmarkView = true;
                //curLineText_pre = "(" + (bookmark_idx + 1).ToString() + "/" + bookmark_count + ") ";
                curLineText_pre = string.Format("({0}/{1}) ", (bookmark_idx + 1).ToString(), bookmark_count);
            }
            else
            {
                BookmarkView = false;
                curLineText_pre = String.Format("({0:0.0}%) ", progress);
            }

            Int32 curLine_idx = curLineNum - 1;
            if (curLine_idx >= 0 && curLine_idx < txt_book.Count())
            {
                curLineText_content = txt_book[curLine_idx].Trim();
                lineOffset = (lineOffset >= curLineText_content.Length) ? 0 : lineOffset;
                lineOffset_OLD = (lineOffset_OLD >= curLineText_content.Length) ? 0 : lineOffset_OLD;

                if (flag == 0)
                    curLineText = TruncatePixelLength(curLineText_pre, curLineText_content, lineOffset_OLD);
                else curLineText = TruncatePixelLength(curLineText_pre, curLineText_content, lineOffset);

                SetText(curLineText);
            }
            else
            {
                if ((curLine_idx) < 0)
                {
                    SetText(tools.getString("readbackward_nomore"));
                    curLineNum = 1;
                }
                else
                {
                    SetText(tools.getString("readforward_nomore"));
                    curLineNum = txt_book.Count();
                }

                // timer here to go back to reading in 3 secs
                timer.Enabled = true;
                timerCount = 2;
                timerFlag = 2;
            }
        }

        private void ReadForward()
        {
            if (txt_URL != null)
            {
                if (lineOffset != 0)
                    lineOffset += 1;    // ?
                else curLineNum++;

                BookmarkView = false;

                JumpToLine(1);
                SaveCurProgess();

                if (this.Visible && aptTime > 0)
                {
                    // timer here to apt in given secs
                    timer.Enabled = true;
                    timerCount = aptTime;
                    timerFlag = 0;
                }
            }
        }

        private void ReadCurrentLine()
        {
            if (txt_URL != null)
            {
                BookmarkView = false;

                JumpToLine(0);
                SaveCurProgess();
            }
        }

        private void ReadBackward()
        {
            if (txt_URL != null)
            {
                if (lineOffset != 0)
                    lineOffset = 0;
                if (lineOffset_OLD == 0)
                    curLineNum--;

                BookmarkView = false;

                JumpToLine(-1);
                SaveCurProgess();
            }
        }

        private void LoadCurProgess()
        {
            Tuple<int, int> progress = tools.loadCurLoc();
            curLineNum = progress.Item1;
            lineOffset = progress.Item2;
            lineOffset_OLD = lineOffset;
        }

        private void SaveCurProgess()
        {
            tools.writeCurLoc(curLineNum, lineOffset_OLD);
        }

        private void AddBookMark()
        {
            if (txt_URL != null)
            {
                if (tools.writeBookMark(curLineNum, lineOffset_OLD))
                {
                    SetText(tools.getString("bookmark_added"));
                }
                else
                {
                    SetText(tools.getString("bookmark_exists"));
                }

                // timer here to go back to reading in 3 secs
                timer.Enabled = true;
                timerCount = 2;
                timerFlag = 2;
            }
        }

        // flag == -1: previous bookmark; flag == 0: current bookmark; flag == 1: next bookmark
        private void JumpThroughBookMarks(Int32 flag)
        {
            if (txt_URL != null)
            {
                bookmarks = tools.loadBookMarks();
                if (bookmarks == null || bookmarks.Count == 0)
                {
                    SetText(tools.getString("bookmark_none"));

                    // timer here to go back to reading in 3 secs
                    timer.Enabled = true;
                    timerCount = 2;
                    timerFlag = 2;
                }
                else
                {
                    BookmarkView = true;

                    bookmark_count = bookmarks.Count;
                    if (bookmark_idx == bookmark_count - 1)
                    {
                        if (flag == 1)
                            bookmark_idx = 0;
                        else if (flag == -1)
                            bookmark_idx--;
                    }
                    else if (bookmark_idx == 0)
                    {
                        if (flag == 1)
                            bookmark_idx++;
                        else if (flag == -1)
                            bookmark_idx = bookmark_count - 1;
                    }
                    else
                    {
                        if (flag == 1)
                            bookmark_idx++;
                        else if (flag == -1)
                            bookmark_idx--;
                    }

                    curLineNum = bookmarks[bookmark_idx].Item1;
                    lineOffset = bookmarks[bookmark_idx].Item2;
                    lineOffset_OLD = lineOffset;
                    JumpToLine(2);
                }
            }
        }

        private void DeleteCurBookMark()
        {
            if (txt_URL != null && isBookmarkView)
            {
                if (tools.deleteBookMark(curLineNum, lineOffset_OLD))
                {
                    bookmarks = tools.loadBookMarks();
                    if (bookmarks == null || bookmarks.Count == 0)
                    {
                        SetText(tools.getString("bookmark_all_deleted"));

                        // timer here to go back to reading in 3 secs
                        timer.Enabled = true;
                        timerCount = 2;
                        timerFlag = 2;
                        curLineNum = prevLineNum;
                        lineOffset = prevLineOffset;
                        lineOffset_OLD = lineOffset;
                        BookmarkView = false;

                    }
                    else
                    {
                        bookmark_count = bookmarks.Count;
                        if (bookmark_idx >= bookmark_count - 1)
                            bookmark_idx = 0;
                        else bookmark_idx++;

                        curLineNum = bookmarks[bookmark_idx].Item1;
                        lineOffset = bookmarks[bookmark_idx].Item2;
                        lineOffset_OLD = lineOffset;
                        JumpToLine(2);
                    }
                }
                else
                {
                    SetText(tools.getString("bookmark_delete_error"));

                    // timer here to go back to reading in 3 secs
                    timer.Enabled = true;
                    timerCount = 2;
                    timerFlag = 2;
                }
            }
        }

        private void DeleteBookMark(int lineNum, int offset)
        {
            if (txt_URL != null)
            {
                if (tools.deleteBookMark(lineNum, offset))
                {
                    bookmarks = tools.loadBookMarks();
                    if (bookmarks == null || bookmarks.Count == 0)
                    {
                        SetText(tools.getString("bookmark_all_deleted"));

                        // timer here to go back to reading in 3 secs
                        timer.Enabled = true;
                        timerCount = 2;
                        timerFlag = 2;
                        BookmarkView = false;
                        LoadCurProgess();
                    }
                }
                else
                {
                    SetText(tools.getString("bookmark_delete_error"));

                    // timer here to go back to reading in 3 secs
                    timer.Enabled = true;
                    timerCount = 2;
                    timerFlag = 2;
                }
            }
        }

        private void ToggleBookmarkView()
        {
            if (txt_URL != null)
            {
                if (isBookmarkView)
                {
                    // JumpFromBookmarkToCurLoc
                    curLineNum = prevLineNum;
                    lineOffset = prevLineOffset;
                    lineOffset_OLD = lineOffset;

                    BookmarkView = false;

                    ReadCurrentLine();
                }
                else
                {
                    // JumpFromCurLocToBookmark
                    JumpThroughBookMarks(0);
                }
            }
        }

        private string GetBookmarkPreview(int lineNum, int offset)
        {
            string bookmark_preview = "";
            if (txt_URL != null && bookmarks != null && bookmarks.Count > 0)
            {
                //Console.WriteLine(string.Format("Line ({0}, {1}): {2}", lineNum, offset, txt_book[lineNum-1].Trim().Substring(offset)));
                bookmark_preview = txt_book[lineNum-1].Trim().Substring(offset);

                // take the first 30 characters
                int length = 30;
                int length_safe = length > bookmark_preview.Length ? bookmark_preview.Length : length;
                string trailing = length > length_safe ? "" : "...";
                bookmark_preview = bookmark_preview.Substring(0, length_safe);
                // Truncate substring at word
                Int32 newIdx = bookmark_preview.LastIndexOf(" ", length_safe - 1, length_safe);
                bookmark_preview = ((newIdx > 0) ? bookmark_preview.Substring(0, newIdx) : bookmark_preview) + trailing;
            }
            return bookmark_preview;
        }

        #endregion

        #region Helper functions

        private void SetText(String s)
        {
            content.Text = s;
        }

        private void ProcessBook()
        {
            try
            {
                txt_book = File.ReadAllLines(txt_URL, Encoding.Default)
                    .Where(arg => !String.IsNullOrWhiteSpace(arg)).ToArray();
                //txt_name = Path.GetFileNameWithoutExtension(txt_URL);
                txt_name = Path.GetFullPath(txt_URL);
                totalLineNum = txt_book.Count();
                curLineNum = 0;
                lineOffset = 0;
                lineOffset_OLD = 0;

                tools.Name = txt_name;
                tools.LineNum = totalLineNum;

                LoadCurProgess();
                ReadCurrentLine();
                bookmarks = tools.loadBookMarks();
                bookmark_idx = 0;
            }
            catch
            {
                tools.deleteBook(txt_URL);
                return;
            }
        }

        private String TruncatePixelLength(String pre, String line, Int32 startIdx)
        {
            // Update lineOffset_OLD
            lineOffset_OLD = startIdx;

            String content = line.Substring(startIdx);
            String input = pre + content;
            Int32 safty_value = MeasureDisplayStringWidth(pre + pre + "......");

            // If everything fits, return
            //Console.WriteLine("Text length: " + TextRenderer.MeasureText(input, SystemFonts.CaptionFont).Width);
            //Console.WriteLine("Text length2: " + MeasureDisplayStringWidth(input, SystemFonts.CaptionFont));
            //Int32 stringLengthInPixel = TextRenderer.MeasureText(input, SystemFonts.CaptionFont).Width;
            Int32 stringLengthInPixel = MeasureDisplayStringWidth(input);
            //Console.WriteLine("stringLengthInPixel: " + stringLengthInPixel);
            if (stringLengthInPixel < width - safty_value)
            {
                lineOffset = 0;
                return input;
            }

            // If not, calculate max substring that fits
            //Int32 newLength = length - TextRenderer.MeasureText(pre + "...", SystemFonts.CaptionFont).Width;
            Int32 newLength = width - safty_value;  // just to be safe
            //Console.WriteLine("newLength: " + newLength);
            Int32 tempLength = 0;
            Int32 idx = 0;
            for (; idx < content.Length - 1 && tempLength < newLength; idx++)
            {
                //tempLength = TextRenderer.MeasureText(content.Substring(0, idx + 1), SystemFonts.CaptionFont).Width;
                tempLength = MeasureDisplayStringWidth(content.Substring(0, idx + 1));
            }            
            while (tempLength >= newLength)
            {
                idx--;
                tempLength = MeasureDisplayStringWidth(content.Substring(0, idx + 1));
            }
            //Console.WriteLine("tempLength2: " + tempLength);

            // Truncate substring at word
            Int32 newIdx = content.LastIndexOf(" ", idx, idx + 1);
            idx = (newIdx > 0) ? newIdx : idx;

            // Update lineOffset
            if (idx == content.Length - 1)
                lineOffset = 0;
            else lineOffset = startIdx + idx;

            // return result
            string result = pre + content.Substring(0, idx + 1) + "...";
            //Console.WriteLine("final length: " + MeasureDisplayStringWidth(result));
            return result;
        }

        private Int32 MeasureDisplayStringWidth(String text)
        {
            Int32 result = 0;
            //using (Graphics graphics = this.CreateGraphics())
            //{
            //    /*
            //    StringFormat format = new StringFormat();
            //    RectangleF rect = new RectangleF(0, 0, 1000, 1000);
            //    CharacterRange[] ranges = { new CharacterRange(0, text.Length) };
            //    Region[] regions = new Region[1];

            //    format.SetMeasurableCharacterRanges(ranges);

            //    regions = graphics.MeasureCharacterRanges(text, font, rect, format);
            //    rect = regions[0].GetBounds(graphics);
            //    result = (Int32)(rect.Right + 1.0f);
            //    */

            //    Single doubleWidth = graphics.MeasureString(text + text, font).Width;
            //    Single singleWidth = graphics.MeasureString(text, font).Width;
            //    result = (Int32)(doubleWidth - singleWidth);
            //    Console.WriteLine("graphics measure: " + result);
            //}

            // draw in label, use textrenderer
            var ddoubleWidth = TextRenderer.MeasureText(text + text, font).Width;
            var ssingleWidth = TextRenderer.MeasureText(text, font).Width;
            result = (Int32)(ddoubleWidth - ssingleWidth);
            //Console.WriteLine("textrenderer measure: " + result);

            return result;
        }

        #endregion
    }
}
