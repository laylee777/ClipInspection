using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using IVMCommon;

namespace ClipInspection
{
	public partial class FrmReport : Form
	{
		FrmMain main = null;

		string strSaveDataPath;

		public FrmReport(FrmMain main)
		{
			InitializeComponent();
			this.main = main;
		}

		private void FrmReport_Load(object sender, EventArgs e)
		{
			BuildCameraButton();
		}

		private void BuildCameraButton()
		{
			for (int i = 0; i < FrmMain.MAX_CAMERA_CNT; i++)
			{
				RadioButton btn = new RadioButton();
				btn.Appearance = Appearance.Button;
				btn.Location = new System.Drawing.Point(1 + i * (104 + 6), 1);
				btn.Size = new System.Drawing.Size(104, 35);
				btn.Tag = i.ToString();
				btn.Text = main.paramIni.CamInfoCol[i].DisplayName;
				btn.TextAlign = ContentAlignment.MiddleCenter;
				btn.Click += new EventHandler(btnCamera_Click);
				this.Controls.Add(btn);
			}
			{
				RadioButton btn = new RadioButton();
				btn.Appearance = Appearance.Button;
				btn.Location = new System.Drawing.Point(1 + FrmMain.MAX_CAMERA_CNT * (104 + 6), 1);
				btn.Size = new System.Drawing.Size(104, 35);
				btn.Tag = FrmMain.MAX_CAMERA_CNT.ToString();
				btn.Text = "불러오기";
				btn.TextAlign = ContentAlignment.MiddleCenter;
				btn.Click += new EventHandler(btnCamera_Click);
				this.Controls.Add(btn);
			}
		}

		void btnCamera_Click(object sender, EventArgs e)
		{
			try
			{
				strSaveDataPath = "";
				lstReport.Columns.Clear();
				lstReport.Items.Clear();

				int nCamNo = int.Parse(((RadioButton)sender).Tag.ToString());
				if (nCamNo == FrmMain.MAX_CAMERA_CNT)
				{
					OpenFileDialog open = new OpenFileDialog();
					open.Filter = "Save Data files (*.csv)|*.csv|All files (*.*)|*.*";

					if (open.ShowDialog() == DialogResult.OK)
					{
						strSaveDataPath = open.FileName;
					}
				}
				else
					strSaveDataPath = main.strSaveDataPath[nCamNo];
				if (strSaveDataPath != null && strSaveDataPath != "")
				{
					Thread threadRefreshList = new Thread(RefreshListThread_Run);
					threadRefreshList.Start();

				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[btnCamera_Click] " + exc.Message);
			}
		}

		private void RefreshListThread_Run(object lparam)
		{
			RefreshList();
		}

		delegate void RefreshListCollback();
		private void RefreshList()
		{
			try
			{
				if (lstReport.InvokeRequired)
				{
					RefreshListCollback d = new RefreshListCollback(RefreshList);
					this.Invoke(d);
				}
				else
				{
					string path = Directory.GetCurrentDirectory() + "temp.csv";
					File.Copy(strSaveDataPath, path, true);
					using (StreamReader sr = new StreamReader(path, Encoding.Default, false))
					{
						string[] col = sr.ReadLine().Split(new char[] { ',' });
						for (int i = 0; i < col.Length; i++)
						{
							ColumnHeader header = new ColumnHeader();
							header.Text = col[i];
							if (i == 1)
								header.TextAlign = HorizontalAlignment.Left;
							else
								header.TextAlign = HorizontalAlignment.Right;
							header.Width = 60;
							lstReport.Columns.Add(header);
						}

						while (sr.EndOfStream == false)
						{
							string[] token = sr.ReadLine().Split(new char[] { ',' });
							ListViewItem lvi = new ListViewItem(token[0]);
							for (int i = 1; i < token.Length; i++)
							{
								lvi.SubItems.Add(token[i]);
							}
							lstReport.Items.Add(lvi);
						}
					}

					RefreshStatistics();
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[RefreshList] " + exc.ToString());
			}
		}

		private void RefreshStatistics()
		{
			try
			{
				lstStatistics.Columns.Clear();
				lstStatistics.Items.Clear();

				for (int i = 0; i < lstReport.Columns.Count; i++)
				{
					ColumnHeader header = new ColumnHeader();
					if (i > 1)
						header.Text = lstReport.Columns[i].Text;
					else
						header.Text = "";
					header.TextAlign = lstReport.Columns[i].TextAlign;
					header.Width = lstReport.Columns[i].Width;
					lstStatistics.Columns.Add(header);
				}

				ListViewItem lviMax = new ListViewItem("최대");
				lviMax.SubItems.Add("(오차)");
				ListViewItem lviAve = new ListViewItem("평균");
				lviAve.SubItems.Add("");
				ListViewItem lviMin = new ListViewItem("최소");
				lviMin.SubItems.Add("(오차)");
				for (int i = 2; i < lstStatistics.Columns.Count - 2; i++)
				{
					double dbMax = double.MinValue, dbMin = double.MaxValue, dbTotal = 0, dbValue = 0;
					int nCnt = 0;
					for (int j = 0; j < lstReport.Items.Count; j++)
					{
						try
						{
							dbValue = double.Parse(lstReport.Items[j].SubItems[i].Text);
						}
						catch
						{
							continue;
						}
						nCnt++;
						dbTotal += dbValue;
						if (dbValue > dbMax)
							dbMax = dbValue;
						if (dbValue < dbMin)
							dbMin = dbValue;
					}
					if (nCnt == 0)
						return;
					double dbAve = dbTotal / nCnt;
					lviMax.SubItems.Add((dbMax - dbAve).ToString());
					lviAve.SubItems.Add(dbAve.ToString());
					lviMin.SubItems.Add((dbAve - dbMin).ToString());
				}
				lstStatistics.Items.Add(lviMax);
				lstStatistics.Items.Add(lviAve);
				lstStatistics.Items.Add(lviMin);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[RefreshStatistics] " + exc.Message);
			}
		}
	}
}
