using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskbarReader
{
    public partial class HotKeys : Form
    {
        public HotKeys(Tools tools)
        {
            InitializeComponent();

            this.ForeColor = tools.ForeColor;
            this.BackColor = tools.BackColor;

            label1.Text = tools.GetString("hotkey_label1");
            label2.Text = tools.GetString("hotkey_label2");
            label3.Text = tools.GetString("hotkey_label3");
            hotkeys_ok_button.Text = tools.GetString("button_ok");

            hotkey1.Text = tools.GetString("hotkey1");
            hotkey2.Text = tools.GetString("hotkey2");
            hotkey3.Text = tools.GetString("hotkey3");
            hotkey4.Text = tools.GetString("hotkey4");
            hotkey5.Text = tools.GetString("hotkey5");
            hotkey6.Text = tools.GetString("hotkey6");
            hotkey7.Text = tools.GetString("hotkey7");
            hotkey8.Text = tools.GetString("hotkey8");
            hotkey9.Text = tools.GetString("hotkey9");
            hotkey10.Text = tools.GetString("hotkey10");
            mouse1.Text = tools.GetString("mouse1");
            mouse2.Text = tools.GetString("mouse2");
            mouse3.Text = tools.GetString("mouse3");

            hotkey1_label.Text = tools.GetString("hotkey1_label");
            hotkey2_label.Text = tools.GetString("hotkey2_label");
            hotkey3_label.Text = tools.GetString("hotkey3_label");
            hotkey4_label.Text = tools.GetString("hotkey4_label");
            hotkey5_label.Text = tools.GetString("hotkey5_label");
            hotkey6_label.Text = tools.GetString("hotkey6_label");
            hotkey7_label.Text = tools.GetString("hotkey7_label");
            hotkey8_label.Text = tools.GetString("hotkey8_label");
            hotkey9_label.Text = tools.GetString("hotkey9_label");
            hotkey10_label.Text = tools.GetString("hotkey10_label");
            mouse1_label.Text = tools.GetString("mouse1_label");
            mouse2_label.Text = tools.GetString("mouse2_label");
            mouse3_label.Text = tools.GetString("mouse3_label");
        }

        private void hotkeys_ok_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
