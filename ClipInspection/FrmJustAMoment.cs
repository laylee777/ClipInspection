using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IVMCommon
{
	public partial class FrmJustAMoment : Form
	{
		int nLanguage = 0;

		delegate void CloseCallback();

		private Size OwnerSize;

		public FrmJustAMoment(int language)
		{
			InitializeComponent();
			nLanguage = language;
		}

		public void SetOwnerSize(Size size)
		{
			OwnerSize.Width = size.Width;
			OwnerSize.Height = size.Height;
		}

		private void FrmJustAMoment_Shown(object sender, EventArgs e)
		{
			int x = (OwnerSize.Width - this.Width) / 2;
			int y = (OwnerSize.Height - this.Height*4) / 2;
			this.Location = new Point(x, y);
		}

		private void FrmJustAMoment_Load(object sender, EventArgs e)
		{
			int x = (OwnerSize.Width - this.Width) / 2;
			int y = (OwnerSize.Height - this.Height*4) / 2;
			this.Location = new Point(x, y);
			switch (nLanguage)
			{
				case 0:	// 한국어
					lblMsg.Text = "초기화중입니다. 잠시만 기다려 주십시오.";
					break;
				case 1:	// 영어
					lblMsg.Text = "Initializing. Please wait a minute.";
					break;
				case 2:	// 중국어
					lblMsg.Text = "重新开机器中 等一下";
					break;
			}
		}

		public void CloseDialog()
		{
			if (this.InvokeRequired)
			{
				CloseCallback d = new CloseCallback(CloseDialog);
				this.Invoke(d);
			}
			else
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}
	}
}
