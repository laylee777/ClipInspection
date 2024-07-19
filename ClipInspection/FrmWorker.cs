using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;

namespace ClipInspection
{
	public partial class FrmWorker : Form
	{
		FrmMain main = null;
		public string RetWorker = "";

		public FrmWorker(FrmMain main)
		{
			InitializeComponent();
			this.main = main;
		}

		private void FrmWorker_Load(object sender, EventArgs e)
		{
			try
			{
				string path = FrmMain.CURRENT_DIRECTORY + "Worker.cfg";
				using (StreamReader sr = new StreamReader(path))
				{
					while (sr.Peek() >= 0)
					{
						string str = sr.ReadLine();
						lvWorkerList.Items.Add(str);
					}
				}
			}
			catch
			{
			}
		}

		private void FrmWorker_FormClosing(object sender, FormClosingEventArgs e)
		{
			string path = FrmMain.CURRENT_DIRECTORY + "Worker.cfg";
			using (StreamWriter wr = new StreamWriter(path))
			{
				foreach (ListViewItem lvi in lvWorkerList.Items)
				{
					wr.WriteLine(lvi.Text);
				}
			}
		}

		private void lvWorkerList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvWorkerList.SelectedItems.Count > 0)
			{
				txtName.Text = lvWorkerList.SelectedItems[0].Text;
			}
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			string str = txtName.Text.Trim();
			if (str == "")
				MessageBox.Show("작업자 이름을 입력해 주십시오.");
			else
			{
				bool bAdd = true;
				foreach (ListViewItem lvi in lvWorkerList.Items)
				{
					if (str.ToUpper() == lvi.Text.ToUpper())
					{
						MessageBox.Show("작업자 이름이 이미 존재하므로, 추가할 수 없습니다.");
						bAdd = false;
						break;
					}
				}
				if (bAdd == true)
				{
					lvWorkerList.Items.Add(str);
				}
			}
		}

		private void btnModify_Click(object sender, EventArgs e)
		{
			if (lvWorkerList.SelectedItems.Count == 0)
				MessageBox.Show("수정하고자 하는 작업자를 리스트에서 선택해 주십시오.");
			else
			{
				string str = txtName.Text.Trim();
				if (str == "")
					MessageBox.Show("작업자 이름을 입력해 주십시오.");
				else
				{
					if (MessageBox.Show(string.Format("[{0}]를 [{1}]로 변경하시겠습니까?", lvWorkerList.SelectedItems[0].Text, str), "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						lvWorkerList.SelectedItems[0].Text = str;
					}
				}
			}
		}

		private void btnSelect_Click(object sender, EventArgs e)
		{
			if (lvWorkerList.SelectedItems.Count == 0)
				MessageBox.Show("작업자를 리스트에서 작업자명을 선택해 주십시오.");
			else
			{
				RetWorker = lvWorkerList.SelectedItems[0].Text;
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}
