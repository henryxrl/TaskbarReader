using System;
using System.Windows.Forms;
using TaskbarReader;

namespace AutoUpdate
{
	/// <summary>
	/// Form to prompt the user to accept the update
	/// </summary>
	internal partial class AutoUpdateAcceptForm : DevComponents.DotNetBar.Metro.MetroForm
	{
		/// <summary>
		/// The program to update's info
		/// </summary>
		private AutoUpdatable applicationInfo;

		/// <summary>
		/// The update info from the update.xml
		/// </summary>
		private AutoUpdateXml updateInfo;

		/// <summary>
		/// The program to update's tools
		/// </summary>
		private Tools tools;

		/// <summary>
		/// Creates a new AutoUpdateAcceptForm
		/// </summary>
		/// <param name="applicationInfo"></param>
		/// <param name="updateInfo"></param>
		internal AutoUpdateAcceptForm(AutoUpdatable applicationInfo, AutoUpdateXml updateInfo)
		{
			InitializeComponent();

			this.applicationInfo = applicationInfo;
			this.updateInfo = updateInfo;
			this.tools = applicationInfo.Tools;

			pictureBox.Image = tools.Img;

			this.Text = tools.GetString("update_found_title");

			this.lblAppName.Text = this.applicationInfo.ApplicationName;
			this.lblUpdateAvail.Text = tools.GetString("update_found");
			this.lblNewVersion_label.Text = tools.GetString("update_new");
			this.lblCurVersion_label.Text = tools.GetString("update_cur");
			this.lblDescription.Text = tools.GetString("update_description");

			this.btnYes.Text = tools.GetString("button_ok");
			this.btnNo.Text = tools.GetString("button_cancel");

			// Assigns the icon if it isn't null
			if (this.applicationInfo.ApplicationIcon != null)
				this.Icon = this.applicationInfo.ApplicationIcon;

			// Adds the current version # to the form
			this.lblCurVersion.Text = applicationInfo.ApplicationAssembly.GetName().Version.ToString();

			// Adds the update version # to the form
			this.lblNewVersion.Text = this.updateInfo.Version.ToString();

			// Fill in update info
			this.txtDescription.Text = updateInfo.Description;
		}

		private void AutoUpdateAcceptForm_Load(object sender, EventArgs e)
		{
			this.lblNewVersion_label.ForeColor = tools.ThemeColor;
			this.lblCurVersion_label.ForeColor = tools.ThemeColor;
			this.lblDescription.ForeColor = tools.ThemeColor;
		}

		private void btnYes_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Yes;
			this.Close();
		}

		private void btnNo_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.No;
			this.Close();
		}

		private void txtDescription_KeyDown(object sender, KeyEventArgs e)
		{
			// Only allow Cntrl - C to copy text
			if (!(e.Control && e.KeyCode == Keys.C))
				e.SuppressKeyPress = true;
		}

	}
}
