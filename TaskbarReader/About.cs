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
    public partial class About : Form
    {
        public About(Tools tools)
        {
            InitializeComponent();

            this.ForeColor = tools.ForeColor;
            this.BackColor = tools.BackColor;

            about_version_label.Text = tools.getString("about_version_label");
            about_author_label.Text = tools.getString("about_author_label");
            about_email_label.Text = tools.getString("about_email_label");
            about_intro_label.Text = tools.getString("about_intro_label");

            about_name.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            about_version.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            about_author.Text = "Henry Xu";
            about_email.Text = "HenryXrl@Gmail.com";
            about_intro.Text = string.Join(Environment.NewLine, tools.getString("about_intro").Split(new string[] { "<br/>" }, StringSplitOptions.None));

            about_ok_button.Text = tools.getString("button_ok");

            about_pictureBox.Image = tools.img;
            about_pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void about_ok_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
