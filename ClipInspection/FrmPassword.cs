using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClipInspection
{
	public partial class FrmPassword : Form
	{
		FrmMain main = null;

		public FrmPassword(FrmMain main)
		{
			InitializeComponent();
			this.main = main;
		}

		private void FrmPassword_Load(object sender, EventArgs e)
		{
			txtPassword.Focus();
		}

		private void btnRepassword_Click(object sender, EventArgs e)
		{
			FrmRePassword frm = new FrmRePassword(main);
			frm.ShowDialog();
			txtPassword.Focus();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			string strPass = txtPassword.Text.Trim();
			if (strPass != main.paramIni.Password)
			{
				MessageBox.Show("암호가 잘못되었습니다.");
				return;
			}
			main.bAdminMode = true;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
