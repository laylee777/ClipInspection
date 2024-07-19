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
	public partial class FrmModelList : Form
	{
		FrmMain main = null;

		public FrmModelList(FrmMain main)
		{
			InitializeComponent();
			this.main = main;
		}

		private void FrmModelList_Load(object sender, EventArgs e)
		{
			RefreshList();
		}

		private void RefreshList()
		{
			lvModelList.Items.Clear();

			paramModelInfo modelInfo = null;
			DirectoryInfo di = new DirectoryInfo(main.paramIni.ConfigPath);
			foreach (FileInfo fi in di.GetFiles("*.cfg"))
			{
				try
				{
					using (StreamReader sr = new StreamReader(fi.FullName))
					{
						XmlSerializer xs = new XmlSerializer(typeof(paramModelInfo));
						modelInfo = (paramModelInfo)xs.Deserialize(sr);
					}
					ListViewItem lvi = new ListViewItem();
					lvi.Text = modelInfo.No.ToString();
					lvi.SubItems.Add(modelInfo.Name);
					lvModelList.Items.Add(lvi);
				}
				catch
				{
				}
			}
		}

		public static paramModelInfo NewConfig(int no)
		{
			paramModelInfo infoModel = new paramModelInfo();
			infoModel.No = no;
			infoModel.Name = "Default Model";
			infoModel.GubunInspect4144 = 0;

			infoModel.Cam1_Data = new paramCam1_Data();
			infoModel.Cam1_Data.CamNo = 0;
			infoModel.Cam1_Data.UseInspect = true;
			infoModel.Cam1_Data.Expose = 500;
			infoModel.Cam1_Data.Gain = 1000;
			infoModel.Cam1_Data.DelayCapture = 0;
			infoModel.Cam1_Data.Threshold = 100;
			infoModel.Cam1_Data.ROI = new Rectangle(220, 130, 200, 230);
			infoModel.Cam1_Data.DefectWidthMax = 0;
			infoModel.Cam1_Data.DefectHeightMax = 0;
			infoModel.Cam1_Data.Masks.Clear();
			infoModel.Cam1_Data.AlignROI = new Rectangle(600, 130, 200, 200);
			infoModel.Cam1_Data.AlignCenter = new Point();

			infoModel.Cam2_Data = new paramCam2_Data();
			infoModel.Cam2_Data.CamNo = 1;
			infoModel.Cam2_Data.UseInspect = true;
			infoModel.Cam2_Data.Expose = 500;
			infoModel.Cam2_Data.Gain = 1000;
			infoModel.Cam2_Data.DelayCapture = 0;
			infoModel.Cam2_Data.Threshold = 100;
			infoModel.Cam2_Data.ROI = new Rectangle(220, 130, 200, 230);
			infoModel.Cam2_Data.DefectWidthMax = 0;
			infoModel.Cam2_Data.DefectHeightMax = 0;
			infoModel.Cam2_Data.Masks.Clear();
			infoModel.Cam2_Data.AlignROI = new Rectangle(600, 130, 200, 200);
			infoModel.Cam2_Data.AlignCenter = new Point();

			infoModel.Cam3_Data = new paramCam3_Data();
			infoModel.Cam3_Data.CamNo = 2;
			infoModel.Cam3_Data.UseInspect = true;
			infoModel.Cam3_Data.Expose = 500;
			infoModel.Cam3_Data.Gain = 1000;
			infoModel.Cam3_Data.DelayCapture = 0;
			infoModel.Cam3_Data.Threshold = 100;
			infoModel.Cam3_Data.ROI = new Rectangle(220, 130, 200, 230);
			infoModel.Cam3_Data.DefectWidthMax = 0;
			infoModel.Cam3_Data.DefectHeightMax = 0;
			infoModel.Cam3_Data.Masks.Clear();
			infoModel.Cam3_Data.AlignROI = new Rectangle(600, 130, 200, 200);
			infoModel.Cam3_Data.AlignCenter = new Point();

			infoModel.Cam4_Data = new paramCam4_Data();
			infoModel.Cam4_Data.CamNo = 3;
			infoModel.Cam4_Data.UseInspect = true;
			infoModel.Cam4_Data.Expose = 500;
			infoModel.Cam4_Data.Gain = 1000;
			infoModel.Cam4_Data.DelayCapture = 0;
			infoModel.Cam4_Data.Threshold = 100;
			infoModel.Cam4_Data.ROI = new Rectangle(220, 130, 200, 230);
			infoModel.Cam4_Data.DefectWidthMax = 0;
			infoModel.Cam4_Data.DefectHeightMax = 0;
			infoModel.Cam4_Data.Masks.Clear();
			infoModel.Cam4_Data.AlignROI = new Rectangle(600, 130, 200, 200);
			infoModel.Cam4_Data.AlignCenter = new Point();

			infoModel.Cam5_Data = new paramCam5_Data();
			infoModel.Cam5_Data.CamNo = 4;
			infoModel.Cam5_Data.UseInspect = true;
			infoModel.Cam5_Data.UseXLYL = true;
			infoModel.Cam5_Data.UseLHRH = true;
			infoModel.Cam5_Data.UseT = true;
			infoModel.Cam5_Data.UseP = true;
			infoModel.Cam5_Data.UseT2 = true;
			infoModel.Cam5_Data.UseWL = true;
			infoModel.Cam5_Data.Threshold = 100;
			infoModel.Cam5_Data.ROI = new Rectangle(220, 130, 200, 230);
			infoModel.Cam5_Data.XLMax = 0;
			infoModel.Cam5_Data.XL = 0;
			infoModel.Cam5_Data.XLMin = 0;
			infoModel.Cam5_Data.YLMax = 0;
			infoModel.Cam5_Data.YL = 0;
			infoModel.Cam5_Data.YLMin = 0;
			infoModel.Cam5_Data.LHMax = 0;
			infoModel.Cam5_Data.LH = 0;
			infoModel.Cam5_Data.LHMin = 0;
			infoModel.Cam5_Data.RHMax = 0;
			infoModel.Cam5_Data.RH = 0;
			infoModel.Cam5_Data.RHMin = 0;
			infoModel.Cam5_Data.TMax = 0;
			infoModel.Cam5_Data.T = 0;
			infoModel.Cam5_Data.TMin = 0;
			infoModel.Cam5_Data.PMax = 0;
			infoModel.Cam5_Data.P = 0;
			infoModel.Cam5_Data.PMin = 0;
			infoModel.Cam5_Data.T2Max = 0;
			infoModel.Cam5_Data.T2 = 0;
			infoModel.Cam5_Data.T2Min = 0;
			infoModel.Cam5_Data.WLMax = 0;
			infoModel.Cam5_Data.WL = 0;
			infoModel.Cam5_Data.WLMin = 0;
			infoModel.Cam5_Data.XLOffset = 0;
			infoModel.Cam5_Data.YLOffset = 0;
			infoModel.Cam5_Data.LHOffset = 0;
			infoModel.Cam5_Data.RHOffset = 0;
			infoModel.Cam5_Data.TOffset = 0;
			infoModel.Cam5_Data.POffset = 0;
			infoModel.Cam5_Data.T2Offset = 0;
			infoModel.Cam5_Data.WLOffset = 0;
			infoModel.Cam5_Data.TAngleStart = 53;
			infoModel.Cam5_Data.TAngleGap = 8;
			infoModel.Cam5_Data.TAngleCount = 32;
			infoModel.Cam5_Data.TMaxDetail = 0;
			infoModel.Cam5_Data.TMinDetail = 0;
			infoModel.Cam5_Data.T2AngleStart = 30;
			infoModel.Cam5_Data.AngleInspectPos = 100;
			infoModel.Cam5_Data.RealLength = 0;
			infoModel.Cam5_Data.CalcLength = 0;
			infoModel.Cam5_Data.MmPerPixel = 0;
			infoModel.Cam5_Data.ViewUnit = 0;

			return infoModel;
		}

		private paramModelInfo LoadConfig(int no)
		{
			paramModelInfo modelInfo = null;
			try
			{
				string path = string.Format("{0}{1:000}.cfg", main.paramIni.ConfigPath, no);
				using (StreamReader sr = new StreamReader(path))
				{
					XmlSerializer xs = new XmlSerializer(typeof(paramModelInfo));
					modelInfo = (paramModelInfo)xs.Deserialize(sr);
				}
			}
			catch
			{
			}
			return modelInfo;
		}

		private void SaveConfig(paramModelInfo modelInfo)
		{
			string path = string.Format("{0}{1:000}.cfg", main.paramIni.ConfigPath, modelInfo.No);
			using (StreamWriter sw = new StreamWriter(path))
			{
				XmlSerializer xs = new XmlSerializer(typeof(paramModelInfo));
				xs.Serialize(sw, modelInfo);
			}
		}

		private void DeleteConfig(int no)
		{
			try
			{
				string path = string.Format("{0}{1:000}.cfg", main.paramIni.ConfigPath, no);
				File.Delete(path);
			}
			catch(Exception exc)
			{
				MessageBox.Show("파일을 삭제할 수 없습니다.\r\n" + exc.Message, "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private paramModelInfo CopyModel(paramModelInfo src)
		{
			paramModelInfo dst = new paramModelInfo();
			dst.No = src.No;
			dst.Name = src.Name;
			dst.GubunInspect4144 = src.GubunInspect4144;

			dst.Cam1_Data = new paramCam1_Data();
			dst.Cam1_Data.CamNo = src.Cam1_Data.CamNo;
			dst.Cam1_Data.UseInspect = src.Cam1_Data.UseInspect;
			dst.Cam1_Data.Expose = src.Cam1_Data.Expose;
			dst.Cam1_Data.Gain = src.Cam1_Data.Gain;
			dst.Cam1_Data.DelayCapture = src.Cam1_Data.DelayCapture;
			dst.Cam1_Data.Threshold = src.Cam1_Data.Threshold;
			dst.Cam1_Data.ROI = src.Cam1_Data.ROI;
			dst.Cam1_Data.DefectWidthMax = src.Cam1_Data.DefectWidthMax;
			dst.Cam1_Data.DefectHeightMax = src.Cam1_Data.DefectHeightMax;
			dst.Cam1_Data.Masks.Clear();
			foreach (paramMask_Data mask in src.Cam1_Data.Masks)
			{
				paramMask_Data pmask = new paramMask_Data();
				pmask.No = mask.No;
				pmask.ROI = mask.ROI;
				dst.Cam1_Data.Masks.Add(pmask);
			}
			dst.Cam1_Data.AlignROI = src.Cam1_Data.AlignROI;
			dst.Cam1_Data.AlignCenter = src.Cam1_Data.AlignCenter;

			dst.Cam2_Data = new paramCam2_Data();
			dst.Cam2_Data.CamNo = src.Cam2_Data.CamNo;
			dst.Cam2_Data.UseInspect = src.Cam2_Data.UseInspect;
			dst.Cam2_Data.Expose = src.Cam2_Data.Expose;
			dst.Cam2_Data.Gain = src.Cam2_Data.Gain;
			dst.Cam2_Data.DelayCapture = src.Cam2_Data.DelayCapture;
			dst.Cam2_Data.Threshold = src.Cam2_Data.Threshold;
			dst.Cam2_Data.ROI = src.Cam2_Data.ROI;
			dst.Cam2_Data.DefectWidthMax = src.Cam2_Data.DefectWidthMax;
			dst.Cam2_Data.DefectHeightMax = src.Cam2_Data.DefectHeightMax;
			dst.Cam2_Data.Masks.Clear();
			foreach (paramMask_Data mask in src.Cam2_Data.Masks)
			{
				paramMask_Data pmask = new paramMask_Data();
				pmask.No = mask.No;
				pmask.ROI = mask.ROI;
				dst.Cam2_Data.Masks.Add(pmask);
			}
			dst.Cam2_Data.AlignROI = src.Cam2_Data.AlignROI;
			dst.Cam2_Data.AlignCenter = src.Cam2_Data.AlignCenter;

			dst.Cam3_Data = new paramCam3_Data();
			dst.Cam3_Data.CamNo = src.Cam3_Data.CamNo;
			dst.Cam3_Data.UseInspect = src.Cam3_Data.UseInspect;
			dst.Cam3_Data.Expose = src.Cam3_Data.Expose;
			dst.Cam3_Data.Gain = src.Cam3_Data.Gain;
			dst.Cam3_Data.DelayCapture = src.Cam3_Data.DelayCapture;
			dst.Cam3_Data.Threshold = src.Cam3_Data.Threshold;
			dst.Cam3_Data.ROI = src.Cam3_Data.ROI;
			dst.Cam3_Data.DefectWidthMax = src.Cam3_Data.DefectWidthMax;
			dst.Cam3_Data.DefectHeightMax = src.Cam3_Data.DefectHeightMax;
			dst.Cam3_Data.Masks.Clear();
			foreach (paramMask_Data mask in src.Cam3_Data.Masks)
			{
				paramMask_Data pmask = new paramMask_Data();
				pmask.No = mask.No;
				pmask.ROI = mask.ROI;
				dst.Cam3_Data.Masks.Add(pmask);
			}
			dst.Cam3_Data.AlignROI = src.Cam3_Data.AlignROI;
			dst.Cam3_Data.AlignCenter = src.Cam3_Data.AlignCenter;

			dst.Cam4_Data = new paramCam4_Data();
			dst.Cam4_Data.CamNo = src.Cam4_Data.CamNo;
			dst.Cam4_Data.UseInspect = src.Cam4_Data.UseInspect;
			dst.Cam4_Data.Expose = src.Cam4_Data.Expose;
			dst.Cam4_Data.Gain = src.Cam4_Data.Gain;
			dst.Cam4_Data.DelayCapture = src.Cam4_Data.DelayCapture;
			dst.Cam4_Data.Threshold = src.Cam4_Data.Threshold;
			dst.Cam4_Data.ROI = src.Cam4_Data.ROI;
			dst.Cam4_Data.DefectWidthMax = src.Cam4_Data.DefectWidthMax;
			dst.Cam4_Data.DefectHeightMax = src.Cam4_Data.DefectHeightMax;
			dst.Cam4_Data.Masks.Clear();
			foreach (paramMask_Data mask in src.Cam4_Data.Masks)
			{
				paramMask_Data pmask = new paramMask_Data();
				pmask.No = mask.No;
				pmask.ROI = mask.ROI;
				dst.Cam4_Data.Masks.Add(pmask);
			}
			dst.Cam4_Data.AlignROI = src.Cam4_Data.AlignROI;
			dst.Cam4_Data.AlignCenter = src.Cam4_Data.AlignCenter;

			dst.Cam5_Data = new paramCam5_Data();
			dst.Cam5_Data.CamNo = src.Cam5_Data.CamNo;
			dst.Cam5_Data.UseInspect = src.Cam5_Data.UseInspect;
			dst.Cam5_Data.UseXLYL = src.Cam5_Data.UseXLYL;
			dst.Cam5_Data.UseLHRH = src.Cam5_Data.UseLHRH;
			dst.Cam5_Data.UseT = src.Cam5_Data.UseT;
			dst.Cam5_Data.UseP = src.Cam5_Data.UseP;
			dst.Cam5_Data.UseT2 = src.Cam5_Data.UseT2;
			dst.Cam5_Data.UseWL = src.Cam5_Data.UseWL;
			dst.Cam5_Data.Threshold = src.Cam5_Data.Threshold;
			dst.Cam5_Data.ROI = src.Cam5_Data.ROI;
			dst.Cam5_Data.XLMax = src.Cam5_Data.XLMax;
			dst.Cam5_Data.XL = src.Cam5_Data.XL;
			dst.Cam5_Data.XLMin = src.Cam5_Data.XLMin;
			dst.Cam5_Data.YLMax = src.Cam5_Data.YLMax;
			dst.Cam5_Data.YL = src.Cam5_Data.YL;
			dst.Cam5_Data.YLMin = src.Cam5_Data.YLMin;
			dst.Cam5_Data.LHMax = src.Cam5_Data.LHMax;
			dst.Cam5_Data.LH = src.Cam5_Data.LH;
			dst.Cam5_Data.LHMin = src.Cam5_Data.LHMin;
			dst.Cam5_Data.RHMax = src.Cam5_Data.RHMax;
			dst.Cam5_Data.RH = src.Cam5_Data.RH;
			dst.Cam5_Data.RHMin = src.Cam5_Data.RHMin;
			dst.Cam5_Data.TMax = src.Cam5_Data.TMax;
			dst.Cam5_Data.T = src.Cam5_Data.T;
			dst.Cam5_Data.TMin = src.Cam5_Data.TMin;
			dst.Cam5_Data.PMax = src.Cam5_Data.PMax;
			dst.Cam5_Data.P = src.Cam5_Data.P;
			dst.Cam5_Data.PMin = src.Cam5_Data.PMin;
			dst.Cam5_Data.T2Max = src.Cam5_Data.T2Max;
			dst.Cam5_Data.T2 = src.Cam5_Data.T2;
			dst.Cam5_Data.T2Min = src.Cam5_Data.T2Min;
			dst.Cam5_Data.WLMax = src.Cam5_Data.WLMax;
			dst.Cam5_Data.WL = src.Cam5_Data.WL;
			dst.Cam5_Data.WLMin = src.Cam5_Data.WLMin;
			dst.Cam5_Data.XLOffset = src.Cam5_Data.XLOffset;
			dst.Cam5_Data.YLOffset = src.Cam5_Data.YLOffset;
			dst.Cam5_Data.LHOffset = src.Cam5_Data.LHOffset;
			dst.Cam5_Data.RHOffset = src.Cam5_Data.RHOffset;
			dst.Cam5_Data.TOffset = src.Cam5_Data.TOffset;
			dst.Cam5_Data.POffset = src.Cam5_Data.POffset;
			dst.Cam5_Data.T2Offset = src.Cam5_Data.T2Offset;
			dst.Cam5_Data.WLOffset = src.Cam5_Data.WLOffset;
			dst.Cam5_Data.TAngleStart = src.Cam5_Data.TAngleStart;
			dst.Cam5_Data.TAngleGap = src.Cam5_Data.TAngleGap;
			dst.Cam5_Data.TAngleCount = src.Cam5_Data.TAngleCount;
			dst.Cam5_Data.TMaxDetail = src.Cam5_Data.TMaxDetail;
			dst.Cam5_Data.TMinDetail = src.Cam5_Data.TMinDetail;
			dst.Cam5_Data.T2AngleStart = src.Cam5_Data.T2AngleStart;
			dst.Cam5_Data.AngleInspectPos = src.Cam5_Data.AngleInspectPos;
			dst.Cam5_Data.RealLength = src.Cam5_Data.RealLength;
			dst.Cam5_Data.CalcLength = src.Cam5_Data.CalcLength;
			dst.Cam5_Data.MmPerPixel = src.Cam5_Data.MmPerPixel;
			dst.Cam5_Data.ViewUnit = src.Cam5_Data.ViewUnit;

			return dst;
		}

		private void lvModelList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvModelList.SelectedItems.Count > 0)
			{
				txtNo.Text = lvModelList.SelectedItems[0].Text;
				txtName.Text = lvModelList.SelectedItems[0].SubItems[1].Text;
			}
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			int nMax = 0;
			foreach (ListViewItem lvi in lvModelList.Items)
			{
				if (Int32.Parse(lvi.Text) > nMax)
					nMax = Int32.Parse(lvi.Text);
			}
			nMax++;
			txtNo.Text = nMax.ToString();

			ListViewItem lvi2 = new ListViewItem();
			lvi2.Text = nMax.ToString();
			lvi2.SubItems.Add(txtName.Text);
			lvModelList.Items.Add(lvi2);

			paramModelInfo modelInfo = CopyModel(main.ModelInfo);
			modelInfo.No = nMax;
			modelInfo.Name = txtName.Text;
			SaveConfig(modelInfo);

			MessageBox.Show("모델을 추가하였습니다.");
		}

		private void btnModify_Click(object sender, EventArgs e)
		{
			if (txtNo.Text == "")
				return;

			foreach (ListViewItem lvi in lvModelList.Items)
			{
				if (lvi.Text == txtNo.Text)
				{
					lvi.SubItems[1].Text = txtName.Text;
					break;
				}
			}

			paramModelInfo modelInfo = LoadConfig(Int32.Parse(txtNo.Text));
			if (modelInfo != null)
			{
				modelInfo.No = Int32.Parse(txtNo.Text);
				modelInfo.Name = txtName.Text;

				SaveConfig(modelInfo);
				MessageBox.Show("모델을 수정하였습니다.");
			}
			else
			{
				MessageBox.Show("제품을 수정하는 중 오류가 발생하였습니다.");
			}
		}

		private void btnSelect_Click(object sender, EventArgs e)
		{
			if (txtNo.Text == "")
			{
				MessageBox.Show("제품을 먼저 선택하세요.");
				return;
			}

			paramModelInfo modelInfo = LoadConfig(Int32.Parse(txtNo.Text));
			if (modelInfo != null)
			{
				main.ModelInfo = modelInfo;
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
			else
			{
				MessageBox.Show("저장된 모델의 파일이 잘못되었습니다.");
			}
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void btnModifyNo_Click(object sender, EventArgs e)
		{
			if (txtNo.Text == "")
			{
				MessageBox.Show("제품을 먼저 선택하세요.");
				return;
			}

			int no = 0;
			if (int.TryParse(txtModifyNo.Text, out no) == true)
			{
				if (no > 0)
				{
					bool bOK = true;
					string str = no.ToString();
					foreach (ListViewItem lvi in lvModelList.Items)
					{
						if (str == lvi.Text)
						{
							MessageBox.Show("이미 변경하고자 하는 번호가 존재합니다.");
							bOK = false;
							break;
						}
					}
					if (bOK == true)
					{
						paramModelInfo modelInfo = LoadConfig(int.Parse(txtNo.Text));
						modelInfo.No = no;
						SaveConfig(modelInfo);
						DeleteConfig(int.Parse(txtNo.Text));
						txtNo.Text = "";
						txtName.Text = "";
						RefreshList();
					}
				}
				else
					MessageBox.Show("번호는 양수여야 합니다.");
			}
			else
				MessageBox.Show("번호는 정수형이어야 합니다.");
		}
	}
}
