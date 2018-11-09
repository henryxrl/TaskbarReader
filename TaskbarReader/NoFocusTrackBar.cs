﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBReader2
{
	internal class NoFocusTrackBar : System.Windows.Forms.TrackBar
	{
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public extern static int SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

		private static int MakeParam(int loWord, int hiWord)
		{
			return (hiWord << 16) | (loWord & 0xffff);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			SendMessage(this.Handle, 0x0128, MakeParam(1, 0x1), 0);
		}
	}
}
