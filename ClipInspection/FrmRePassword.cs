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
	public partial class FrmRePassword : Form
	{
		FrmMain main = null;

		public FrmRePassword(FrmMain main)
		{
			InitializeComponent();
			this.main = main;
		}

		private void FrmRePassword_Load(object sender, EventArgs e)
		{
			txtPassword.Focus();
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			string strPass = txtPassword.Text.Trim();
			string strNewPass = txtNewPassword.Text.Trim();
			string strNewConfirm = txtNewPasswordConfirm.Text.Trim();

			if (strPass != main.paramIni.Password && main.paramIni.Password != null)
			{
				MessageBox.Show("이전 암호가 잘못되었습니다.");
				return;
			}
			else if (strNewPass != strNewConfirm)
			{
				MessageBox.Show("새 암호와 암호 확인이 서로 틀립니다.");
				return;
			}
			main.paramIni.Password = strNewPass;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
