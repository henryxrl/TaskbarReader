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
    public partial class APTDialog : Form
    {
        public int apt_time = 0;

        public APTDialog(Tools tools)
        {
            InitializeComponent();

            this.ForeColor = tools.ForeColor;
            this.BackColor = tools.BackColor;

            label1.Text = tools.GetString("apt_label2");
            label2.Text = tools.GetString("apt_label_sec");

            button1.Text = tools.GetString("button_ok");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            apt_time = (int)numericUpDown1.Value;
            this.Close();
        }
    }
}
