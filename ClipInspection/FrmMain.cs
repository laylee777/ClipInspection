using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using IVMCommon;
using Mitsubishi;
using Euresys.MultiCam;

namespace ClipInspection
{
	public partial class FrmMain : Form
	{
		public static int MAX_CAMERA_CNT = 5;
		public static int MAX_CAMERA_BUFFER_CNT = 16;
		public static string CURRENT_DIRECTORY = Directory.GetCurrentDirectory() + "\\";

		CImageViewer[] ivCamViewer = new CImageViewer[MAX_CAMERA_CNT];
		CImageViewer[] ivMainViewer = new CImageViewer[MAX_CAMERA_CNT];
		public bool bIsROIVisible = true;
		public bool bAutoStart = false;
		public int nView = 0;
		//public int nTotalCount = 0;
		public int nNGCount1 = 0;
		public int nOKCount2 = 0;
		public int nNGCount2_1 = 0;
		public int nNGCount2_2 = 0;
		public int[,] nInspectResult = new int[MAX_CAMERA_CNT, MAX_CAMERA_BUFFER_CNT];
		public bool bAdminMode = false;

		paramResult1 displayResult1 = new paramResult1();
		paramResult2 displayResult2 = new paramResult2();
		paramResult3 displayResult3 = new paramResult3();
		paramResult4 displayResult4 = new paramResult4();
		paramResult5 displayResult5 = new paramResult5();

		// The Mutex object that will protect image objects during processing
		private static Mutex[] imageMutex = new Mutex[MAX_CAMERA_CNT];
		// The MultiCam object that controls the acquisition
		UInt32[] channel = new UInt32[MAX_CAMERA_CNT];
		// The MultiCam object that contains the acquired buffer
		private UInt32[] currentSurface = new UInt32[MAX_CAMERA_CNT];
		MC.CALLBACK[] multiCamCallback = new MC.CALLBACK[MAX_CAMERA_CNT];
		//Color[] thresPalette = new Color[256];
		Color[][] thresPalette = new Color[MAX_CAMERA_CNT][];
		private int nCaptureCnt = 0;
		private bool bItemInverse = false;

		public paramINI paramIni = new paramINI();
		public paramModelInfo ModelInfo = new paramModelInfo();	// 제품에 대한 정보와 검사 정보
		public Bitmap[,] lstImage = new Bitmap[MAX_CAMERA_CNT, MAX_CAMERA_BUFFER_CNT + 1];	// 이미지들을 저장해 놓는 장소. MAX_CAMERA_BUFFER_CNT개씩 루프한다.
		private int[] nGrabLoopCnt = new int[MAX_CAMERA_CNT];	// 현재 찍은 이미지의 루프 카운터.
		public string[] strSaveDataPath = new string[MAX_CAMERA_CNT];

		CCLink ctlCom = new CCLink(CURRENT_DIRECTORY + @"\CC-Link.ivm");	// CCLINK 컨트롤
		private short[] nCCLinkData = new short[10];

		public FrmMain()
		{
			InitializeComponent();
		}

		FrmJustAMoment frmJustAMoment = null;
		private void ShowFrmJustAMoment(object size)
		{
			frmJustAMoment = new FrmJustAMoment(0);
			frmJustAMoment.SetOwnerSize((Size)size);
			frmJustAMoment.ShowDialog();
			frmJustAMoment.Dispose();
			frmJustAMoment = null;
		}

		private void FrmMain_Load(object sender, EventArgs e)
		{
			Log.InitializeLog("check");
			ctlCom.portOpen();

			LoadIni("Default.ini");
			LockUI();

			Thread threadFrmJustAMoment = new Thread(ShowFrmJustAMoment);
			threadFrmJustAMoment.Start(new Size(Width, Height));

			InitMulticam();

			InitControls();
			InitImageViewer();
			InitData();
			LoadConfig(paramIni.LastModelNo);
			LastCountRead();

			bwPLCEcho.RunWorkerAsync();
			bwCCLink.RunWorkerAsync();

			DeleteLastImage();

			frmJustAMoment.CloseDialog();
		}

		private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			bwPLCEcho.CancelAsync();
			bwCCLink.CancelAsync();

			Saveini("Default.ini");
			LastCountWrite();
			ExitMulticam();
			ctlCom.portClose();

			//Log.AddLogMessage(Log.LogType.ERROR, 0, "Program Exit.");
			Log.FlushLogFile();
			Log.CloseLog();
		}

		#region /// Init Module ///
		private void InitData()
		{
			try
			{
				for (int i = 0; i < MAX_CAMERA_CNT; i++)
				{
					for (int j = 0; j < MAX_CAMERA_BUFFER_CNT + 1; j++)
						lstImage[i, j] = null;
					nGrabLoopCnt[i] = 0;

					thresPalette[i] = new Color[256];
					for (int j = 0; j < 256; j++)
						thresPalette[i][j] = Color.FromArgb(j, j, j);
				}
				ResetData();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[InitData] " + exc.Message);
			}
		}

		private void ResetData()
		{
			for (int i = 0; i < MAX_CAMERA_CNT; i++)
			{
				if (nGrabLoopCnt[i] > 0)
				{
					if (lstImage[i, nGrabLoopCnt[i]] != null)
					{
						if (lstImage[i, 0] != null)
							lstImage[i, 0].Dispose();
						lstImage[i, 0] = (Bitmap)lstImage[i, nGrabLoopCnt[i] % 4].Clone();
					}
					nGrabLoopCnt[i] = 0;
					if (lstImage[i, 0] == null)
					{
						ivCamViewer[i].Image = null;
						ivMainViewer[i].Image = null;
					}
					else
					{
						ivCamViewer[i].Image = (Bitmap)lstImage[i, 0].Clone();
						ivMainViewer[i].Image = (Bitmap)lstImage[i, 0].Clone();
					}
					ivCamViewer[i].Invalidate();
					ivMainViewer[i].Invalidate();
				}
				for (int j = 0; j < MAX_CAMERA_BUFFER_CNT; j++)
					nInspectResult[i, j] = 0;
			}
			nCaptureCnt = 0;
			//nTotalCount = 0;
			nNGCount1 = 0;
			nOKCount2 = 0;
			nNGCount2_1 = 0;
			nNGCount2_2 = 0;
			displayResult5.nXLNGCnt = 0;
			displayResult5.nYLNGCnt = 0;
			displayResult5.nLHNGCnt = 0;
			displayResult5.nRHNGCnt = 0;
			displayResult5.nTNGCnt = 0;
			displayResult5.nPNGCnt = 0;
			displayResult5.nT2NGCnt = 0;
			displayResult5.nWLNGCnt = 0;

			txtOKCnt1.SetTextInt(0);
			txtNGCnt1.SetTextInt(0);
			txtOKCnt2.SetTextInt(0);
			txtNGCnt2.SetTextInt(0);
			txtOKCnt3.SetTextInt(0);
			txtNGCnt3.SetTextInt(0);
			txtOKCnt4.SetTextInt(0);
			txtNGCnt4.SetTextInt(0);
			txtOKCnt5.SetTextInt(0);
			txtNGCnt5_1.SetTextInt(0);
			txtNGCnt5_2.SetTextInt(0);
			txtOKCntDetail1.SetTextInt(0);
			txtNGCntDetail1.SetTextInt(0);
			txtOKCntDetail2.SetTextInt(0);
			txtNGCntDetail2.SetTextInt(0);
			txtOKCntDetail3.SetTextInt(0);
			txtNGCntDetail3.SetTextInt(0);
			txtOKCntDetail4.SetTextInt(0);
			txtNGCntDetail4.SetTextInt(0);
			txtOKCntDetail5.SetTextInt(0);
			txtNGCntDetail5_1.SetTextInt(0);
			txtNGCntDetail5_2.SetTextInt(0);

			DisplayResult(0);
		}

		private void InitControls()
		{
			axTotalCnt.ColorOff = Color.FromArgb(50, 50, 50);
			axOKCnt.ColorOff = Color.FromArgb(0, 50, 0);
			axOKCnt.ColorOn = Color.LawnGreen;
			axNGCnt1.ColorOff = Color.FromArgb(60, 0, 0);
			axNGCnt1.ColorOn = Color.Red;
			axNGCnt2_1.ColorOff = Color.FromArgb(55, 0, 0);
			axNGCnt2_1.ColorOn = Color.Red;
			axNGCnt2_2.ColorOff = Color.FromArgb(50, 0, 0);
			axNGCnt2_2.ColorOn = Color.Red;
			axOKRate.ColorOff = Color.FromArgb(50, 0, 0);
			axOKRate.ColorOn = Color.Coral;
			axOKRate.Precision = 2;
		}

		private void InitImageViewer()
		{
			ivCamViewer[0] = ivCamViewer1;
			ivCamViewer[1] = ivCamViewer2;
			ivCamViewer[2] = ivCamViewer3;
			ivCamViewer[3] = ivCamViewer4;
			ivCamViewer[4] = ivCamViewer5;

			ivMainViewer[0] = ivMainViewer1;
			ivMainViewer[1] = ivMainViewer2;
			ivMainViewer[2] = ivMainViewer3;
			ivMainViewer[3] = ivMainViewer4;
			ivMainViewer[4] = ivMainViewer5;

			// 시작은 전체화면부터 보이도록 함.
			nView = 0;
			rdoViewModeAll.Checked = true;
		}

		private void LoadIni(string filename)
		{
			try
			{
				using (StreamReader sr = new StreamReader(filename))
				{
					XmlSerializer xs = new XmlSerializer(typeof(paramINI));
					paramIni = (paramINI)xs.Deserialize(sr);
				}
			}
			catch (Exception exc)
			{
				if (File.Exists("Default - 복사본.Ini") == true)
				{
					MessageBox.Show("INI파일이 없습니다. 백업한 파일을 불러옵니다.");
					using (StreamReader sr = new StreamReader("Default - 복사본.Ini"))
					{
						XmlSerializer xs = new XmlSerializer(typeof(paramINI));
						paramIni = (paramINI)xs.Deserialize(sr);
					}
				}
				else
				{
					Log.AddLogMessage(Log.LogType.ERROR, 0, exc.ToString());
					MessageBox.Show("INI파일이 없습니다. 기본파일을 생성합니다.");
					paramIni.NGImagePath = @"D:\SaveImage\";
					paramIni.IsNGImage = 0;
					paramIni.SaveDataPath = @"D:\SaveData\";
					paramIni.IsSaveData = false;
					paramIni.ConfigPath = @"Config\";
					paramIni.NGImageAliveDays = 7;
					paramIni.LastModelNo = ModelInfo.No = 1;
					paramIni.Password = "";
					paramIni.CamInfoCol.Clear();
					for (int i = 0; i < MAX_CAMERA_CNT; i++)
					{
						paramCamInfo camInfo = new paramCamInfo();
						camInfo.DisplayName = string.Format("카메라 {0}", i + 1);
						camInfo.DriveIndex = 0;
						camInfo.ConnectorName = string.Format("CAM{0}", i + 1);
						camInfo.CamFile = @"Camera\default.cam";
						camInfo.SurfaceCount = 10;
						camInfo.ColorFormat = "Y8";
						camInfo.BoardTopology = "";
						camInfo.MmPerPixel = 0;
						paramIni.CamInfoCol.Insert(i, camInfo);
					}
				}
				Saveini(filename);
			}
			Directory.CreateDirectory(paramIni.NGImagePath);
			Directory.CreateDirectory(paramIni.SaveDataPath);
			Directory.CreateDirectory(paramIni.ConfigPath);

			chkUseSaveData.Checked = paramIni.IsSaveData;
			txtSaveDataPath.Text = paramIni.SaveDataPath;
			if (paramIni.IsNGImage == 2)
				rdoUseNGImage1.Checked = true;
			else if (paramIni.IsNGImage == 1)
				rdoUseNGImage2.Checked = true;
			else
				rdoUseNGImage3.Checked = true;
			txtNGImagePath.Text = paramIni.NGImagePath;
			if (paramIni.Password == null || paramIni.Password == "")
				bAdminMode = true;
		}

		public void Saveini(string filename)
		{
			using (StreamWriter sw = new StreamWriter(filename))
			{
				XmlSerializer xs = new XmlSerializer(typeof(paramINI));
				xs.Serialize(sw, paramIni);
			}

		}

		private void LoadConfig(int nIndex)
		{
			try
			{
				string path = string.Format("{0}{1:000}.cfg", paramIni.ConfigPath, nIndex);
				using (StreamReader sr = new StreamReader(path))
				{
					XmlSerializer xs = new XmlSerializer(typeof(paramModelInfo));
					ModelInfo = (paramModelInfo)xs.Deserialize(sr);
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[LoadConfig] " + exc.ToString());
				#region /// 데이터 초기화 ///
				ModelInfo = FrmModelList.NewConfig(nIndex);
				#endregion
			}
			ChangeModel();
		}

		private void SaveConfig(int nIndex)
		{
			string path = string.Format("{0}{1:000}.cfg", paramIni.ConfigPath, nIndex);
			using (StreamWriter sw = new StreamWriter(path))
			{
				XmlSerializer xs = new XmlSerializer(typeof(paramModelInfo));
				xs.Serialize(sw, ModelInfo);
			}

		}

		private void ChangeModel()
		{
			try
			{
				BindModelData();

				paramIni.LastModelNo = ModelInfo.No;
				Saveini("Default.ini");
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ChangeModel] " + exc.ToString());
			}
		}

		delegate void BindModelDataCollback();
		private void BindModelData()
		{
			if (this.txtModelNo.InvokeRequired)
			{
				BindModelDataCollback d = new BindModelDataCollback(BindModelData);
				this.Invoke(d);
			}
			else
			{
				txtModelNo.Text = ModelInfo.No.ToString();
				txtModelName.Text = ModelInfo.Name;

				txtThresLevel1.SetTextInt(ModelInfo.Cam1_Data.Threshold);
				txtExpose1.SetTextInt(ModelInfo.Cam1_Data.Expose);
				txtGain1.SetTextInt(ModelInfo.Cam1_Data.Gain);
				txtDelayCapture1.SetTextInt(ModelInfo.Cam1_Data.DelayCapture);
				txtThreshold1.SetTextInt(ModelInfo.Cam1_Data.Threshold);
				txtDefectWidthMax1.SetTextDouble(ModelInfo.Cam1_Data.DefectWidthMax);
				txtDefectHeightMax1.SetTextDouble(ModelInfo.Cam1_Data.DefectHeightMax);
				chkUseInspect1.Checked = ModelInfo.Cam1_Data.UseInspect;
				lstMask1.Items.Clear();
				foreach (paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
					lstMask1.Items.Add(mask.No.ToString());

				txtThresLevel2.SetTextInt(ModelInfo.Cam2_Data.Threshold);
				txtExpose2.SetTextInt(ModelInfo.Cam2_Data.Expose);
				txtGain2.SetTextInt(ModelInfo.Cam2_Data.Gain);
				txtDelayCapture2.SetTextInt(ModelInfo.Cam2_Data.DelayCapture);
				txtThreshold2.SetTextInt(ModelInfo.Cam2_Data.Threshold);
				txtDefectWidthMax2.SetTextDouble(ModelInfo.Cam2_Data.DefectWidthMax);
				txtDefectHeightMax2.SetTextDouble(ModelInfo.Cam2_Data.DefectHeightMax);
				chkUseInspect2.Checked = ModelInfo.Cam2_Data.UseInspect;
				lstMask2.Items.Clear();
				foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
					lstMask2.Items.Add(mask.No.ToString());
				if (ModelInfo.GubunInspect4144 == 1)
					rdoGubunInspect4144_1.Checked = true;
				else if (ModelInfo.GubunInspect4144 == 2)
					rdoGubunInspect4144_2.Checked = true;
				else
					rdoGubunInspect4144_0.Checked = true;

				txtThresLevel3.SetTextInt(ModelInfo.Cam3_Data.Threshold);
				txtExpose3.SetTextInt(ModelInfo.Cam3_Data.Expose);
				txtGain3.SetTextInt(ModelInfo.Cam3_Data.Gain);
				txtDelayCapture3.SetTextInt(ModelInfo.Cam3_Data.DelayCapture);
				txtThreshold3.SetTextInt(ModelInfo.Cam3_Data.Threshold);
				txtDefectWidthMax3.SetTextDouble(ModelInfo.Cam3_Data.DefectWidthMax);
				txtDefectHeightMax3.SetTextDouble(ModelInfo.Cam3_Data.DefectHeightMax);
				chkUseInspect3.Checked = ModelInfo.Cam3_Data.UseInspect;
				lstMask3.Items.Clear();
				foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
					lstMask3.Items.Add(mask.No.ToString());

				txtThresLevel4.SetTextInt(ModelInfo.Cam4_Data.Threshold);
				txtExpose4.SetTextInt(ModelInfo.Cam4_Data.Expose);
				txtGain4.SetTextInt(ModelInfo.Cam4_Data.Gain);
				txtDelayCapture4.SetTextInt(ModelInfo.Cam4_Data.DelayCapture);
				txtThreshold4.SetTextInt(ModelInfo.Cam4_Data.Threshold);
				txtDefectWidthMax4.SetTextDouble(ModelInfo.Cam4_Data.DefectWidthMax);
				txtDefectHeightMax4.SetTextDouble(ModelInfo.Cam4_Data.DefectHeightMax);
				chkUseInspect4.Checked = ModelInfo.Cam4_Data.UseInspect;
				lstMask4.Items.Clear();
				foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
					lstMask4.Items.Add(mask.No.ToString());

				txtThresLevel5.SetTextInt(ModelInfo.Cam5_Data.Threshold);
				txtThreshold5.SetTextInt(ModelInfo.Cam5_Data.Threshold);
				txtRealLength5.SetTextDouble(ModelInfo.Cam5_Data.RealLength);
				txtMmPerPixel5.SetTextDouble(ModelInfo.Cam5_Data.MmPerPixel);
				chkUseInspect5.Checked = ModelInfo.Cam5_Data.UseInspect;
				chkUseXLYL5.Checked = ModelInfo.Cam5_Data.UseXLYL;
				chkUseLHRH5.Checked = ModelInfo.Cam5_Data.UseLHRH;
				chkUseT5.Checked = ModelInfo.Cam5_Data.UseT;
				chkUseP5.Checked = ModelInfo.Cam5_Data.UseP;
				chkUseT25.Checked = ModelInfo.Cam5_Data.UseT2;
				chkUseWL5.Checked = ModelInfo.Cam5_Data.UseWL;
				txtXLMax5.SetTextDouble(ModelInfo.Cam5_Data.XLMax);
				txtXL5.SetTextDouble(ModelInfo.Cam5_Data.XL);
				txtXLMin5.SetTextDouble(ModelInfo.Cam5_Data.XLMin);
				txtYLMax5.SetTextDouble(ModelInfo.Cam5_Data.YLMax);
				txtYL5.SetTextDouble(ModelInfo.Cam5_Data.YL);
				txtYLMin5.SetTextDouble(ModelInfo.Cam5_Data.YLMin);
				txtLHMax5.SetTextDouble(ModelInfo.Cam5_Data.LHMax);
				txtLH5.SetTextDouble(ModelInfo.Cam5_Data.LH);
				txtLHMin5.SetTextDouble(ModelInfo.Cam5_Data.LHMin);
				txtRHMax5.SetTextDouble(ModelInfo.Cam5_Data.RHMax);
				txtRH5.SetTextDouble(ModelInfo.Cam5_Data.RH);
				txtRHMin5.SetTextDouble(ModelInfo.Cam5_Data.RHMin);
				txtTMax5.SetTextDouble(ModelInfo.Cam5_Data.TMax);
				txtT5.SetTextDouble(ModelInfo.Cam5_Data.T);
				txtTMin5.SetTextDouble(ModelInfo.Cam5_Data.TMin);
				txtPMax5.SetTextDouble(ModelInfo.Cam5_Data.PMax);
				txtP5.SetTextDouble(ModelInfo.Cam5_Data.P);
				txtPMin5.SetTextDouble(ModelInfo.Cam5_Data.PMin);
				txtT2Max5.SetTextDouble(ModelInfo.Cam5_Data.T2Max);
				txtT25.SetTextDouble(ModelInfo.Cam5_Data.T2);
				txtT2Min5.SetTextDouble(ModelInfo.Cam5_Data.T2Min);
				txtWLMax5.SetTextDouble(ModelInfo.Cam5_Data.WLMax);
				txtWL5.SetTextDouble(ModelInfo.Cam5_Data.WL);
				txtWLMin5.SetTextDouble(ModelInfo.Cam5_Data.WLMin);
				txtXLOffset5.SetTextDouble(ModelInfo.Cam5_Data.XLOffset);
				txtYLOffset5.SetTextDouble(ModelInfo.Cam5_Data.YLOffset);
				txtLHOffset5.SetTextDouble(ModelInfo.Cam5_Data.LHOffset);
				txtRHOffset5.SetTextDouble(ModelInfo.Cam5_Data.RHOffset);
				txtTOffset5.SetTextDouble(ModelInfo.Cam5_Data.TOffset);
				txtPOffset5.SetTextDouble(ModelInfo.Cam5_Data.POffset);
				txtT2Offset5.SetTextDouble(ModelInfo.Cam5_Data.T2Offset);
				txtWLOffset5.SetTextDouble(ModelInfo.Cam5_Data.WLOffset);
				txtTAngleStart5.SetTextDouble(ModelInfo.Cam5_Data.TAngleStart);
				txtTAngleGap5.SetTextDouble(ModelInfo.Cam5_Data.TAngleGap);
				txtTAngleCnt5.SetTextInt(ModelInfo.Cam5_Data.TAngleCount);
				txtTMaxDetail5.SetTextDouble(ModelInfo.Cam5_Data.TMaxDetail);
				txtTMinDetail5.SetTextDouble(ModelInfo.Cam5_Data.TMinDetail);
				txtT2AngleStart5.SetTextDouble(ModelInfo.Cam5_Data.T2AngleStart);
				txtAngleInspectPos5.SetTextInt(ModelInfo.Cam5_Data.AngleInspectPos);
			}
		}

		private void DeleteLastImage()
		{
			if (paramIni.NGImageAliveDays > 0)
			{
				DateTime dt = DateTime.Now;
				dt = dt.AddDays(-paramIni.NGImageAliveDays);
				int nBasisDate = Int32.Parse(string.Format("{0:0000}{1:00}{2:00}", dt.Year, dt.Month, dt.Day));

				DirectoryInfo di = new DirectoryInfo(paramIni.NGImagePath);
				foreach (DirectoryInfo sdi in di.GetDirectories())
				{
					try
					{
						int nDeleteDate = Int32.Parse(sdi.Name);
						if (nDeleteDate < nBasisDate)
						{
							Directory.Delete(sdi.FullName, true);
						}
					}
					catch (Exception exc)
					{
						Log.AddLogMessage(Log.LogType.ERROR, 0, "[DeleteLastImage] " + exc.ToString());
					}
				}
			}
		}

		[DllImport("kernel32")]
		public static extern uint GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport("kernel32")]
		public static extern uint WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
		string strCountPath = CURRENT_DIRECTORY + @"LastCount.ini";
		private void LastCountRead()
		{
			//nTotalCount = (int)GetPrivateProfileInt("COUNT", "Total", 0, strCountPath);
			nNGCount1 = (int)GetPrivateProfileInt("COUNT", "NG1", 0, strCountPath);
			nOKCount2 = (int)GetPrivateProfileInt("COUNT", "OK2", 0, strCountPath);
			nNGCount2_1 = (int)GetPrivateProfileInt("COUNT", "NG2_1", 0, strCountPath);
			nNGCount2_2 = (int)GetPrivateProfileInt("COUNT", "NG2_2", 0, strCountPath);
			txtOKCnt1.SetTextInt((int)GetPrivateProfileInt("COUNT", "OKCnt1", 0, strCountPath));
			txtOKCntDetail1.SetTextInt(txtOKCnt1.GetTextInt());
			txtOKCnt2.SetTextInt((int)GetPrivateProfileInt("COUNT", "OKCnt2", 0, strCountPath));
			txtOKCntDetail2.SetTextInt(txtOKCnt2.GetTextInt());
			txtOKCnt3.SetTextInt((int)GetPrivateProfileInt("COUNT", "OKCnt3", 0, strCountPath));
			txtOKCntDetail3.SetTextInt(txtOKCnt3.GetTextInt());
			txtOKCnt4.SetTextInt((int)GetPrivateProfileInt("COUNT", "OKCnt4", 0, strCountPath));
			txtOKCntDetail4.SetTextInt(txtOKCnt4.GetTextInt());
			txtOKCnt5.SetTextInt((int)GetPrivateProfileInt("COUNT", "OKCnt5", 0, strCountPath));
			txtOKCntDetail5.SetTextInt(txtOKCnt5.GetTextInt());
			txtNGCnt1.SetTextInt((int)GetPrivateProfileInt("COUNT", "NGCnt1", 0, strCountPath));
			txtNGCntDetail1.SetTextInt(txtNGCnt1.GetTextInt());
			txtNGCnt2.SetTextInt((int)GetPrivateProfileInt("COUNT", "NGCnt2", 0, strCountPath));
			txtNGCntDetail2.SetTextInt(txtNGCnt2.GetTextInt());
			txtNGCnt3.SetTextInt((int)GetPrivateProfileInt("COUNT", "NGCnt3", 0, strCountPath));
			txtNGCntDetail3.SetTextInt(txtNGCnt3.GetTextInt());
			txtNGCnt4.SetTextInt((int)GetPrivateProfileInt("COUNT", "NGCnt4", 0, strCountPath));
			txtNGCntDetail4.SetTextInt(txtNGCnt4.GetTextInt());
			txtNGCnt5_1.SetTextInt((int)GetPrivateProfileInt("COUNT", "NGCnt5_1", 0, strCountPath));
			txtNGCntDetail5_1.SetTextInt(txtNGCnt5_1.GetTextInt());
			txtNGCnt5_2.SetTextInt((int)GetPrivateProfileInt("COUNT", "NGCnt5_2", 0, strCountPath));
			txtNGCntDetail5_2.SetTextInt(txtNGCnt5_2.GetTextInt());
			DisplayResult(0);
		}

		private void LastCountWrite()
		{
			//WritePrivateProfileString("COUNT", "Total1", nTotalCount.ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NG1", nNGCount1.ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "OK2", nOKCount2.ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NG2_1", nNGCount2_1.ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NG2_2", nNGCount2_2.ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "OKCnt1", txtOKCnt1.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "OKCnt2", txtOKCnt2.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "OKCnt3", txtOKCnt3.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "OKCnt4", txtOKCnt4.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "OKCnt5", txtOKCnt5.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NGCnt1", txtNGCnt1.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NGCnt2", txtNGCnt2.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NGCnt3", txtNGCnt3.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NGCnt4", txtNGCnt4.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NGCnt5_1", txtNGCnt5_1.GetTextInt().ToString(), strCountPath);
			WritePrivateProfileString("COUNT", "NGCnt5_2", txtNGCnt5_2.GetTextInt().ToString(), strCountPath);
		}
		#endregion

		#region /// Multicam Module ///
		private void InitMulticam()
		{
			try
			{
				// Open MultiCam driver
				MC.OpenDriver();

				// Enable error logging
				MC.SetParam(MC.CONFIGURATION, "ErrorLog", "error.log");

				for (int i = 0; i < MAX_CAMERA_CNT; i++)
				{
					imageMutex[i] = new Mutex();
					// Create a channel and associate it with the first connector on the first board
					MC.Create("CHANNEL", out channel[i]);

					// ador 20120130 Topology 옵션 추가.
					if (paramIni.CamInfoCol[i].BoardTopology != "")
						MC.SetParam(channel[i], "BoardTopology", paramIni.CamInfoCol[i].BoardTopology);

					MC.SetParam(channel[i], "DriverIndex", paramIni.CamInfoCol[i].DriveIndex);
					MC.SetParam(channel[i], "Connector", paramIni.CamInfoCol[i].ConnectorName);

					// Choose the video standard
					MC.SetParam(channel[i], "CamFile", paramIni.CamInfoCol[i].CamFile);

					// Register the callback function
					multiCamCallback[i] = new MC.CALLBACK(MultiCamCallback);
					MC.RegisterCallback(channel[i], multiCamCallback[i], (uint)i);

					// ador 20110908 C#에 없는 구문으로 버퍼 할당을 20개로 늘려준다.
					MC.SetParam(channel[i], MC.SurfaceCount, paramIni.CamInfoCol[i].SurfaceCount);

					// Enable the signals corresponding to the callback functions
					MC.SetParam(channel[i], MC.SignalEnable + MC.SIG_SURFACE_PROCESSING, "ON");
					MC.SetParam(channel[i], MC.SignalEnable + MC.SIG_ACQUISITION_FAILURE, "ON");

					// Prepare the channel in order to minimize the acquisition sequence startup latency
					MC.SetParam(channel[i], "ChannelState", "READY");

					// Start an acquisition sequence by activating the channel
					String channelState;
					MC.GetParam(channel[i], "ChannelState", out channelState);
					if (channelState != "ACTIVE")
						MC.SetParam(channel[i], "ChannelState", "ACTIVE");

					// Generate a soft trigger event
					//MC.SetParam(channel[i], "ForceTrig", "TRIG");
				}
			}
			catch (Euresys.MultiCamException exc)
			{
				// An exception has occurred in the try {...} block. 
				// Retrieve its description and display it in a message box.
				Log.AddLogMessage(Log.LogType.ERROR, 0, "MultiCam Exception : " + exc.Message);
				MessageBox.Show(exc.Message, "MultiCam Exception");
				//Close();
			}
		}

		private void ExitMulticam()
		{
			try
			{
				for (int i = 0; i < MAX_CAMERA_CNT; i++)
				{
					// Stop an acquisition sequence by deactivating the channel
					if (channel[i] != 0)
						MC.SetParam(channel[i], "ChannelState", "IDLE");
					// Delete the channel
					if (channel[i] != 0)
					{
						MC.Delete(channel[i]);
						channel[i] = 0;
					}
				}
				// Close MultiCam driver
				MC.CloseDriver();
			}
			catch (Euresys.MultiCamException exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "MultiCam Exception : " + exc.Message);
				MessageBox.Show(exc.Message, "MultiCam Exception");
			}
		}

		private void MultiCamCallback(ref MC.SIGNALINFO signalInfo)
		{
			switch (signalInfo.Signal)
			{
				case MC.SIG_SURFACE_PROCESSING:
					ProcessingCallback(signalInfo);
					break;
				case MC.SIG_ACQUISITION_FAILURE:
					AcqFailureCallback(signalInfo);
					break;
				default:
					Log.AddLogMessage(Log.LogType.ERROR, 0, "MultiCam Exception : " + "Unknown signal");
					throw new Euresys.MultiCamException("Unknown signal");
			}
		}

		//Stopwatch swFrameRate = new Stopwatch();
		private void ProcessingCallback(MC.SIGNALINFO signalInfo)
		{
			Int32 cc = (Int32)signalInfo.Context;

			currentSurface[cc] = signalInfo.SignalInfo;

			try
			{
				// Update the image with the acquired image buffer data 
				Int32 width, height, bufferPitch;
				IntPtr bufferAddress;
				// The object that will contain the palette information for the bitmap
				ColorPalette imgpal = null;

				MC.GetParam(channel[cc], "ImageSizeX", out width);
				MC.GetParam(channel[cc], "ImageSizeY", out height);
				MC.GetParam(channel[cc], "BufferPitch", out bufferPitch);
				MC.GetParam(currentSurface[cc], "SurfaceAddr", out bufferAddress);

				try
				{
					imageMutex[cc].WaitOne();

					Bitmap bmImage = null;

					if (paramIni.CamInfoCol[cc].ColorFormat == "RGB24")
					{
						bmImage = new Bitmap(width, height, bufferPitch, PixelFormat.Format24bppRgb, bufferAddress);
					}
					else if (paramIni.CamInfoCol[cc].ColorFormat == "BAYER8")
					{
						bmImage = Basic.Bayer8ToBitmap(width, height, bufferPitch, bufferAddress);
					}
					else
					{
						bmImage = new Bitmap(width, height, bufferPitch, PixelFormat.Format8bppIndexed, bufferAddress);

						imgpal = bmImage.Palette;

						// Build bitmap palette Y8
						for (uint i = 0; i < 256; i++)
						{
							imgpal.Entries[i] = Color.FromArgb(
							(byte)0xFF,
							(byte)i,
							(byte)i,
							(byte)i);
						}

						bmImage.Palette = imgpal;
					}

					/* Insert image analysis and processing code here */
					GrabEndProcess(cc, bmImage);
				}
				finally
				{
					imageMutex[cc].ReleaseMutex();
				}

			}
			catch (Euresys.MultiCamException exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "MultiCam Exception : " + exc.Message);
				MessageBox.Show(exc.Message, "MultiCam Exception");
			}
			catch (System.Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "System Exception : " + exc.Message);
				MessageBox.Show(exc.Message, "System Exception");
			}
		}

		private void AcqFailureCallback(MC.SIGNALINFO signalInfo)
		{
			UInt32 currentChannel = (UInt32)signalInfo.Context;

			try
			{
				// Display frame rate and channel state
				Log.AddLogMessage(Log.LogType.ERROR, 0, "MultiCam Exception : " + String.Format("Acquisition Failure, Channel State: IDLE, Channel Number : {0}", currentChannel));
				MessageBox.Show(String.Format("Acquisition Failure, Channel State: IDLE, Channel Number : {0}", currentChannel));
			}
			catch (System.Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "System Exception : " + exc.Message);
				MessageBox.Show(exc.Message, "System Exception");
			}
		}

		private void MultiCamLiveOn()
		{
			try
			{
				MultiCamLiveOff();

				for (int i = 0; i < MAX_CAMERA_CNT; i++)
				{
					// Stop an acquisition sequence by deactivating the channel
					if (channel[i] != 0)
						MC.SetParam(channel[i], "ChannelState", "IDLE");

					// Choose the way the first acquisition is triggered
					MC.SetParam(channel[i], "TrigMode", "IMMEDIATE");
					// Choose the triggering mode for subsequent acquisitions
					MC.SetParam(channel[i], "NextTrigMode", "SAME");

					// Prepare the channel in order to minimize the acquisition sequence startup latency
					MC.SetParam(channel[i], "ChannelState", "READY");

					// Start an acquisition sequence by activating the channel
					String channelState;
					MC.GetParam(channel[i], "ChannelState", out channelState);
					if (channelState != "ACTIVE")
						MC.SetParam(channel[i], "ChannelState", "ACTIVE");
				}
			}
			catch (Euresys.MultiCamException exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[MultiCamLiveOn] " + exc.Message);
				MessageBox.Show(exc.Message, "MultiCam Exception");
			}
		}

		private void MultiCamLiveOff()
		{
			try
			{
				for (int i = 0; i < MAX_CAMERA_CNT; i++)
				{
					// Stop an acquisition sequence by deactivating the channel
					if (channel[i] != 0)
						MC.SetParam(channel[i], "ChannelState", "IDLE");

					// Choose the way the first acquisition is triggered
					MC.SetParam(channel[i], "TrigMode", "COMBINED");
					// Choose the triggering mode for subsequent acquisitions
					MC.SetParam(channel[i], "NextTrigMode", "SAME");
				}
				Thread threadMultiCamLiveOff = new Thread(MultiCamLiveOff_Run);
				threadMultiCamLiveOff.Start();
			}
			catch (Euresys.MultiCamException exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[MultiCamLiveOff] " + exc.Message);
				MessageBox.Show(exc.Message, "MultiCam Exception");
			}
		}

		private void MultiCamLiveOff_Run(object lParam)
		{
			try
			{
				for (int i = 0; i < MAX_CAMERA_CNT; i++)
				{
					// Prepare the channel in order to minimize the acquisition sequence startup latency
					try
					{
						MC.SetParam(channel[i], "ChannelState", "READY");
					}
					catch
					{
						System.Threading.Thread.Sleep(500);
						MC.SetParam(channel[i], "ChannelState", "READY");
					}

					// Start an acquisition sequence by activating the channel
					String channelState;
					MC.GetParam(channel[i], "ChannelState", out channelState);
					if (channelState != "ACTIVE")
						MC.SetParam(channel[i], "ChannelState", "ACTIVE");
				}
			}
			catch (Euresys.MultiCamException exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[MultiCamLiveOff] " + exc.Message);
				MessageBox.Show(exc.Message, "MultiCam Exception");
			}
		}

		private void SetExpose(int nCamNo, int nValue)
		{
			if (nValue > 0)
			{
				MC.SetParam(channel[nCamNo], "Expose_us", nValue);
			}
		}

		private void SetGain(int nCamNo, int nValue)
		{
			if (nValue >= 0 && nValue <= 8000)
			{
				MC.SetParam(channel[nCamNo], "Gain", nValue);
			}
		}

		private void SetDelayCapture(int nCamNo, int nValue)
		{
			if (nValue >= 0)
			{
				MC.SetParam(channel[nCamNo], "TrigDelay_us", nValue);
			}
		}
		#endregion

		#region /// CCLink Module ///
		private void bwCCLinkEcho_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				bool bit = false;
				//short[] nCommand = new short[2];
				while (true)
				{
					// Echo
					bit = (bit == true) ? false : true;
					ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, 0x100, bit);
					System.Threading.Thread.Sleep(1000);
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[bwCCLinkEcho_DoWork] " + exc.ToString());
			}
		}

		private void bwCCLink_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				short[] nCommand = new short[6];
				while (true)
				{
					// 검사 제품 모델No.(1 ~ 300)
					ctlCom.dataReceive(ctlCom.nPath, ctlCom.nStationNo, CCLink.DEVTYPE_D, 0, 1, nCommand);
					if (nCommand[0] > 0 && nCommand[0] != paramIni.LastModelNo)
						LoadConfig(nCommand[0]);

					ctlCom.dataReceive(ctlCom.nPath, ctlCom.nStationNo, CCLink.DEVTYPE_W, 0x10, 3, nCommand);
					// PLC Manual Mode시 bit 1 set.
					if (nCommand[0] > 0 && bAutoStart == true)
						chkStart_CheckedChanged(null, null);
					// PLC Auto Mode시 bit 1 set.
					if (nCommand[1] > 0 && bAutoStart == false)
						chkStart_CheckedChanged(null, null);
					// PLC Reset 버튼 bit 1 set.
					if (nCommand[2] > 0 && nNGCount1 + nOKCount2 + nNGCount2_1 + nNGCount2_2 > 0)
						btnReset_Click(null, null);

					System.Threading.Thread.Sleep(500);
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[bwCCLink_DoWork] " + exc.ToString());
			}
		}
		#endregion

		#region /// Inspection Module ///

		#region /// Auto Inspection Main Module ///
		struct paramInspectionThread
		{
			public int nCamNo;
			public int nGrabLoopCnt;
		};

		void GrabEndProcess(int nCamNo, Bitmap bmImage)
		{
			try
			{
				short[] nItemIndex = new short[2];
				ctlCom.dataReceive(ctlCom.nPath, ctlCom.nStationNo, CCLink.DEVTYPE_W, (short)(nCamNo == MAX_CAMERA_CNT - 1 ? 2 : 0), (short)nItemIndex.Length, nItemIndex);
				int nGrabLoopCnt = this.nGrabLoopCnt[nCamNo] = nItemIndex[0];

				if (nCamNo == MAX_CAMERA_CNT - 1)
				{
					Bitmap temp = bmImage;
					Bitmap tbmp = Basic.BitmapResizeBilinear(bmImage);
					bmImage = tbmp;
					temp.Dispose();
				}

				if (bAutoStart == true)
				{
					if (lstImage[nCamNo, nGrabLoopCnt % 4] != null)
						lstImage[nCamNo, nGrabLoopCnt % 4].Dispose();
					lstImage[nCamNo, nGrabLoopCnt % 4] = bmImage;

					paramInspectionThread param = new paramInspectionThread();
					param.nCamNo = nCamNo;
					param.nGrabLoopCnt = (short)nGrabLoopCnt;
					if (nCamNo < MAX_CAMERA_CNT - 1)
					{
						Thread threadInspection = new Thread(InspectionThread_Run_1);
						threadInspection.Start(param);
					}
					else
					{
						Thread threadInspection = new Thread(InspectionThread_Run_2);
						threadInspection.Start(param);
					}
				}
				else
				{
					if (lstImage[nCamNo, nGrabLoopCnt % 4] != null)
						lstImage[nCamNo, nGrabLoopCnt % 4].Dispose();
					lstImage[nCamNo, nGrabLoopCnt % 4] = bmImage;

					// Display the new image
					Bitmap bmp1 = ivCamViewer[nCamNo].Image;
					Bitmap bmp2 = ivMainViewer[nCamNo].Image;
					ivCamViewer[nCamNo].Image = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
					ivCamViewer[nCamNo].Invalidate();
					ivMainViewer[nCamNo].Image = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
					ivMainViewer[nCamNo].Invalidate();
					if (bmp1 != null)
						bmp1.Dispose();
					if (bmp2 != null)
						bmp2.Dispose();
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[GrabEndProcess] " + exc.ToString());
			}
		}

		private void InspectionThread_Run_1(object lParam)
		{
			try
			{
				int nCamNo = ((paramInspectionThread)lParam).nCamNo;
				int nGrabLoopCnt = ((paramInspectionThread)lParam).nGrabLoopCnt;

				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return;

				if (++nCaptureCnt < 4)
					return;
				nCaptureCnt = 0;

				Stopwatch sw = new Stopwatch();
				sw.Reset();
				sw.Start();

				SwapImage(nGrabLoopCnt);

				// 41, 44 구분을 반드시 해야 할 경우, 해당하는 파이의 제품만 표면검사를 수행하고, 나머지는 상부 검사로 넘긴다.
				bool bGubunInspect4144 = false;
				if (ModelInfo.GubunInspect4144 > 0)
					bGubunInspect4144 = Gubun4144Pi(nGrabLoopCnt, ModelInfo.GubunInspect4144);

				if (bGubunInspect4144 == false)
				{
					nInspectResult[0, nGrabLoopCnt] = Inspection1(0, nGrabLoopCnt, false);

					DisplayResultLabel(lblResult1, nInspectResult[0, nGrabLoopCnt]);
					DisplayResultLabel(lblResultDetail1, nInspectResult[0, nGrabLoopCnt]);
					if (nInspectResult[0, nGrabLoopCnt] == 1)
					{
						txtOKCnt1.SetTextInt(txtOKCnt1.GetTextInt() + 1);
						txtOKCntDetail1.SetTextInt(txtOKCnt1.GetTextInt());
					}
					else
					{
						txtNGCnt1.SetTextInt(txtNGCnt1.GetTextInt() + 1);
						txtNGCntDetail1.SetTextInt(txtNGCnt1.GetTextInt());
					}
					nInspectResult[1, nGrabLoopCnt] = Inspection2(1, nGrabLoopCnt, false);

					DisplayResultLabel(lblResult2, nInspectResult[1, nGrabLoopCnt]);
					DisplayResultLabel(lblResultDetail2, nInspectResult[1, nGrabLoopCnt]);
					if (nInspectResult[1, nGrabLoopCnt] == 1)
					{
						txtOKCnt2.SetTextInt(txtOKCnt2.GetTextInt() + 1);
						txtOKCntDetail2.SetTextInt(txtOKCnt2.GetTextInt());
					}
					else
					{
						txtNGCnt2.SetTextInt(txtNGCnt2.GetTextInt() + 1);
						txtNGCntDetail2.SetTextInt(txtNGCnt2.GetTextInt());
					}
					nInspectResult[2, nGrabLoopCnt] = Inspection3(2, nGrabLoopCnt, false);

					DisplayResultLabel(lblResult3, nInspectResult[2, nGrabLoopCnt]);
					DisplayResultLabel(lblResultDetail3, nInspectResult[2, nGrabLoopCnt]);
					if (nInspectResult[2, nGrabLoopCnt] == 1)
					{
						txtOKCnt3.SetTextInt(txtOKCnt3.GetTextInt() + 1);
						txtOKCntDetail3.SetTextInt(txtOKCnt3.GetTextInt());
					}
					else
					{
						txtNGCnt3.SetTextInt(txtNGCnt3.GetTextInt() + 1);
						txtNGCntDetail3.SetTextInt(txtNGCnt3.GetTextInt());
					}
					nInspectResult[3, nGrabLoopCnt] = Inspection4(3, nGrabLoopCnt, false);

					DisplayResultLabel(lblResult4, nInspectResult[3, nGrabLoopCnt]);
					DisplayResultLabel(lblResultDetail4, nInspectResult[3, nGrabLoopCnt]);
					if (nInspectResult[3, nGrabLoopCnt] == 1)
					{
						txtOKCnt4.SetTextInt(txtOKCnt4.GetTextInt() + 1);
						txtOKCntDetail4.SetTextInt(txtOKCnt4.GetTextInt());
					}
					else
					{
						txtNGCnt4.SetTextInt(txtNGCnt4.GetTextInt() + 1);
						txtNGCntDetail4.SetTextInt(txtNGCnt4.GetTextInt());
					}

					SaveNGImage(0, nGrabLoopCnt);
					SaveNGImage(1, nGrabLoopCnt);
					SaveNGImage(2, nGrabLoopCnt);
					SaveNGImage(3, nGrabLoopCnt);
					SaveData(0, nGrabLoopCnt);
					SaveData(1, nGrabLoopCnt);
					SaveData(2, nGrabLoopCnt);
					SaveData(3, nGrabLoopCnt);
					IntegrateResultAndSendPLC_1(nGrabLoopCnt);
				}
				else
				{
#if false	// 일단 패스될 경우, 카운터는 증가 안 되도록 막는다.
					txtOKCnt1.SetTextInt(txtOKCnt1.GetTextInt() + 1);
					txtOKCntDetail1.SetTextInt(txtOKCnt1.GetTextInt());
					txtOKCnt2.SetTextInt(txtOKCnt2.GetTextInt() + 1);
					txtOKCntDetail2.SetTextInt(txtOKCnt2.GetTextInt());
					txtOKCnt3.SetTextInt(txtOKCnt3.GetTextInt() + 1);
					txtOKCntDetail3.SetTextInt(txtOKCnt3.GetTextInt());
					txtOKCnt4.SetTextInt(txtOKCnt4.GetTextInt() + 1);
					txtOKCntDetail4.SetTextInt(txtOKCnt4.GetTextInt());
#endif

					for (int i = 0; i < MAX_CAMERA_CNT - 1; i++)
						nInspectResult[i, nGrabLoopCnt] = 0;
					ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, (short)nGrabLoopCnt, true);
					//DisplayResult(-1);
				}
				sw.Stop();

				displayResult1.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;
				displayResult2.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;
				displayResult3.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;
				displayResult4.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

				// Display the new image
				for (int i = 0; i < 4; i++)
				{
					Bitmap bmp1 = ivCamViewer[i].Image;
					Bitmap bmp2 = ivMainViewer[i].Image;
					ivCamViewer[i].Image = (Bitmap)lstImage[i, nGrabLoopCnt % 4].Clone();
					ivCamViewer[i].Invalidate();
					ivMainViewer[i].Image = (Bitmap)lstImage[i, nGrabLoopCnt % 4].Clone();
					ivMainViewer[i].Invalidate();
					if (bmp1 != null)
						bmp1.Dispose();
					if (bmp2 != null)
						bmp2.Dispose();
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[InspectionThread_Run_1] " + exc.ToString());
			}
		}

		private static Mutex mutIntegrateResultAndSendPLC = new Mutex();
		private void IntegrateResultAndSendPLC_1(int nGrabLoopCnt)
		{
			mutIntegrateResultAndSendPLC.WaitOne();
			try
			{
				bool bIsOK = true;
				int nFind = 0;
				for (int i = 0; i < MAX_CAMERA_CNT - 1; i++)
				{
					if (nInspectResult[i, nGrabLoopCnt] != 0)
					{
						nFind++;
						if (nInspectResult[i, nGrabLoopCnt] != 1)
							bIsOK = false;
					}
				}
				if (nFind == MAX_CAMERA_CNT - 1)
				{
					if (bIsOK == true)
					{
						ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, (short)nGrabLoopCnt, true);
						//DisplayResult(1);
					}
					else
					{
						ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, (short)(nGrabLoopCnt + 8), true);
						nNGCount1++;
						DisplayResult(2);
					}
					for (int i = 0; i < MAX_CAMERA_CNT - 1; i++)
						nInspectResult[i, nGrabLoopCnt] = 0;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[IntegrateResultAndSendPLC] " + exc.ToString());
			}
			mutIntegrateResultAndSendPLC.ReleaseMutex();
		}

		private void InspectionThread_Run_2(object lParam)
		{
			try
			{
				int nCamNo = ((paramInspectionThread)lParam).nCamNo;
				int nGrabLoopCnt = ((paramInspectionThread)lParam).nGrabLoopCnt;

				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return;

				Stopwatch sw = new Stopwatch();
				sw.Reset();
				sw.Start();

				nInspectResult[nCamNo, nGrabLoopCnt] = Inspection5(nCamNo, nGrabLoopCnt, false);

				DisplayResultLabel(lblResult5, nInspectResult[nCamNo, nGrabLoopCnt]);
				DisplayResultLabel(lblResultDetail5, nInspectResult[nCamNo, nGrabLoopCnt]);
				if (nInspectResult[nCamNo, nGrabLoopCnt] == 1)
				{
					txtOKCnt5.SetTextInt(txtOKCnt5.GetTextInt() + 1);
					txtOKCntDetail5.SetTextInt(txtOKCnt5.GetTextInt());
				}
				else if (nInspectResult[nCamNo, nGrabLoopCnt] == 2)
				{
					txtNGCnt5_1.SetTextInt(txtNGCnt5_1.GetTextInt() + 1);
					txtNGCntDetail5_1.SetTextInt(txtNGCnt5_1.GetTextInt());
				}
				else
				{
					txtNGCnt5_2.SetTextInt(txtNGCnt5_2.GetTextInt() + 1);
					txtNGCntDetail5_2.SetTextInt(txtNGCnt5_2.GetTextInt());
				}

				//nTotalCount++;
				if (nInspectResult[nCamNo, nGrabLoopCnt] == 1)
				{
					ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, (short)(nGrabLoopCnt + 0x10), true);
					nOKCount2++;
					DisplayResult(1);
				}
				else if (nInspectResult[nCamNo, nGrabLoopCnt] == 2)
				{
					ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, (short)(nGrabLoopCnt + 0x30), true);
					nNGCount2_1++;
					DisplayResult(2);
				}
				else
				{
					ctlCom.dataSendBit(ctlCom.nPath, ctlCom.nStationNo, (short)(nGrabLoopCnt + 0x50), true);
					nNGCount2_2++;
					DisplayResult(2);
				}
				sw.Stop();
				displayResult5.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

				SaveNGImage(nCamNo, nGrabLoopCnt);
				SaveData(nCamNo, nGrabLoopCnt);
				nInspectResult[nCamNo, nGrabLoopCnt] = 0;

				// Display the new image
				Bitmap bmp1 = ivCamViewer[nCamNo].Image;
				Bitmap bmp2 = ivMainViewer[nCamNo].Image;
				ivCamViewer[nCamNo].Image = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				ivCamViewer[nCamNo].Invalidate();
				Bitmap bmp3 = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				//ivMainViewer[nCamNo].UseFiltering = false;
				ivMainViewer[nCamNo].Image = bmp3;
				ivMainViewer[nCamNo].Invalidate();
				if (bmp1 != null)
					bmp1.Dispose();
				if (bmp2 != null)
					bmp2.Dispose();
				GC.Collect();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[InspectionThread_Run_2] " + exc.ToString());
			}
		}
		#endregion

		#region /// Main Inspection Logic ///
		private bool SwapImage(int nGrabLoopCnt)
		{
			try
			{
				if (lstImage[0, nGrabLoopCnt % 4] == null)
					return false;

				BLOBINFO[] info = new BLOBINFO[1000];
				BLOBOPTION option = new BLOBOPTION();

				Bitmap bmp = lstImage[0, nGrabLoopCnt % 4].Clone(ModelInfo.Cam1_Data.AlignROI, lstImage[0, nGrabLoopCnt % 4].PixelFormat);
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam1_Data.Threshold);
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 1000;
				int blobcount = blob.Labeling(ref option, info);

				int nLeft = ModelInfo.Cam1_Data.AlignROI.Width;
				int nRight = 0;
				for (int k = 0; k < blobcount; k++)
				{
					if (info[k].rcObj.Rect.Left < nLeft)
						nLeft = info[k].rcObj.Rect.Left;
					if (info[k].rcObj.Rect.Right > nRight)
						nRight = info[k].rcObj.Rect.Right;
				}
				bmp.Dispose();

				if ((nRight - nLeft) < 550)
				{
					bItemInverse = true;
					Bitmap temp = lstImage[0, nGrabLoopCnt % 4];
					lstImage[0, nGrabLoopCnt % 4] = lstImage[1, nGrabLoopCnt % 4];
					lstImage[1, nGrabLoopCnt % 4] = temp;
					lstImage[0, nGrabLoopCnt % 4].RotateFlip(RotateFlipType.Rotate180FlipY);
					lstImage[1, nGrabLoopCnt % 4].RotateFlip(RotateFlipType.Rotate180FlipY);

					temp = lstImage[2, nGrabLoopCnt % 4];
					lstImage[2, nGrabLoopCnt % 4] = lstImage[3, nGrabLoopCnt % 4];
					lstImage[3, nGrabLoopCnt % 4] = temp;
					lstImage[2, nGrabLoopCnt % 4].RotateFlip(RotateFlipType.Rotate180FlipY);
					lstImage[3, nGrabLoopCnt % 4].RotateFlip(RotateFlipType.Rotate180FlipY);
				}
				else
					bItemInverse = false;
				return true;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SwapImage] " + exc.ToString());
				return false;
			}
		}

		private bool Gubun4144Pi(int nGrabLoopCnt, int gubun)
		{
			try
			{
				if (lstImage[1, nGrabLoopCnt % 4] == null)
					return false;

				BLOBINFO[] info = new BLOBINFO[1000];
				BLOBOPTION option = new BLOBOPTION();

				Bitmap bmp = lstImage[1, nGrabLoopCnt % 4].Clone(ModelInfo.Cam1_Data.AlignROI, lstImage[1, nGrabLoopCnt % 4].PixelFormat);
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam1_Data.Threshold);
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 1000;
				int blobcount = blob.Labeling(ref option, info);

				Rectangle rect = new Rectangle();
				int max = 0;
				for (int k = 0; k < blobcount; k++)
				{
					if (info[k].rcObj.Rect.Left > 2 &&
						info[k].rcObj.Rect.Top > 2 &&
						info[k].rcObj.Rect.Right < bmp.Width - 2 &&
						info[k].nObjArea > max)
					{
						rect = info[k].rcObj.Rect;
						max = info[k].nObjArea;
					}
				}
				bmp.Dispose();

				if (gubun == 1)	// 41파이만 표면검사 수행함.
				{
					if (rect.Width < 465)
						return false;	// false면 표면검사 수행함.
					else
						return true;
				}
				else if (gubun == 2)
				{
					if (rect.Width > 465)
						return false;
					else
						return true;
				}
				else
					return false;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SwapImage] " + exc.ToString());
				return false;
			}
		}

		private int Inspection1(int nCamNo, int nGrabLoopCnt, bool bIsManual)
		{
			try
			{
				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return -1;

				displayResult1.lstErrRect.Clear();
				displayResult1.lstErrArea.Clear();
				displayResult1.bResult = false;

				if (ModelInfo.Cam1_Data.UseInspect == false)
					return 1;

				BLOBINFO[] info = new BLOBINFO[1000];
				BLOBOPTION option = new BLOBOPTION();

				#region // 얼라인 잡기 //
				{
					Rectangle rtAlign1 = new Rectangle();
					Point ptAlign1 = new Point();
					Bitmap bmpAlign = Basic.BitmapClone(lstImage[nCamNo, nGrabLoopCnt % 4], ModelInfo.Cam1_Data.AlignROI);
					Basic.Thresholding(bmpAlign, bmpAlign, ModelInfo.Cam1_Data.Threshold);
					Blob blob1 = new Blob(bmpAlign);
					option.nObjBufSize = 1000;
					int blobcount1 = blob1.Labeling(ref option, info);

					for (int i = 0; i < blobcount1; i++)
					{
						if (info[i].nObjArea > 100)
						{
							if (rtAlign1.IsEmpty == true)
								rtAlign1 = info[i].rcObj.Rect;
							else
							{
								if (info[i].rcObj.Rect.Left < rtAlign1.Left)
								{
									rtAlign1.Width += rtAlign1.Left - info[i].rcObj.Rect.Left;
									rtAlign1.X -= rtAlign1.Left - info[i].rcObj.Rect.Left;
								}
								if (info[i].rcObj.Rect.Top < rtAlign1.Top)
								{
									rtAlign1.Height += rtAlign1.Top - info[i].rcObj.Rect.Top;
									rtAlign1.Y -= rtAlign1.Top - info[i].rcObj.Rect.Top;
								}
								if (info[i].rcObj.Rect.Right > rtAlign1.Right)
									rtAlign1.Width += info[i].rcObj.Rect.Right - rtAlign1.Right;
								if (info[i].rcObj.Rect.Bottom > rtAlign1.Bottom)
									rtAlign1.Height += info[i].rcObj.Rect.Bottom - rtAlign1.Bottom;
							}
						}
					}
					bmpAlign.Dispose();
					if (rtAlign1.IsEmpty == true)
						return -1;

					rtAlign1.Offset(ModelInfo.Cam1_Data.AlignROI.Location);
					ptAlign1 = new Point((rtAlign1.Left + rtAlign1.Right) / 2, rtAlign1.Top);

					if (ptAlign1.IsEmpty == false)
					{
						if (ModelInfo.Cam1_Data.AlignCenter.IsEmpty == false)
						{
							int nWidthGap = ptAlign1.X - ModelInfo.Cam1_Data.AlignCenter.X;
							int nHeightGap = ptAlign1.Y - ModelInfo.Cam1_Data.AlignCenter.Y;

							Rectangle rt = ModelInfo.Cam1_Data.ROI;
							rt.Offset(nWidthGap, nHeightGap);
							ModelInfo.Cam1_Data.ROI = rt;

							for (int i = 0; i < ModelInfo.Cam1_Data.Masks.Count; i++)
							{
								rt = ModelInfo.Cam1_Data.Masks[i].ROI;
								rt.Offset(nWidthGap, nHeightGap);
								ModelInfo.Cam1_Data.Masks[i].ROI = rt;
							}
						}
						ModelInfo.Cam1_Data.AlignCenter = ptAlign1;
					}
				}
				#endregion

				double dbDefectWidth = ModelInfo.Cam1_Data.DefectWidthMax;
				double dbDefectHeight = ModelInfo.Cam1_Data.DefectHeightMax;
				if (paramIni.CamInfoCol[0].MmPerPixel > 0)
				{
					dbDefectWidth /= paramIni.CamInfoCol[0].MmPerPixel;
					dbDefectHeight /= paramIni.CamInfoCol[0].MmPerPixel;
				}

				Bitmap org = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				BitmapData bdOrg = org.LockBits(new Rectangle(new Point(0, 0), org.Size),
										ImageLockMode.ReadWrite,
										org.PixelFormat);
				unsafe
				{
					int orgStride = bdOrg.Stride;
					byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();

					foreach (paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
					{
						Rectangle rect = Basic.CheckRoi(mask.ROI, org);
						for (int y = rect.Top; y < rect.Bottom; y++)
							for (int x = rect.Left; x < rect.Right; x++)
								pOrg[y * orgStride + x] = 0;
					}
				}
				org.UnlockBits(bdOrg);

				Bitmap bmp = Basic.BitmapClone(org, ModelInfo.Cam1_Data.ROI);
				//Bitmap bmp = org.Clone(ModelInfo.Cam1_Data.ROI, org.PixelFormat);
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam1_Data.Threshold);
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 1000;
				int blobcount = blob.Labeling(ref option, info);

				for (int k = 0; k < blobcount; k++)
				{
					//if (info[k].rcObj.Rect.Left > 2 &&
					//	info[k].rcObj.Rect.Top > 2 &&
					//	info[k].rcObj.Rect.Right < bmp.Width - 2 &&
					//	info[k].rcObj.Rect.Bottom < bmp.Height - 2)
					//{
						if (info[k].rcObj.Rect.Width > dbDefectWidth &&
							info[k].rcObj.Rect.Height > dbDefectHeight)
						{
							Rectangle rect = info[k].rcObj.Rect;
							rect.Offset(ModelInfo.Cam1_Data.ROI.Location);
							displayResult1.lstErrRect.Add(rect);
							displayResult1.lstErrArea.Add(info[k].nObjArea);
						}
					//}
				}

				if (displayResult1.lstErrRect.Count == 0)
					displayResult1.bResult = true;
				else
					displayResult1.bResult = false;
				bmp.Dispose();
				org.Dispose();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[Inspection1] " + exc.ToString());
				displayResult1.bResult = false;
				return -1;
			}
			if (displayResult1.bResult == true)
				return 1;
			else
				return 2;
		}

		private int Inspection2(int nCamNo, int nGrabLoopCnt, bool bIsManual)
		{
			try
			{
				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return -1;

				displayResult2.lstErrRect.Clear();
				displayResult2.lstErrArea.Clear();
				displayResult2.bResult = false;

				if (ModelInfo.Cam2_Data.UseInspect == false)
					return 1;

				BLOBINFO[] info = new BLOBINFO[1000];
				BLOBOPTION option = new BLOBOPTION();

				#region // 얼라인 잡기 //
				{
					Rectangle rtAlign1 = new Rectangle();
					Point ptAlign1 = new Point();
					Bitmap bmpAlign = Basic.BitmapClone(lstImage[nCamNo, nGrabLoopCnt % 4], ModelInfo.Cam2_Data.AlignROI);
					Basic.Thresholding(bmpAlign, bmpAlign, ModelInfo.Cam2_Data.Threshold);
					Blob blob1 = new Blob(bmpAlign);
					option.nObjBufSize = 1000;
					int blobcount1 = blob1.Labeling(ref option, info);

					for (int i = 0; i < blobcount1; i++)
					{
						if (info[i].nObjArea > 100)
						{
							if (rtAlign1.IsEmpty == true)
								rtAlign1 = info[i].rcObj.Rect;
							else
							{
								if (info[i].rcObj.Rect.Left < rtAlign1.Left)
								{
									rtAlign1.Width += rtAlign1.Left - info[i].rcObj.Rect.Left;
									rtAlign1.X -= rtAlign1.Left - info[i].rcObj.Rect.Left;
								}
								if (info[i].rcObj.Rect.Top < rtAlign1.Top)
								{
									rtAlign1.Height += rtAlign1.Top - info[i].rcObj.Rect.Top;
									rtAlign1.Y -= rtAlign1.Top - info[i].rcObj.Rect.Top;
								}
								if (info[i].rcObj.Rect.Right > rtAlign1.Right)
									rtAlign1.Width += info[i].rcObj.Rect.Right - rtAlign1.Right;
								if (info[i].rcObj.Rect.Bottom > rtAlign1.Bottom)
									rtAlign1.Height += info[i].rcObj.Rect.Bottom - rtAlign1.Bottom;
							}
						}
					}
					bmpAlign.Dispose();
					if (rtAlign1.IsEmpty == true)
						return -1;

					rtAlign1.Offset(ModelInfo.Cam2_Data.AlignROI.Location);
					ptAlign1 = new Point((rtAlign1.Left + rtAlign1.Right) / 2, rtAlign1.Top);

					if (ptAlign1.IsEmpty == false)
					{
						if (ModelInfo.Cam2_Data.AlignCenter.IsEmpty == false)
						{
							int nWidthGap = ptAlign1.X - ModelInfo.Cam2_Data.AlignCenter.X;
							int nHeightGap = ptAlign1.Y - ModelInfo.Cam2_Data.AlignCenter.Y;

							Rectangle rt = ModelInfo.Cam2_Data.ROI;
							rt.Offset(nWidthGap, nHeightGap);
							ModelInfo.Cam2_Data.ROI = rt;

							for (int i = 0; i < ModelInfo.Cam2_Data.Masks.Count; i++)
							{
								rt = ModelInfo.Cam2_Data.Masks[i].ROI;
								rt.Offset(nWidthGap, nHeightGap);
								ModelInfo.Cam2_Data.Masks[i].ROI = rt;
							}
						}
						ModelInfo.Cam2_Data.AlignCenter = ptAlign1;
					}
				}
				#endregion

				double dbDefectWidth = ModelInfo.Cam2_Data.DefectWidthMax;
				double dbDefectHeight = ModelInfo.Cam2_Data.DefectHeightMax;
				if (paramIni.CamInfoCol[0].MmPerPixel > 0)
				{
					dbDefectWidth /= paramIni.CamInfoCol[0].MmPerPixel;
					dbDefectHeight /= paramIni.CamInfoCol[0].MmPerPixel;
				}

				Bitmap org = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				BitmapData bdOrg = org.LockBits(new Rectangle(new Point(0, 0), org.Size),
										ImageLockMode.ReadWrite,
										org.PixelFormat);
				unsafe
				{
					int orgStride = bdOrg.Stride;
					byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();

					foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
					{
						Rectangle rect = Basic.CheckRoi(mask.ROI, org);
						for (int y = rect.Top; y < rect.Bottom; y++)
							for (int x = rect.Left; x < rect.Right; x++)
								pOrg[y * orgStride + x] = 0;
					}
				}
				org.UnlockBits(bdOrg);

				Bitmap bmp = null;
				try
				{
					bmp = Basic.BitmapClone(org, ModelInfo.Cam2_Data.ROI);
				}
				catch
				{
					Log.AddLogMessage(Log.LogType.ERROR, 0, ModelInfo.Cam2_Data.ROI.ToString());
				}
				//Bitmap bmp = org.Clone(ModelInfo.Cam2_Data.ROI, org.PixelFormat);
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam2_Data.Threshold);
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 1000;
				int blobcount = blob.Labeling(ref option, info);

				for (int k = 0; k < blobcount; k++)
				{
					//if (info[k].rcObj.Rect.Left > 2 &&
					//	info[k].rcObj.Rect.Top > 2 &&
					//	info[k].rcObj.Rect.Right < bmp.Width - 2 &&
					//	info[k].rcObj.Rect.Bottom < bmp.Height - 2)
					//{
						if (info[k].rcObj.Rect.Width > dbDefectWidth &&
							info[k].rcObj.Rect.Height > dbDefectHeight)
						{
							Rectangle rect = info[k].rcObj.Rect;
							rect.Offset(ModelInfo.Cam2_Data.ROI.Location);
							displayResult2.lstErrRect.Add(rect);
							displayResult2.lstErrArea.Add(info[k].nObjArea);
						}
					//}
				}

				if (displayResult2.lstErrRect.Count == 0)
					displayResult2.bResult = true;
				else
					displayResult2.bResult = false;
				bmp.Dispose();
				org.Dispose();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[Inspection2] " + exc.ToString());
				return -1;
			}
			if (displayResult2.bResult == true)
				return 1;
			else
				return 2;
		}

		private int Inspection3(int nCamNo, int nGrabLoopCnt, bool bIsManual)
		{
			try
			{
				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return -1;

				displayResult3.lstErrRect.Clear();
				displayResult3.lstErrArea.Clear();
				displayResult3.bResult = false;

				if (ModelInfo.Cam3_Data.UseInspect == false)
					return 1;

				BLOBINFO[] info = new BLOBINFO[1000];
				BLOBOPTION option = new BLOBOPTION();

				#region // 얼라인 잡기 //
				{
					Rectangle rtAlign1 = new Rectangle();
					Point ptAlign1 = new Point();
					Bitmap bmpAlign = Basic.BitmapClone(lstImage[nCamNo, nGrabLoopCnt % 4], ModelInfo.Cam3_Data.AlignROI);
					Basic.Thresholding(bmpAlign, bmpAlign, ModelInfo.Cam3_Data.Threshold);
					Blob blob1 = new Blob(bmpAlign);
					option.nObjBufSize = 1000;
					int blobcount1 = blob1.Labeling(ref option, info);

					for (int i = 0; i < blobcount1; i++)
					{
						if (info[i].nObjArea > 100)
						{
							if (rtAlign1.IsEmpty == true)
								rtAlign1 = info[i].rcObj.Rect;
							else
							{
								if (info[i].rcObj.Rect.Left < rtAlign1.Left)
								{
									rtAlign1.Width += rtAlign1.Left - info[i].rcObj.Rect.Left;
									rtAlign1.X -= rtAlign1.Left - info[i].rcObj.Rect.Left;
								}
								if (info[i].rcObj.Rect.Top < rtAlign1.Top)
								{
									rtAlign1.Height += rtAlign1.Top - info[i].rcObj.Rect.Top;
									rtAlign1.Y -= rtAlign1.Top - info[i].rcObj.Rect.Top;
								}
								if (info[i].rcObj.Rect.Right > rtAlign1.Right)
									rtAlign1.Width += info[i].rcObj.Rect.Right - rtAlign1.Right;
								if (info[i].rcObj.Rect.Bottom > rtAlign1.Bottom)
									rtAlign1.Height += info[i].rcObj.Rect.Bottom - rtAlign1.Bottom;
							}
						}
					}
					bmpAlign.Dispose();
					if (rtAlign1.IsEmpty == true)
						return -1;

					rtAlign1.Offset(ModelInfo.Cam3_Data.AlignROI.Location);
					ptAlign1 = new Point((rtAlign1.Left + rtAlign1.Right) / 2, rtAlign1.Top);

					if (ptAlign1.IsEmpty == false)
					{
						if (ModelInfo.Cam3_Data.AlignCenter.IsEmpty == false)
						{
							int nWidthGap = ptAlign1.X - ModelInfo.Cam3_Data.AlignCenter.X;
							int nHeightGap = ptAlign1.Y - ModelInfo.Cam3_Data.AlignCenter.Y;

							Rectangle rt = ModelInfo.Cam3_Data.ROI;
							rt.Offset(nWidthGap, nHeightGap);
							ModelInfo.Cam3_Data.ROI = rt;

							for (int i = 0; i < ModelInfo.Cam3_Data.Masks.Count; i++)
							{
								rt = ModelInfo.Cam3_Data.Masks[i].ROI;
								rt.Offset(nWidthGap, nHeightGap);
								ModelInfo.Cam3_Data.Masks[i].ROI = rt;
							}
						}
						ModelInfo.Cam3_Data.AlignCenter = ptAlign1;
					}
				}
				#endregion

				double dbDefectWidth = ModelInfo.Cam3_Data.DefectWidthMax;
				double dbDefectHeight = ModelInfo.Cam3_Data.DefectHeightMax;
				if (paramIni.CamInfoCol[0].MmPerPixel > 0)
				{
					dbDefectWidth /= paramIni.CamInfoCol[0].MmPerPixel;
					dbDefectHeight /= paramIni.CamInfoCol[0].MmPerPixel;
				}

				Bitmap org = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				BitmapData bdOrg = org.LockBits(new Rectangle(new Point(0, 0), org.Size),
										ImageLockMode.ReadWrite,
										org.PixelFormat);
				unsafe
				{
					int orgStride = bdOrg.Stride;
					byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();

					foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
					{
						Rectangle rect = Basic.CheckRoi(mask.ROI, org);
						for (int y = rect.Top; y < rect.Bottom; y++)
							for (int x = rect.Left; x < rect.Right; x++)
								pOrg[y * orgStride + x] = 0;
					}
				}
				org.UnlockBits(bdOrg);

				Bitmap bmp = null;
				try
				{
					bmp = Basic.BitmapClone(org, ModelInfo.Cam3_Data.ROI);
				}
				catch
				{
					Log.AddLogMessage(Log.LogType.ERROR, 0, ModelInfo.Cam3_Data.ROI.ToString());
				}
				//Bitmap bmp = org.Clone(ModelInfo.Cam3_Data.ROI, org.PixelFormat);
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam3_Data.Threshold);
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 1000;
				int blobcount = blob.Labeling(ref option, info);

				for (int k = 0; k < blobcount; k++)
				{
					//if (info[k].rcObj.Rect.Left > 2 &&
					//	info[k].rcObj.Rect.Top > 2 &&
					//	info[k].rcObj.Rect.Right < bmp.Width - 2 &&
					//	info[k].rcObj.Rect.Bottom < bmp.Height - 2)
					//{
						if (info[k].rcObj.Rect.Width > dbDefectWidth &&
							info[k].rcObj.Rect.Height > dbDefectHeight)
						{
							Rectangle rect = info[k].rcObj.Rect;
							rect.Offset(ModelInfo.Cam3_Data.ROI.Location);
							displayResult3.lstErrRect.Add(rect);
							displayResult3.lstErrArea.Add(info[k].nObjArea);
						}
					//}
				}

				if (displayResult3.lstErrRect.Count == 0)
					displayResult3.bResult = true;
				else
					displayResult3.bResult = false;
				bmp.Dispose();
				org.Dispose();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[Inspection3] " + exc.ToString());
				displayResult3.bResult = false;
				return -1;
			}
			if (displayResult3.bResult == true)
				return 1;
			else
				return 2;
		}

		private int Inspection4(int nCamNo, int nGrabLoopCnt, bool bIsManual)
		{
			try
			{
				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return -1;

				displayResult4.lstErrRect.Clear();
				displayResult4.lstErrArea.Clear();
				displayResult4.bResult = false;

				if (ModelInfo.Cam4_Data.UseInspect == false)
					return 1;

				BLOBINFO[] info = new BLOBINFO[1000];
				BLOBOPTION option = new BLOBOPTION();

				#region // 얼라인 잡기 //
				{
					Rectangle rtAlign1 = new Rectangle();
					Point ptAlign1 = new Point();
					Bitmap bmpAlign = Basic.BitmapClone(lstImage[nCamNo, nGrabLoopCnt % 4], ModelInfo.Cam4_Data.AlignROI);
					Basic.Thresholding(bmpAlign, bmpAlign, ModelInfo.Cam4_Data.Threshold);
					Blob blob1 = new Blob(bmpAlign);
					option.nObjBufSize = 1000;
					int blobcount1 = blob1.Labeling(ref option, info);

					for (int i = 0; i < blobcount1; i++)
					{
						if (info[i].nObjArea > 100)
						{
							if (rtAlign1.IsEmpty == true)
								rtAlign1 = info[i].rcObj.Rect;
							else
							{
								if (info[i].rcObj.Rect.Left < rtAlign1.Left)
								{
									rtAlign1.Width += rtAlign1.Left - info[i].rcObj.Rect.Left;
									rtAlign1.X -= rtAlign1.Left - info[i].rcObj.Rect.Left;
								}
								if (info[i].rcObj.Rect.Top < rtAlign1.Top)
								{
									rtAlign1.Height += rtAlign1.Top - info[i].rcObj.Rect.Top;
									rtAlign1.Y -= rtAlign1.Top - info[i].rcObj.Rect.Top;
								}
								if (info[i].rcObj.Rect.Right > rtAlign1.Right)
									rtAlign1.Width += info[i].rcObj.Rect.Right - rtAlign1.Right;
								if (info[i].rcObj.Rect.Bottom > rtAlign1.Bottom)
									rtAlign1.Height += info[i].rcObj.Rect.Bottom - rtAlign1.Bottom;
							}
						}
					}
					bmpAlign.Dispose();
					if (rtAlign1.IsEmpty == true)
						return -1;

					rtAlign1.Offset(ModelInfo.Cam4_Data.AlignROI.Location);
					ptAlign1 = new Point((rtAlign1.Left + rtAlign1.Right) / 2, rtAlign1.Top);

					if (ptAlign1.IsEmpty == false)
					{
						if (ModelInfo.Cam4_Data.AlignCenter.IsEmpty == false)
						{
							int nWidthGap = ptAlign1.X - ModelInfo.Cam4_Data.AlignCenter.X;
							int nHeightGap = ptAlign1.Y - ModelInfo.Cam4_Data.AlignCenter.Y;

							Rectangle rt = ModelInfo.Cam4_Data.ROI;
							rt.Offset(nWidthGap, nHeightGap);
							ModelInfo.Cam4_Data.ROI = rt;

							for (int i = 0; i < ModelInfo.Cam4_Data.Masks.Count; i++)
							{
								rt = ModelInfo.Cam4_Data.Masks[i].ROI;
								rt.Offset(nWidthGap, nHeightGap);
								ModelInfo.Cam4_Data.Masks[i].ROI = rt;
							}
						}
						ModelInfo.Cam4_Data.AlignCenter = ptAlign1;
					}
				}
				#endregion

				double dbDefectWidth = ModelInfo.Cam4_Data.DefectWidthMax;
				double dbDefectHeight = ModelInfo.Cam4_Data.DefectHeightMax;
				if (paramIni.CamInfoCol[0].MmPerPixel > 0)
				{
					dbDefectWidth /= paramIni.CamInfoCol[0].MmPerPixel;
					dbDefectHeight /= paramIni.CamInfoCol[0].MmPerPixel;
				}

				Bitmap org = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				BitmapData bdOrg = org.LockBits(new Rectangle(new Point(0, 0), org.Size),
										ImageLockMode.ReadWrite,
										org.PixelFormat);
				unsafe
				{
					int orgStride = bdOrg.Stride;
					byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();

					foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
					{
						Rectangle rect = Basic.CheckRoi(mask.ROI, org);
						for (int y = rect.Top; y < rect.Bottom; y++)
							for (int x = rect.Left; x < rect.Right; x++)
								pOrg[y * orgStride + x] = 0;
					}
				}
				org.UnlockBits(bdOrg);

				Bitmap bmp = Basic.BitmapClone(org, ModelInfo.Cam4_Data.ROI);
				//Bitmap bmp = org.Clone(ModelInfo.Cam4_Data.ROI, org.PixelFormat);
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam4_Data.Threshold);
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 1000;
				int blobcount = blob.Labeling(ref option, info);

				for (int k = 0; k < blobcount; k++)
				{
					//if (info[k].rcObj.Rect.Left > 2 &&
					//	info[k].rcObj.Rect.Top > 2 &&
					//	info[k].rcObj.Rect.Right < bmp.Width - 2 &&
					//	info[k].rcObj.Rect.Bottom < bmp.Height - 2)
					//{
						if (info[k].rcObj.Rect.Width > dbDefectWidth &&
							info[k].rcObj.Rect.Height > dbDefectHeight)
						{
							Rectangle rect = info[k].rcObj.Rect;
							rect.Offset(ModelInfo.Cam4_Data.ROI.Location);
							displayResult4.lstErrRect.Add(rect);
							displayResult4.lstErrArea.Add(info[k].nObjArea);
						}
					//}
				}

				if (displayResult4.lstErrRect.Count == 0)
					displayResult4.bResult = true;
				else
					displayResult4.bResult = false;
				bmp.Dispose();
				org.Dispose();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[Inspection4] " + exc.ToString());
				return -1;
			}
			if (displayResult4.bResult == true)
				return 1;
			else
				return 2;
		}

		private int Inspection5(int nCamNo, int nGrabLoopCnt, bool bIsManual)
		{
			try
			{
				if (lstImage[nCamNo, nGrabLoopCnt % 4] == null)
					return -1;

				#region // 초기화 //
				displayResult5.bResult = false;
				displayResult5.bXL = !ModelInfo.Cam5_Data.UseXLYL;
				displayResult5.bYL = !ModelInfo.Cam5_Data.UseXLYL;
				displayResult5.bLH = !ModelInfo.Cam5_Data.UseLHRH;
				displayResult5.bRH = !ModelInfo.Cam5_Data.UseLHRH;
				displayResult5.bT = !ModelInfo.Cam5_Data.UseT;
				displayResult5.bP = !ModelInfo.Cam5_Data.UseP;
				displayResult5.bT2 = !ModelInfo.Cam5_Data.UseT2;
				displayResult5.bWL = !ModelInfo.Cam5_Data.UseWL;
				displayResult5.dbXLPixel = 0;
				displayResult5.dbYLPixel = 0;
				displayResult5.dbLHPixel = 0;
				displayResult5.dbRHPixel = 0;
				displayResult5.dbTPixel = 0;
				displayResult5.dbPPixel = 0;
				displayResult5.dbT2Pixel[0] = 0;
				displayResult5.dbT2Pixel[1] = 0;
				displayResult5.dbWLPixel = 0;
				displayResult5.dbRValueReal = 0;
				displayResult5.dbXLValue = 0;
				displayResult5.dbYLValue = 0;
				displayResult5.dbLHValue = 0;
				displayResult5.dbRHValue = 0;
				displayResult5.dbTValue = 0;
				displayResult5.dbTValueMax = 0;
				displayResult5.dbTValueMin = 0;
				displayResult5.dbPValue = 0;
				displayResult5.dbT2Value[0] = 0;
				displayResult5.dbT2Value[1] = 0;
				displayResult5.dbWLValue = 0;
				displayResult5.dbRValue = 0;
				#endregion

				BLOBINFO[] info = new BLOBINFO[10000];
				BLOBOPTION option = new BLOBOPTION();

				#region // 내경 구하기 //
				Bitmap bmp = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
				Basic.Thresholding(bmp, bmp, ModelInfo.Cam5_Data.Threshold);
				//bmp.Save(@"C:\Temp\origin.bmp", ImageFormat.Bmp);
				//Bitmap aaa = (Bitmap)bmp.Clone(new Rectangle(675, 470, 20, 20), bmp.PixelFormat);
				//aaa.Save(@"C:\Temp\test.bmp", ImageFormat.Bmp);
				//aaa.Dispose();
				Blob blob = new Blob(bmp);
				option.nObjBufSize = 10000;
				int blobcount = blob.Labeling(ref option, info);

				Rectangle rectInner = new Rectangle();
				Point ptInCenter = new Point();
				int nMaxArea = 0, nSecondArea = 0, nMaxIndex = -1;
				for (int k = 0; k < blobcount; k++)
				{
					if (info[k].nObjArea > nMaxArea)
					{
						if (info[k].rcObj.Rect.Left > 5 &&
							info[k].rcObj.Rect.Top > 5 &&
							info[k].rcObj.Rect.Right < bmp.Width - 5 &&
							info[k].rcObj.Rect.Bottom < bmp.Height - 5)
						{
							if (info[k].rcObj.Rect.IntersectsWith(ModelInfo.Cam5_Data.ROI) == true)
							{
								rectInner = info[k].rcObj.Rect;
								ptInCenter = info[k].rcObj.Center.Point;
								nMaxArea = info[k].nObjArea;
								nMaxIndex = k;
							}
						}
					}
				}
				for (int k = 0; k < blobcount; k++)
				{
					if (info[k].nObjArea > nSecondArea && k != nMaxIndex)
					{
						if (info[k].rcObj.Rect.Left > 5 &&
							info[k].rcObj.Rect.Top > 5 &&
							info[k].rcObj.Rect.Right < bmp.Width - 5 &&
							info[k].rcObj.Rect.Bottom < bmp.Height - 5)
						{
							if (info[k].rcObj.Rect.IntersectsWith(ModelInfo.Cam5_Data.ROI) == true)
							{
								nSecondArea = info[k].nObjArea;
							}
						}
					}
				}
				if (nMaxArea == 0)
					return -1;
				// ador 20120618 제품이 혼입되는 경우, 모두 불량 처리하도록 수정함.
				if (nSecondArea >= (nMaxArea * 9 / 10))
					return 4;

				displayResult5.rectInner = rectInner;
				displayResult5.ptCenter = ptInCenter;
				#endregion

				double dbInHalf = (rectInner.Width + rectInner.Height) / 4.0;
				//double dbDiagonal = Math.Sqrt(Math.Pow(rectInner.Width / 2, 2) + Math.Pow(rectInner.Height / 2, 2)) - 50;
				//double dbDiagonal2 = Math.Sqrt(Math.Pow((rectInner.Width + 400) / 2, 2) + Math.Pow((rectInner.Height + 400) / 2, 2));
				double dbDiagonal = Math.Sqrt(Math.Pow(rectInner.Width / 2, 2) + Math.Pow(rectInner.Height / 2, 2)) + ModelInfo.Cam5_Data.AngleInspectPos;
				double dbDiagonal2 = Math.Sqrt(Math.Pow((rectInner.Width + 200) / 2, 2) + Math.Pow((rectInner.Height + 200) / 2, 2)) + ModelInfo.Cam5_Data.AngleInspectPos;

				int nX = 0, nY = 0;
				BitmapData bdOrg = bmp.LockBits(new Rectangle(new Point(0, 0), bmp.Size),
														ImageLockMode.ReadWrite,
														bmp.PixelFormat);

				unsafe
				{
					Point ptFirst = new Point(), ptLast = new Point();
					int width = bmp.Width, height = bmp.Height;
					int orgStride = bdOrg.Stride;
					byte* pOrg = (byte*)bdOrg.Scan0.ToPointer();

					double angle_LH = 0, angle_RH = 0;
					double arc = Math.Atan(1.0 / dbDiagonal);
					arc = arc * 180 / Math.PI;

					#region // 각도 추적을 위한 기준각도 찾기 //
					bool bIsFindLH = false;
					int nCnt = 0;
					for (double a = 0; a < 360; a += arc)
					{
						nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * a / 180));
						if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
							continue;
						if (pOrg[orgStride * nY + nX] == 0)
							nCnt++;
						else
							nCnt = 0;

						if (nCnt > 3)
						{
							nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * (a - arc * 3) / 180));
							nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * (a - arc * 3) / 180));
							if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
								continue;
							if (bIsFindLH == false)
							{
								angle_LH = CalcAngle(ptInCenter, new Point(nX, nY));
								a += 30;	// ador 20120528 경계값이 지저분할 경우 잘 찾지 못하는 문제 수정.
								bIsFindLH = true;
								nCnt = 0;
							}
							else
							{
								angle_RH = CalcAngle(ptInCenter, new Point(nX, nY));
								break;
							}
						}
					}
					if (CalcAngle(angle_LH, angle_RH, true) > CalcAngle(angle_LH, angle_RH, false))
					{
						double dbTemp = angle_LH;
						angle_LH = angle_RH;
						angle_RH = dbTemp;
					}
					#endregion

					double angle_s = angle_RH + 30;	// CalcAngle(ptOutCenter, ptInCenter);
					#region // LH, RH 위치 및 시작점 찾기 //
					Point ptLH_s = new Point();
					Point ptRH_s = new Point();
					for (double a = angle_s + arc; a < angle_s + 360; a += arc)
					{
						nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * a / 180));
						if (nX > 0 && nY > 0 && nX < width - 1 && nY < height - 1)
							break;
					}
					byte bytePreValue = pOrg[orgStride * nY + nX];
					for (double a = angle_s; a > angle_s - 360; a -= arc)
					{
						Point ptPre = new Point(nX, nY);
						nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * a / 180));
						if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
							continue;
						if (pOrg[orgStride * nY + nX] == 0)
						{
							if (bytePreValue > 0)
							{
								nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * (a + arc) / 180));
								nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * (a + arc) / 180));
								if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
									continue;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									ptRH_s.X = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * (a + arc) / 180)); ;
									ptRH_s.Y = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * (a + arc) / 180)); ;
									angle_RH = a;
									break;
								}
							}
						}
						bytePreValue = pOrg[orgStride * nY + nX];
					}
					angle_s = angle_LH - 30;
					for (double a = angle_s - arc; a > angle_s - 360; a -= arc)
					{
						nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * a / 180));
						if (nX > 0 && nY > 0 && nX < width - 1 && nY < height - 1)
							break;
					}
					bytePreValue = pOrg[orgStride * nY + nX];
					for (double a = angle_s; a < angle_s + 360; a += arc)
					{
						Point ptPre = new Point(nX, nY);
						nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * a / 180));
						if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
							continue;
						if (pOrg[orgStride * nY + nX] == 0)
						{
							if (bytePreValue > 0)
							{
								nX = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * (a - arc) / 180));
								nY = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * (a - arc) / 180));
								if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
									continue;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									ptLH_s.X = ptInCenter.X + (int)(dbDiagonal * Math.Cos(Math.PI * (a - arc) / 180)); ;
									ptLH_s.Y = ptInCenter.Y + (int)(dbDiagonal * Math.Sin(Math.PI * (a - arc) / 180)); ;
									angle_LH = a;
									break;
								}
							}
						}
						bytePreValue = pOrg[orgStride * nY + nX];
					}
					displayResult5.ptLH = ptLH_s;
					displayResult5.ptRH = ptRH_s;

					if (angle_LH > 360.0)
					{
						angle_LH -= 360.0;
						angle_RH -= 360.0;
					}
					if (angle_RH > 360.0)
					{
						angle_LH -= 360.0;
						angle_RH -= 360.0;
					}
					if (ptLH_s.IsEmpty == true || ptRH_s.IsEmpty == true)
						return -1;
					angle_s = (angle_RH + angle_LH) / 2.0;
					if (Math.Abs(angle_LH - angle_RH) > 180)
						angle_s += 180;
					displayResult5.dbLHAngle = angle_LH;
					displayResult5.dbRHAngle = angle_RH;
					displayResult5.dbCenterAngle = angle_s;
					#endregion

					double angle_s2 = angle_RH + 30;
					#region // LH_2, RH_2 위치 찾기 //
					Point ptLH_s2 = new Point();
					Point ptRH_s2 = new Point();

					for (double a = angle_s2 + arc; a < angle_s2 + 360; a += arc)
					{
						nX = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * a / 180));
						if (nX > 0 && nY > 0 && nX < width - 1 && nY < height - 1)
							break;
					}
					bytePreValue = pOrg[orgStride * nY + nX];
					for (double a = angle_s2; a > angle_s2 - 360; a -= arc)
					{
						Point ptPre = new Point(nX, nY);
						nX = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * a / 180));
						if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
							continue;
						if (pOrg[orgStride * nY + nX] == 0)
						{
							if (bytePreValue > 0)
							{
								nX = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * (a + arc) / 180));
								nY = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * (a + arc) / 180));
								if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
									continue;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									ptRH_s2.X = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * (a + arc) / 180));
									ptRH_s2.Y = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * (a + arc) / 180));
									break;
								}
							}
						}
						bytePreValue = pOrg[orgStride * nY + nX];
					}
					angle_s2 = angle_LH - 30;
					for (double a = angle_s2 - arc; a > angle_s2 - 360; a -= arc)
					{
						nX = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * a / 180));
						if (nX > 0 && nY > 0 && nX < width - 1 && nY < height - 1)
							break;
					}
					bytePreValue = pOrg[orgStride * nY + nX];
					for (double a = angle_s2; a < angle_s2 + 360; a += arc)
					{
						Point ptPre = new Point(nX, nY);
						nX = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * a / 180));
						nY = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * a / 180));
						if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
							continue;
						if (pOrg[orgStride * nY + nX] == 0)
						{
							if (bytePreValue > 0)
							{
								nX = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * (a - arc) / 180));
								nY = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * (a - arc) / 180));
								if (nX < 1 || nY < 1 || nX >= width - 1 || nY >= height - 1)
									continue;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									ptLH_s2.X = ptInCenter.X + (int)(dbDiagonal2 * Math.Cos(Math.PI * (a - arc) / 180)); ;
									ptLH_s2.Y = ptInCenter.Y + (int)(dbDiagonal2 * Math.Sin(Math.PI * (a - arc) / 180)); ;
									break;
								}
							}
						}
						bytePreValue = pOrg[orgStride * nY + nX];
					}
					displayResult5.ptLH2 = ptLH_s2;
					displayResult5.ptRH2 = ptRH_s2;
					#endregion

					double dbXL, dbYL;
					double distance;
					int cnt = 0;
					#region // XL, YL 구하기 //
					for (double i = angle_s; i < angle_s + 359; i += 90, cnt++)
					{
						distance = 0;
						for (int loop = 0; loop < width; loop++)
						{
							if (nX < 0 || nY < 0 || nX >= width || nY >= height)
								break;
							ptFirst.X = nX;
							ptFirst.Y = nY;
							distance++;
							nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * i / 180));
							nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * i / 180));
							if (nX < 0 || nY < 0 || nX >= width || nY >= height)
								break;
							if (pOrg[orgStride * nY + nX] == 0)
							{
								break;
							}
						}
						if (cnt == 0)
							displayResult5.ptXL1 = ptFirst;
						else if (cnt == 1)
							displayResult5.ptYL1 = ptFirst;
						else if (cnt == 2)
							displayResult5.ptXL2 = ptFirst;
						else
							displayResult5.ptYL2 = ptFirst;
					}
					dbXL = Math.Sqrt(Math.Pow(displayResult5.ptXL2.X - displayResult5.ptXL1.X, 2) + Math.Pow(displayResult5.ptXL2.Y - displayResult5.ptXL1.Y, 2));
					dbYL = Math.Sqrt(Math.Pow(displayResult5.ptYL2.X - displayResult5.ptYL1.X, 2) + Math.Pow(displayResult5.ptYL2.Y - displayResult5.ptYL1.Y, 2));
					#endregion
					displayResult5.dbXLPixel = dbXL;
					displayResult5.dbYLPixel = dbYL;

					double dbT = 0, dbTMax = 0, dbTMin = double.MaxValue;
					cnt = 0;
					#region // 두께(T) 구하기 //
					int nTAngleCount = ModelInfo.Cam5_Data.TAngleCount;
					displayResult5.dbTValueMax = 0;
					displayResult5.dbTValueMin = double.MaxValue;
					if (displayResult5.ptTPoint1 != null)
						displayResult5.ptTPoint1 = null;
					if (displayResult5.ptTPoint2 != null)
						displayResult5.ptTPoint2 = null;
					if (displayResult5.bTPointDetail != null)
						displayResult5.bTPointDetail = null;
					displayResult5.ptTPoint1 = new Point[nTAngleCount];
					displayResult5.ptTPoint2 = new Point[nTAngleCount];
					displayResult5.bTPointDetail = new bool[nTAngleCount];

					double dbTMaxDetail = ModelInfo.Cam5_Data.T + ModelInfo.Cam5_Data.TMaxDetail;
					double dbTMinDetail = ModelInfo.Cam5_Data.T - ModelInfo.Cam5_Data.TMinDetail;
					double dbTAngle = angle_s + ModelInfo.Cam5_Data.TAngleStart;
					double dbInHalfT = 0;
					if (ModelInfo.Cam5_Data.MmPerPixel > 0)
						dbInHalfT = dbInHalf + (ModelInfo.Cam5_Data.T / ModelInfo.Cam5_Data.MmPerPixel) / 2.0;
					for (int i = 0; i < nTAngleCount; i++)
					{
						//distance = dbInHalf;
						distance = dbInHalfT;
						nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbTAngle / 180));
						nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbTAngle / 180));
						if (nX < 0 || nY < 0 || nX >= width || nY >= height)
							break;
						int nRepeat = 0;
						if (pOrg[orgStride * nY + nX] > 0)
						{
							for (int loop = 0; loop < width; loop++)
							{
								distance++;
								nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbTAngle / 180));
								nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbTAngle / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] == 0)
									nRepeat++;
								else
									nRepeat = 0;
								if (nRepeat > 10)
								{
									distance -= 10;
									nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbTAngle / 180));
									nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbTAngle / 180));
									ptFirst.X = nX;
									ptFirst.Y = nY;
									break;
								}
							}
							nRepeat = 0;
							for (int loop = 0; loop < width; loop++)
							{
								distance++;
								nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbTAngle / 180));
								nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbTAngle / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] > 0)
									nRepeat++;
								else
									nRepeat = 0;
								if (nRepeat > 10)
								{
									nX = ptInCenter.X + (int)((distance - 10) * Math.Cos(Math.PI * dbTAngle / 180));
									nY = ptInCenter.Y + (int)((distance - 10) * Math.Sin(Math.PI * dbTAngle / 180));
									ptLast.X = nX;
									ptLast.Y = nY;
									break;
								}
							}
						}
						else
						{
							for (int loop = 0; loop < width; loop++)
							{
								distance--;
								nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbTAngle / 180));
								nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbTAngle / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] > 0)
									nRepeat++;
								else
									nRepeat = 0;
								if (nRepeat > 10)
								{
									nX = ptInCenter.X + (int)((distance + 10) * Math.Cos(Math.PI * dbTAngle / 180));
									nY = ptInCenter.Y + (int)((distance + 10) * Math.Sin(Math.PI * dbTAngle / 180));
									ptFirst.X = nX;
									ptFirst.Y = nY;
									break;
								}
							}
							nRepeat = 0;
							for (int loop = 0; loop < width; loop++)
							{
								distance++;
								nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbTAngle / 180));
								nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbTAngle / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] > 0)
									nRepeat++;
								else
									nRepeat = 0;
								if (nRepeat > 10)
								{
									nX = ptInCenter.X + (int)((distance - 10) * Math.Cos(Math.PI * dbTAngle / 180));
									nY = ptInCenter.Y + (int)((distance - 10) * Math.Sin(Math.PI * dbTAngle / 180));
									ptLast.X = nX;
									ptLast.Y = nY;
									break;
								}
							}
						}
						distance = Math.Sqrt(Math.Pow(ptLast.X - ptFirst.X, 2) + Math.Pow(ptLast.Y - ptFirst.Y, 2));
						if (distance > dbTMax)
							dbTMax = distance;
						if (distance < dbTMin)
							dbTMin = distance;
						dbT += distance;
						displayResult5.ptTPoint1[cnt] = ptFirst;
						displayResult5.ptTPoint2[cnt] = ptLast;
						double dis = distance * ModelInfo.Cam5_Data.MmPerPixel;
						dis += (ModelInfo.Cam5_Data.T - dis) * 3.0 / 10.0;
						if (dis > displayResult5.dbTValueMax)
							displayResult5.dbTValueMax = dis;
						if (dis < displayResult5.dbTValueMin)
							displayResult5.dbTValueMin = dis;
						if (dis >= dbTMinDetail && dis <= dbTMaxDetail)
							displayResult5.bTPointDetail[cnt++] = true;
						else
							displayResult5.bTPointDetail[cnt++] = false;
						dbTAngle += ModelInfo.Cam5_Data.TAngleGap;
					}
					dbT /= (double)cnt;
					#endregion
					displayResult5.dbTPixel = dbT;

					#region // 두께2(T2) 구하기 //
					for (int i = 0; i < 2; i++)
					{
						double dbT2Angle = angle_s - ModelInfo.Cam5_Data.T2AngleStart;
						if (i == 1)
							dbT2Angle = angle_s + ModelInfo.Cam5_Data.T2AngleStart;

						distance = dbInHalfT / 2;
						nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbT2Angle / 180));
						nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbT2Angle / 180));
						if (nX < 0 || nY < 0 || nX >= width || nY >= height)
							break;
						cnt = 0;
						for (int loop = 0; loop < width; loop++)
						{
							distance++;
							nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbT2Angle / 180));
							nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbT2Angle / 180));
							if (nX < 0 || nY < 0 || nX >= width || nY >= height)
								break;
							if (pOrg[orgStride * nY + nX] == 0)
								cnt++;
							else
								cnt = 0;
							if (cnt > 10)
							{
								ptFirst.X = ptInCenter.X + (int)((distance - cnt) * Math.Cos(Math.PI * dbT2Angle / 180));
								ptFirst.Y = ptInCenter.Y + (int)((distance - cnt) * Math.Sin(Math.PI * dbT2Angle / 180));
								break;
							}
						}
						cnt = 0;
						for (int loop = 0; loop < width; loop++)
						{
							distance++;
							nX = ptInCenter.X + (int)(distance * Math.Cos(Math.PI * dbT2Angle / 180));
							nY = ptInCenter.Y + (int)(distance * Math.Sin(Math.PI * dbT2Angle / 180));
							if (nX < 0 || nY < 0 || nX >= width || nY >= height)
								break;
							if (pOrg[orgStride * nY + nX] > 0)
								cnt++;
							else
								cnt = 0;
							if (cnt > 300)
							{
								ptLast.X = ptInCenter.X + (int)((distance - cnt) * Math.Cos(Math.PI * dbT2Angle / 180));
								ptLast.Y = ptInCenter.Y + (int)((distance - cnt) * Math.Sin(Math.PI * dbT2Angle / 180));
								break;
							}
						}
						distance = Math.Sqrt(Math.Pow(ptLast.X - ptFirst.X, 2) + Math.Pow(ptLast.Y - ptFirst.Y, 2));
						displayResult5.ptT21[i] = ptFirst;
						displayResult5.ptT22[i] = ptLast;
						displayResult5.dbT2Pixel[i] = distance;
					}
					#endregion
					
					double dbLH_angle = CalcAngle(ptLH_s, ptLH_s2);
					Point ptEnd = new Point();
					Point ptWLEnd = new Point();
					#region // LH의 자체 각도와 중심에서의 최대 길이 구하기 & WL 한쪽 좌표 구하기 //
					double angle = dbLH_angle + 90;
					double angle_inverse = dbLH_angle - 90;
					int nX_S = ptInCenter.X + (int)((dbInHalf + dbT) * Math.Cos(Math.PI * angle_s / 180));
					int nY_S = ptInCenter.Y + (int)((dbInHalf + dbT) * Math.Sin(Math.PI * angle_s / 180));
					double max = 0;
					Point pt = ptLH_s2;
					for (int i = 1; i < (int)dbXL; i++)
					{
						pt.X = ptLH_s2.X + (int)(i * Math.Cos(Math.PI * dbLH_angle / 180));
						pt.Y = ptLH_s2.Y + (int)(i * Math.Sin(Math.PI * dbLH_angle / 180));
						if (pt.X < 0 || pt.Y < 0 || pt.X >= width || pt.Y >= height)
							break;
						if (pOrg[orgStride * pt.Y + pt.X] == 0)	// WL 구하기 위해 추가함.
						{
							for (int j = 0; j < (int)dbT; j++)
							{
								nX = pt.X + (int)(j * Math.Cos(Math.PI * angle_inverse / 180));
								nY = pt.Y + (int)(j * Math.Sin(Math.PI * angle_inverse / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] > 0)
								{
									nX = pt.X + (int)((j - 1) * Math.Cos(Math.PI * angle_inverse / 180));
									nY = pt.Y + (int)((j - 1) * Math.Sin(Math.PI * angle_inverse / 180));
									double dis = Math.Sqrt(Math.Pow(nX - nX_S, 2) + Math.Pow(nY - nY_S, 2));
									if (dis > max)
									{
										ptWLEnd = new Point(nX, nY);
										max = dis;
									}
									break;
								}
							}
						}
						else
						{
							for (int j = 0; j < (int)dbT; j++)
							{
								nX = pt.X + (int)(j * Math.Cos(Math.PI * angle / 180));
								nY = pt.Y + (int)(j * Math.Sin(Math.PI * angle / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									double dis = Math.Sqrt(Math.Pow(nX - nX_S, 2) + Math.Pow(nY - nY_S, 2));
									if (dis > max)
									{
										ptWLEnd = new Point(nX, nY);
										max = dis;
									}
									break;
								}
							}
						}
						bool bChk = false;
						for (int j = 0; j < (int)dbT; j++)
						{
							nX = pt.X + (int)(j * Math.Cos(Math.PI * angle / 180));
							nY = pt.Y + (int)(j * Math.Sin(Math.PI * angle / 180));
							if (nX < 0 || nY < 0 || nX >= width || nY >= height)
								break;
							if (pOrg[orgStride * nY + nX] == 0)
							{
								ptEnd.X = nX;
								ptEnd.Y = nY;
								bChk = true;
								break;
							}
						}
						if (bChk == false)
							break;
					}
					#endregion
					displayResult5.ptLHEnd = ptEnd;
					displayResult5.ptWLEnd1 = ptWLEnd;
					double dbLH_distance = Math.Sqrt(Math.Pow(ptEnd.X - ptInCenter.X, 2) + Math.Pow(ptEnd.Y - ptInCenter.Y, 2));

					double dbRH_angle = CalcAngle(ptRH_s, ptRH_s2);
					#region // RH의 자체 각도와 중심에서의 최대 길이 구하기 & WL 다른 한쪽 좌표 구하기 //
					angle = dbRH_angle - 90;
					angle_inverse = dbRH_angle + 90;
					max = 0;
					pt = ptRH_s2;
					for (int i = 1; i < (int)dbXL; i++)
					{
						pt.X = ptRH_s2.X + (int)(i * Math.Cos(Math.PI * dbRH_angle / 180));
						pt.Y = ptRH_s2.Y + (int)(i * Math.Sin(Math.PI * dbRH_angle / 180));
						if (pt.X < 0 || pt.Y < 0 || pt.X >= width || pt.Y >= height)
							break;
						if (pOrg[orgStride * pt.Y + pt.X] == 0)	// WL 구하기 위해 추가함.
						{
							for (int j = 0; j < (int)dbT; j++)
							{
								nX = pt.X + (int)(j * Math.Cos(Math.PI * angle_inverse / 180));
								nY = pt.Y + (int)(j * Math.Sin(Math.PI * angle_inverse / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] > 0)
								{
									nX = pt.X + (int)((j - 1) * Math.Cos(Math.PI * angle_inverse / 180));
									nY = pt.Y + (int)((j - 1) * Math.Sin(Math.PI * angle_inverse / 180));
									double dis = Math.Sqrt(Math.Pow(nX - nX_S, 2) + Math.Pow(nY - nY_S, 2));
									if (dis > max)
									{
										ptWLEnd = new Point(nX, nY);
										max = dis;
									}
									break;
								}
							}
						}
						else
						{
							for (int j = 0; j < (int)dbT; j++)
							{
								nX = pt.X + (int)(j * Math.Cos(Math.PI * angle / 180));
								nY = pt.Y + (int)(j * Math.Sin(Math.PI * angle / 180));
								if (nX < 0 || nY < 0 || nX >= width || nY >= height)
									break;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									double dis = Math.Sqrt(Math.Pow(nX - nX_S, 2) + Math.Pow(nY - nY_S, 2));
									if (dis > max)
									{
										ptWLEnd = new Point(nX, nY);
										max = dis;
									}
									break;
								}
							}
						}
						bool bChk = false;
						for (int j = 0; j < (int)dbT; j++)
						{
							nX = pt.X + (int)(j * Math.Cos(Math.PI * angle / 180));
							nY = pt.Y + (int)(j * Math.Sin(Math.PI * angle / 180));
							if (nX < 0 || nY < 0 || nX >= width || nY >= height)
								break;
							if (pOrg[orgStride * nY + nX] == 0)
							{
								ptEnd.X = nX;
								ptEnd.Y = nY;
								bChk = true;
								break;
							}
						}
						if (bChk == false)
							break;
					}
					#endregion
					displayResult5.ptRHEnd = ptEnd;
					displayResult5.ptWLEnd2 = ptWLEnd;
					displayResult5.dbWLPixel = Math.Sqrt(Math.Pow(displayResult5.ptWLEnd1.X - displayResult5.ptWLEnd2.X, 2) + Math.Pow(displayResult5.ptWLEnd1.Y - displayResult5.ptWLEnd2.Y, 2));
					double dbRH_distance = Math.Sqrt(Math.Pow(ptEnd.X - ptInCenter.X, 2) + Math.Pow(ptEnd.Y - ptInCenter.Y, 2));

					if (dbLH_distance == 0 && dbRH_distance == 0)
						return -1;
					else if (dbLH_distance == 0)
						dbLH_distance = dbRH_distance;
					else if (dbRH_distance == 0)
						dbRH_distance = dbLH_distance;
					displayResult5.dbLHPixel = dbLH_distance - dbInHalf - dbT;
					displayResult5.dbRHPixel = dbRH_distance - dbInHalf - dbT;
					displayResult5.dbRValueReal = CalcAngle(dbLH_angle, dbRH_angle, true);

					nX = ptInCenter.X + (int)(dbInHalf * Math.Cos(Math.PI * angle_s / 180));
					nY = ptInCenter.Y + (int)(dbInHalf * Math.Sin(Math.PI * angle_s / 180));
					displayResult5.ptOutCenter = new Point(nX, nY);
					displayResult5.rectOuter = new Rectangle(ptInCenter.X - (int)dbDiagonal, ptInCenter.Y - (int)dbDiagonal, (int)(dbDiagonal * 2), (int)(dbDiagonal * 2));

					if (ModelInfo.Cam5_Data.UseP == true)
					{
						#region // 돌기(Projection) 체크하기 //
						cnt = 0;
						displayResult5.ptPEnd1 = new Point();
						displayResult5.ptPEnd2 = new Point();
						Point ptL1 = new Point();
						Point ptL2 = new Point();
						Point ptR1 = new Point();
						Point ptR2 = new Point();
						bool bFind = false;
						for (int i = 0; i < dbInHalf / 2; i++)
						{
							bFind = false;
							pt.X = ptLH_s.X + (int)((dbT + i) * Math.Cos(Math.PI * (dbLH_angle + 90) / 180));
							pt.Y = ptLH_s.Y + (int)((dbT + i) * Math.Sin(Math.PI * (dbLH_angle + 90) / 180));
							for (int j = 0; j < dbDiagonal2 - dbDiagonal; j++)
							{
								nX = pt.X + (int)(j * Math.Cos(Math.PI * dbLH_angle / 180));
								nY = pt.Y + (int)(j * Math.Sin(Math.PI * dbLH_angle / 180));
								if (nX <= 0 || nY <= 0 || nX > width - 2 || nY > height - 2)
									continue;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									ptL1 = new Point(nX, nY);
									cnt = j;
									bFind = true;
									break;
								}
							}
							if (bFind == false)
							{
								if (ptL1.IsEmpty == false)
								{
									nX = ptLH_s.X + (int)(cnt * Math.Cos(Math.PI * dbLH_angle / 180));
									nY = ptLH_s.Y + (int)(cnt * Math.Sin(Math.PI * dbLH_angle / 180));
									ptL2 = new Point(nX, nY);
								}
								//break;
							}
						}
						for (int i = 0; i < dbInHalf / 2; i++)
						{
							bFind = false;
							pt.X = ptRH_s.X + (int)((dbT + i) * Math.Cos(Math.PI * (dbRH_angle - 90) / 180));
							pt.Y = ptRH_s.Y + (int)((dbT + i) * Math.Sin(Math.PI * (dbRH_angle - 90) / 180));
							for (int j = 0; j < dbDiagonal2 - dbDiagonal; j++)
							{
								nX = pt.X + (int)(j * Math.Cos(Math.PI * dbRH_angle / 180));
								nY = pt.Y + (int)(j * Math.Sin(Math.PI * dbRH_angle / 180));
								if (nX <= 0 || nY <= 0 || nX > width - 2 || nY > height - 2)
									continue;
								if (pOrg[orgStride * nY + nX] == 0)
								{
									ptR1 = new Point(nX, nY);
									cnt = j;
									bFind = true;
									break;
								}
							}
							if (bFind == false)
							{
								if (ptR1.IsEmpty == false)
								{
									nX = ptRH_s.X + (int)(cnt * Math.Cos(Math.PI * dbRH_angle / 180));
									nY = ptRH_s.Y + (int)(cnt * Math.Sin(Math.PI * dbRH_angle / 180));
									ptR2 = new Point(nX, nY);
								}
								//break;
							}
						}
						double dbLP = Math.Sqrt(Math.Pow(ptL2.X - ptL1.X, 2) + Math.Pow(ptL2.Y - ptL1.Y, 2));
						double dbRP = Math.Sqrt(Math.Pow(ptR2.X - ptR1.X, 2) + Math.Pow(ptR2.Y - ptR1.Y, 2));
						#endregion
						if (dbLP > dbRP)
						{
							displayResult5.ptPEnd1 = ptL1;
							displayResult5.ptPEnd2 = ptL2;
							displayResult5.dbPPixel = dbLP;
						}
						else
						{
							displayResult5.ptPEnd1 = ptR1;
							displayResult5.ptPEnd2 = ptR2;
							displayResult5.dbPPixel = dbRP;

							double temp = displayResult5.dbLHPixel;
							displayResult5.dbLHPixel = displayResult5.dbRHPixel;
							displayResult5.dbRHPixel = temp;
							temp = displayResult5.dbLHValue;
							displayResult5.dbLHValue = displayResult5.dbRHValue;
							displayResult5.dbRHValue = temp;
						}
					}
				}
				bmp.UnlockBits(bdOrg);
				bmp.Dispose();

				#region // 마무리 작업 //
				// 단위 환산 및 수치 보정.
				if (ModelInfo.Cam5_Data.MmPerPixel > 0)
				{
					displayResult5.dbXLValue = displayResult5.dbXLPixel * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbYLValue = displayResult5.dbYLPixel * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbLHValue = displayResult5.dbLHPixel * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbRHValue = displayResult5.dbRHPixel * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbTValue = displayResult5.dbTPixel * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbPValue = displayResult5.dbPPixel * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbT2Value[0] = displayResult5.dbT2Pixel[0] * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbT2Value[1] = displayResult5.dbT2Pixel[1] * ModelInfo.Cam5_Data.MmPerPixel;
					displayResult5.dbWLValue = displayResult5.dbWLPixel * ModelInfo.Cam5_Data.MmPerPixel;

					//displayResult5.dbXLValue += (ModelInfo.Cam5_Data.XL - displayResult5.dbXLValue) * 3.0 / 10.0;
					//displayResult5.dbYLValue += (ModelInfo.Cam5_Data.YL - displayResult5.dbYLValue) * 3.0 / 10.0;
					//displayResult5.dbLHValue += (ModelInfo.Cam5_Data.LH - displayResult5.dbLHValue) * 3.0 / 10.0;
					//displayResult5.dbRHValue += (ModelInfo.Cam5_Data.RH - displayResult5.dbRHValue) * 3.0 / 10.0;
					//displayResult5.dbTValue += (ModelInfo.Cam5_Data.T - displayResult5.dbTValue) * 3.0 / 10.0;
					//displayResult5.dbPValue += (ModelInfo.Cam5_Data.P - displayResult5.dbPValue) * 3.0 / 10.0;
					//displayResult5.dbT2Value[0] += (ModelInfo.Cam5_Data.T2 - displayResult5.dbT2Value[0]) * 3.0 / 10.0;
					//displayResult5.dbT2Value[1] += (ModelInfo.Cam5_Data.T2 - displayResult5.dbT2Value[1]) * 3.0 / 10.0;
					//displayResult5.dbWLValue += (ModelInfo.Cam5_Data.WL - displayResult5.dbWLValue) * 3.0 / 10.0;
					//displayResult5.dbRValue += (ModelInfo.Cam5_Data.R - displayResult5.dbRValue) * 3.0 / 10.0;

					displayResult5.dbXLValue = Math.Round(displayResult5.dbXLValue + ModelInfo.Cam5_Data.XLOffset, 3);
					displayResult5.dbYLValue = Math.Round(displayResult5.dbYLValue + ModelInfo.Cam5_Data.YLOffset, 3);
					displayResult5.dbLHValue = Math.Round(displayResult5.dbLHValue + ModelInfo.Cam5_Data.LHOffset, 3);
					displayResult5.dbRHValue = Math.Round(displayResult5.dbRHValue + ModelInfo.Cam5_Data.RHOffset, 3);
					displayResult5.dbTValue = Math.Round(displayResult5.dbTValue + ModelInfo.Cam5_Data.TOffset, 3);
					displayResult5.dbPValue = Math.Round(displayResult5.dbPValue + ModelInfo.Cam5_Data.POffset, 3);
					displayResult5.dbT2Value[0] = Math.Round(displayResult5.dbT2Value[0] + ModelInfo.Cam5_Data.T2Offset, 3);
					displayResult5.dbT2Value[1] = Math.Round(displayResult5.dbT2Value[1] + ModelInfo.Cam5_Data.T2Offset, 3);
					displayResult5.dbWLValue = Math.Round(displayResult5.dbWLValue + ModelInfo.Cam5_Data.WLOffset, 3);
					displayResult5.dbRValue = Math.Round(displayResult5.dbRValueReal/* + ModelInfo.Cam5_Data.ROffset*/, 3);
				}
				else
				{
					displayResult5.dbXLValue = 0;
					displayResult5.dbYLValue = 0;
					displayResult5.dbLHValue = 0;
					displayResult5.dbRHValue = 0;
					displayResult5.dbTValue = 0;
					displayResult5.dbPValue = 0;
					displayResult5.dbT2Value[0] = 0;
					displayResult5.dbT2Value[1] = 0;
					displayResult5.dbWLValue = 0;
					displayResult5.dbRValue = 0;
				}

				// NG, OK 판별
				displayResult5.bResult = true;
				displayResult5.bXL = true;
				displayResult5.bYL = true;
				displayResult5.bLH = true;
				displayResult5.bRH = true;
				displayResult5.bT = true;
				displayResult5.bP = true;
				displayResult5.bT2 = true;
				displayResult5.bWL = true;
				if (ModelInfo.Cam5_Data.UseXLYL == true)
				{
					if (displayResult5.dbXLValue < ModelInfo.Cam5_Data.XL - ModelInfo.Cam5_Data.XLMin
						|| displayResult5.dbXLValue > ModelInfo.Cam5_Data.XL + ModelInfo.Cam5_Data.XLMax)
					{
						displayResult5.bXL = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nXLNGCnt++;
					}
					if (displayResult5.dbYLValue < ModelInfo.Cam5_Data.YL - ModelInfo.Cam5_Data.YLMin
						|| displayResult5.dbYLValue > ModelInfo.Cam5_Data.YL + ModelInfo.Cam5_Data.YLMax)
					{
						displayResult5.bYL = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nYLNGCnt++;
					}
				}
				if (ModelInfo.Cam5_Data.UseLHRH == true)
				{
					if (displayResult5.dbLHValue < ModelInfo.Cam5_Data.LH - ModelInfo.Cam5_Data.LHMin
						|| displayResult5.dbLHValue > ModelInfo.Cam5_Data.LH + ModelInfo.Cam5_Data.LHMax)
					{
						displayResult5.bLH = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nLHNGCnt++;
					}
					if (displayResult5.dbRHValue < ModelInfo.Cam5_Data.RH - ModelInfo.Cam5_Data.RHMin
						|| displayResult5.dbRHValue > ModelInfo.Cam5_Data.RH + ModelInfo.Cam5_Data.RHMax)
					{
						displayResult5.bRH = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nRHNGCnt++;
					}
				}
				if (ModelInfo.Cam5_Data.UseT == true)
				{
					if (displayResult5.dbTValue < ModelInfo.Cam5_Data.T - ModelInfo.Cam5_Data.TMin
						|| displayResult5.dbTValue > ModelInfo.Cam5_Data.T + ModelInfo.Cam5_Data.TMax)
					{
						displayResult5.bT = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nTNGCnt++;
					}
					if (displayResult5.bT == true)
					{
						int nTAnlgeCount = ModelInfo.Cam5_Data.TAngleCount;
						for (int i = 0; i < nTAnlgeCount; i++)
							if (displayResult5.bTPointDetail[i] == false)
							{
								displayResult5.bT = false;
								displayResult5.bResult = false;
								if (bIsManual == false)
									displayResult5.nTNGCnt++;
								break;
							}
					}
				}
				if (ModelInfo.Cam5_Data.UseP == true)
				{
					if (displayResult5.dbPValue < ModelInfo.Cam5_Data.P - ModelInfo.Cam5_Data.PMin
						|| displayResult5.dbPValue > ModelInfo.Cam5_Data.P + ModelInfo.Cam5_Data.PMax)
					{
						displayResult5.bP = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nPNGCnt++;
					}
				}
				if (ModelInfo.Cam5_Data.UseT2 == true)
				{
					if (displayResult5.dbT2Value[0] < ModelInfo.Cam5_Data.T2 - ModelInfo.Cam5_Data.T2Min
						|| displayResult5.dbT2Value[0] > ModelInfo.Cam5_Data.T2 + ModelInfo.Cam5_Data.T2Max
						|| displayResult5.dbT2Value[1] < ModelInfo.Cam5_Data.T2 - ModelInfo.Cam5_Data.T2Min
						|| displayResult5.dbT2Value[1] > ModelInfo.Cam5_Data.T2 + ModelInfo.Cam5_Data.T2Max)
					{
						displayResult5.bT2 = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nT2NGCnt++;
					}
				}
				if (ModelInfo.Cam5_Data.UseWL == true)
				{
					if (displayResult5.dbWLValue < ModelInfo.Cam5_Data.WL - ModelInfo.Cam5_Data.WLMin
						|| displayResult5.dbWLValue > ModelInfo.Cam5_Data.WL + ModelInfo.Cam5_Data.WLMax)
					{
						displayResult5.bWL = false;
						displayResult5.bResult = false;
						if (bIsManual == false)
							displayResult5.nWLNGCnt++;
					}
				}
				#endregion
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[Inspection5] " + exc.ToString());
				return -1;
			}
			if (displayResult5.bResult == true)
				return 1;
			else if ((displayResult5.bXL == false || displayResult5.bYL == false || displayResult5.bLH == false || displayResult5.bRH == true)
				&& displayResult5.bT == true && displayResult5.bP == true && displayResult5.bT2 == true && displayResult5.bWL == true)
				return 2;
			else
				return 3;
		}

		private unsafe int CountWhiteEdge(byte* pOrg, int orgStride, Point s)
		{
			int nRet = 0;
			if (pOrg[(s.Y - 1) * orgStride + s.X] > 0)
				nRet++;
			if (pOrg[(s.Y - 1) * orgStride + (s.X + 1)] > 0)
				nRet++;
			if (pOrg[s.Y * orgStride + (s.X + 1)] > 0)
				nRet++;
			if (pOrg[(s.Y + 1) * orgStride + (s.X + 1)] > 0)
				nRet++;
			if (pOrg[(s.Y + 1) * orgStride + s.X] > 0)
				nRet++;
			if (pOrg[(s.Y + 1) * orgStride + (s.X - 1)] > 0)
				nRet++;
			if (pOrg[s.Y * orgStride + (s.X - 1)] > 0)
				nRet++;
			if (pOrg[(s.Y - 1) * orgStride + (s.X - 1)] > 0)
				nRet++;
			return nRet;
		}

		private double CalcAngle(double dbFirst, double dbSecond, bool bClockwise)
		{
			double dbRet = 0;
			if (bClockwise == true)
			{
				while (dbFirst > dbSecond)
					dbFirst -= 360;
				dbRet = Math.Abs(dbSecond - dbFirst);
			}
			else
			{
				while (dbSecond > dbFirst)
					dbSecond -= 360;
				dbRet = Math.Abs(dbFirst - dbSecond);
			}
			dbRet %= 360;
			return dbRet;
		}

		private double CalcAngle(Point ptFirst, Point ptSecond)
		{
			double dbRet = 0;
			if (ptSecond.X == ptFirst.X)
			{
				if (ptSecond.Y >= ptFirst.Y)
					dbRet = 90;
				else
					dbRet = 270;
			}
			else
			{
				dbRet = Math.Atan((double)(ptSecond.Y - ptFirst.Y) / (double)(ptSecond.X - ptFirst.X));
				dbRet = dbRet * 180 / Math.PI;
				if (ptSecond.X < ptFirst.X)
					dbRet += 180;
			}
			return dbRet;
		}
		#endregion

		#region /// Save NG Image Logic ///
		private void SaveNGImage(int nCamNo, int nGrabLoopCnt)
		{
			if (paramIni.IsNGImage == 2 || (paramIni.IsNGImage == 1 && nInspectResult[nCamNo, nGrabLoopCnt] != 1))
			{
				paramInspectionThread param = new paramInspectionThread();
				param.nCamNo = nCamNo;
				param.nGrabLoopCnt = nGrabLoopCnt;
				Thread threadSaveNGImage = new Thread(SaveNGImage);
				threadSaveNGImage.Start(param);
			}
		}

		private void SaveNGImage(object lParam)
		{
			try
			{
				int nCamNo = ((paramInspectionThread)lParam).nCamNo;
				int nGrabLoopCnt = ((paramInspectionThread)lParam).nGrabLoopCnt;
				DateTime now = DateTime.Now;
				string path = paramIni.NGImagePath + string.Format("{0:0000}{1:00}{2:00}\\", now.Year, now.Month, now.Day);
				Directory.CreateDirectory(path);
				path += string.Format("{0}\\", nCamNo + 1);
				Directory.CreateDirectory(path);
				path += string.Format("{0:00}{1:00}{2:00}_NGImage_{3}_{4}.bmp", now.Hour, now.Minute, now.Second, nCamNo + 1, nGrabLoopCnt);
				if (lstImage[nCamNo, nGrabLoopCnt % 4] != null)
				{
					Bitmap bmp = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
					bmp.Save(path);
					bmp.Dispose();
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveNGImage] " + exc.ToString());
			}
		}

		//private void SaveTestImage(int nCamNo, int nGrabLoopCnt)
		//{
		//	paramInspectionThread param = new paramInspectionThread();
		//	param.nCamNo = nCamNo;
		//	param.nGrabLoopCnt = nGrabLoopCnt;
		//	Thread threadSaveNGImage = new Thread(SaveTestImage);
		//	threadSaveNGImage.Start(param);
		//}

		//private void SaveTestImage(object lParam)
		//{
		//	try
		//	{
		//		int nCamNo = ((paramInspectionThread)lParam).nCamNo;
		//		int nGrabLoopCnt = ((paramInspectionThread)lParam).nGrabLoopCnt;
		//		DateTime now = DateTime.Now;
		//		string path = string.Format("D:\\TestImage\\");
		//		Directory.CreateDirectory(path);
		//		path += string.Format("{0:0000}{1:00}{2:00}\\", now.Year, now.Month, now.Day);
		//		Directory.CreateDirectory(path);
		//		path += string.Format("{0}\\", nCamNo + 1);
		//		Directory.CreateDirectory(path);
		//		path += string.Format("{0:00}{1:00}{2:00}_NGImage_{3}_{4}.bmp", now.Hour, now.Minute, now.Second, nCamNo + 1, nGrabLoopCnt);
		//		if (lstImage[nCamNo, nGrabLoopCnt % 4] != null)
		//		{
		//			Bitmap bmp = (Bitmap)lstImage[nCamNo, nGrabLoopCnt % 4].Clone();
		//			bmp.Save(path);
		//			bmp.Dispose();
		//		}
		//	}
		//	catch (Exception exc)
		//	{
		//		Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveNGImage] " + exc.ToString());
		//	}
		//}
		#endregion

		#region /// Save Data Logic ///
		int[] nSaveDataLineCount = new int[MAX_CAMERA_CNT];
		private void SaveColumnData()
		{
			try
			{
				if (paramIni.IsSaveData == true)
				{
					string content = "";
					string path = string.Format("{0}{1}_{2}\\", paramIni.SaveDataPath, ModelInfo.No, ModelInfo.Name);
					Directory.CreateDirectory(path);
					path += string.Format("{0:0000}{1:00}{2:00}\\", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
					Directory.CreateDirectory(path);
					for (int i = 0; i < MAX_CAMERA_CNT; i++)
					{
						strSaveDataPath[i] = path + string.Format("{0:00}{1:00}{2:00}_{3}.csv", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, i + 1);

						if (i == 0)
							content = "No, Result, Error Count, Obj Area(Pt), Date, Time";
						else if (i == 1)
							content = "No, Result, Error Count, Obj Area(Pt), Date, Time";
						else if (i == 2)
							content = "No, Result, Error Count, Obj Area(Pt), Date, Time";
						else if (i == 3)
							content = "No, Result, Error Count, Obj Area(Pt), Date, Time";
						else if (i == 4)
							content = "No, Result, X축, Y축, L길이, R길이, T, STOP핀, T2-1, T2-2, 날개간격, Date, Time";

						using (StreamWriter sw = new StreamWriter(strSaveDataPath[i], false, Encoding.Default))
						{
							sw.WriteLine(content);
						}
						nSaveDataLineCount[i] = 0;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveColumnData] " + exc.ToString());
			}
		}

		private void SaveData(int nCamNo, int nGrabLoopCnt)
		{
			if (paramIni.IsSaveData == true)
			{
				if (nCamNo == 0)
					SaveData1();
				else if (nCamNo == 1)
					SaveData2();
				else if (nCamNo == 2)
					SaveData3();
				else if (nCamNo == 3)
					SaveData4();
				else if (nCamNo == 4)
					SaveData5();
			}
		}

		private void SaveData1()
		{
			try
			{
				string content = string.Format("{0}", txtOKCnt1.GetTextInt() + txtNGCnt1.GetTextInt());

				content += string.Format(", {0}", displayResult1.bResult);
				content += string.Format(", {0}", displayResult1.lstErrArea.Count);
				for (int i = 0; i < displayResult1.lstErrArea.Count; i++)
					content += string.Format(", {0}", displayResult1.lstErrArea[i]);
				DateTime now = DateTime.Now;
				content += string.Format(", {0:0000}-{1:00}-{2:00}", now.Year, now.Month, now.Day);
				content += string.Format(", {0:00}:{1:00}:{2:00}", now.Hour, now.Minute, now.Second);

				if (strSaveDataPath[0] == null || nSaveDataLineCount[0] >= 10000)
					SaveColumnData();

				using (StreamWriter sw = new StreamWriter(strSaveDataPath[0], true, Encoding.Default))
				{
					sw.WriteLine(content);
				}
				nSaveDataLineCount[0]++;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveData1] " + exc.ToString());
			}
		}

		private void SaveData2()
		{
			try
			{
				string content = string.Format("{0}", txtOKCnt2.GetTextInt() + txtNGCnt2.GetTextInt());

				content += string.Format(", {0}", displayResult2.bResult);
				content += string.Format(", {0}", displayResult2.lstErrArea.Count);
				for (int i = 0; i < displayResult2.lstErrArea.Count; i++)
					content += string.Format(", {0}", displayResult2.lstErrArea[i]);
				DateTime now = DateTime.Now;
				content += string.Format(", {0:0000}-{1:00}-{2:00}", now.Year, now.Month, now.Day);
				content += string.Format(", {0:00}:{1:00}:{2:00}", now.Hour, now.Minute, now.Second);

				if (strSaveDataPath[1] == null || nSaveDataLineCount[1] >= 10000)
					SaveColumnData();

				using (StreamWriter sw = new StreamWriter(strSaveDataPath[1], true, Encoding.Default))
				{
					sw.WriteLine(content);
				}
				nSaveDataLineCount[1]++;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveData2] " + exc.ToString());
			}
		}

		private void SaveData3()
		{
			try
			{
				string content = string.Format("{0}", txtOKCnt3.GetTextInt() + txtNGCnt3.GetTextInt());

				content += string.Format(", {0}", displayResult3.bResult);
				content += string.Format(", {0}", displayResult3.lstErrArea.Count);
				for (int i = 0; i < displayResult3.lstErrArea.Count; i++)
					content += string.Format(", {0}", displayResult3.lstErrArea[i]);
				DateTime now = DateTime.Now;
				content += string.Format(", {0:0000}-{1:00}-{2:00}", now.Year, now.Month, now.Day);
				content += string.Format(", {0:00}:{1:00}:{2:00}", now.Hour, now.Minute, now.Second);

				if (strSaveDataPath[2] == null || nSaveDataLineCount[2] >= 10000)
					SaveColumnData();

				using (StreamWriter sw = new StreamWriter(strSaveDataPath[2], true, Encoding.Default))
				{
					sw.WriteLine(content);
				}
				nSaveDataLineCount[2]++;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveData3] " + exc.ToString());
			}
		}

		private void SaveData4()
		{
			try
			{
				string content = string.Format("{0}", txtOKCnt4.GetTextInt() + txtNGCnt4.GetTextInt());

				content += string.Format(", {0}", displayResult4.bResult);
				content += string.Format(", {0}", displayResult4.lstErrArea.Count);
				for (int i = 0; i < displayResult4.lstErrArea.Count; i++)
					content += string.Format(", {0}", displayResult4.lstErrArea[i]);
				DateTime now = DateTime.Now;
				content += string.Format(", {0:0000}-{1:00}-{2:00}", now.Year, now.Month, now.Day);
				content += string.Format(", {0:00}:{1:00}:{2:00}", now.Hour, now.Minute, now.Second);

				if (strSaveDataPath[3] == null || nSaveDataLineCount[3] >= 10000)
					SaveColumnData();

				using (StreamWriter sw = new StreamWriter(strSaveDataPath[3], true, Encoding.Default))
				{
					sw.WriteLine(content);
				}
				nSaveDataLineCount[3]++;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveData4] " + exc.ToString());
			}
		}

		private void SaveData5()
		{
			try
			{
				string content = string.Format("{0}", txtOKCnt5.GetTextInt() + txtNGCnt5_1.GetTextInt() + txtNGCnt5_2.GetTextInt());

				content += string.Format(", {0}", displayResult5.bResult);
				content += string.Format(", {0}", displayResult5.dbYLValue);	// 동아금속이랑 XL, YL의 명칭이 바뀜.
				content += string.Format(", {0}", displayResult5.dbXLValue);
				content += string.Format(", {0}", displayResult5.dbRHValue);	// LH랑 RH도 서로 바뀜.
				content += string.Format(", {0}", displayResult5.dbLHValue);
				content += string.Format(", {0}", displayResult5.dbTValue);
				content += string.Format(", {0}", displayResult5.dbPValue);
				content += string.Format(", {0}", displayResult5.dbT2Value[0]);
				content += string.Format(", {0}", displayResult5.dbT2Value[1]);
				content += string.Format(", {0}", displayResult5.dbWLValue);
				DateTime now = DateTime.Now;
				content += string.Format(", {0:0000}-{1:00}-{2:00}", now.Year, now.Month, now.Day);
				content += string.Format(", {0:00}:{1:00}:{2:00}", now.Hour, now.Minute, now.Second);

				if (strSaveDataPath[4] == null || nSaveDataLineCount[4] >= 10000)	// ador 20130926 저장량이 너무 많아지면, 이런 데이터 저장 때문에 전체적인 속도가 느려져 버린다.
					SaveColumnData();

				using (StreamWriter sw = new StreamWriter(strSaveDataPath[4], true, Encoding.Default))
				{
					sw.WriteLine(content);
				}
				nSaveDataLineCount[4]++;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SaveData5] " + exc.ToString());
			}
		}
		#endregion
		#endregion

		#region /// Main Controls ///
		private void LockUI()
		{
			gbGubunInspect4144.Enabled = bAdminMode;
			gbImageSave.Enabled = bAdminMode;
			gbSaveDataPath.Enabled = bAdminMode;
			btnPassword.Text = bAdminMode ? "관리자 모드" : "일반 모드";
			btnModelSave.Enabled = bAdminMode;

			btnRectClear1.Enabled = bAdminMode;
			gbCameraSetting1.Enabled = bAdminMode;
			gbThreshold1.Enabled = bAdminMode;
			gbSetting1.Enabled = bAdminMode;
			chkUseInspect1.Enabled = bAdminMode;

			btnRectClear2.Enabled = bAdminMode;
			gbCameraSetting2.Enabled = bAdminMode;
			gbThreshold2.Enabled = bAdminMode;
			gbSetting2.Enabled = bAdminMode;
			chkUseInspect2.Enabled = bAdminMode;

			btnRectClear3.Enabled = bAdminMode;
			gbCameraSetting3.Enabled = bAdminMode;
			gbThreshold3.Enabled = bAdminMode;
			gbSetting3.Enabled = bAdminMode;
			chkUseInspect3.Enabled = bAdminMode;

			btnRectClear4.Enabled = bAdminMode;
			gbCameraSetting4.Enabled = bAdminMode;
			gbThreshold4.Enabled = bAdminMode;
			gbSetting4.Enabled = bAdminMode;
			chkUseInspect4.Enabled = bAdminMode;

			btnResetROI5.Enabled = bAdminMode;
			gbCalibration5.Enabled = bAdminMode;
			gbThreshold5.Enabled = bAdminMode;
			gbUseInspection5.Enabled = bAdminMode;
			gbInspectSetting5.Enabled = bAdminMode;
			gbOffset5.Enabled = bAdminMode;
			gbTSetting5.Enabled = bAdminMode;
			gbT2Setting5.Enabled = bAdminMode;
			gbAngleInspectPos5.Enabled = bAdminMode;
		}

		private void rdoViewMode_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				RadioButton btn = (RadioButton)sender;
				if (btn.Checked)
				{
					int view = int.Parse(btn.Tag.ToString());
					nView = view;

					#region /// Panel Visible Setting ///
					panelViewAll.Visible = false;
					panelView1.Visible = false;
					panelView2.Visible = false;
					panelView3.Visible = false;
					panelView4.Visible = false;
					panelView5.Visible = false;

					switch (nView)
					{
						case 0:
							//panelViewAll.Size = new Size(1190, 830);
							panelViewAll.Visible = true;
							break;
						case 1:
							panelView1.Visible = true;
							break;
						case 2:
							panelView2.Visible = true;
							break;
						case 3:
							panelView3.Visible = true;
							break;
						case 4:
							panelView4.Visible = true;
							break;
						case 5:
							panelView5.Visible = true;
							break;
					}
					#endregion

					if (nView == 0)
					{
						for (int i = 0; i < MAX_CAMERA_CNT; i++)
							ivCamViewer[i].Invalidate();

						//gbSaveDataPath.Visible = false;
						//gbImageSave.Visible = false;
					}
					else
					{
						//gbSaveDataPath.Visible = true;
						//gbImageSave.Visible = true;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[rdoViewMode_CheckedChanged] " + exc.ToString());
			}
		}

		private void chkROIVisible_CheckedChanged(object sender, EventArgs e)
		{
			bIsROIVisible = chkROIVisible.Checked;
			for (int i = 0; i < MAX_CAMERA_CNT; i++)
			{
				ivCamViewer[i].Invalidate();
				ivMainViewer[i].Invalidate();
			}
		}

		private void chkStart_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				if (this.chkStart.InvokeRequired)
				{
					System.EventHandler d = new System.EventHandler(chkStart_CheckedChanged);
					this.Invoke(d, new object[] { sender, e });
				}
				else
				{
					if (bAutoStart == false)
					{
						bAutoStart = true;
						//ResetData();
						chkStart.Image = global::ClipInspection.Properties.Resources.RUN;
						chkStart.Text = "자동 모드";

						SaveColumnData();
					}
					else
					{
						bAutoStart = false;
						chkStart.Image = global::ClipInspection.Properties.Resources.Stop;
						chkStart.Text = "수동 모드";
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[chkStart_CheckedChanged] " + exc.ToString());
			}
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			try
			{
				if (this.btnReset.InvokeRequired)
				{
					System.EventHandler d = new System.EventHandler(btnReset_Click);
					this.Invoke(d, new object[] { sender, e });
				}
				else
				{
					ResetData();
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[btnReset_Click] " + exc.ToString());
			}
		}

		private void btnModelChange_Click(object sender, EventArgs e)
		{
			try
			{
				FrmModelList fm = new FrmModelList(this);
				if (fm.ShowDialog() == DialogResult.OK)
				{
					ChangeModel();
					//ResetData();
					//btnReset_Click(null, null);
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[btnModelChange_Click] " + exc.ToString());
			}
		}

		private void btnModelSave_Click(object sender, EventArgs e)
		{
			SaveConfig(ModelInfo.No);
		}

		delegate void DisplayResultCollback(int nResult);
		private void DisplayResult(int nResult)
		{
			try
			{
				if (this.axTotalCnt.InvokeRequired)
				{
					DisplayResultCollback d = new DisplayResultCollback(DisplayResult);
					this.Invoke(d, new object[] { nResult });
				}
				else
				{
					int nTotalCount = nNGCount1 + nOKCount2 + nNGCount2_1 + nNGCount2_2;

					axTotalCnt.ValueNumber = nTotalCount;
					axOKCnt.ValueNumber = nOKCount2;
					axNGCnt1.ValueNumber = nNGCount1;
					axNGCnt2_1.ValueNumber = nNGCount2_1;
					axNGCnt2_2.ValueNumber = nNGCount2_2;
					if (nTotalCount > 0)
					{
						axOKRate.ValueNumber = (double)nOKCount2 / (double)nTotalCount * 100.0;
					}
					else
					{
						axOKRate.ValueNumber = 100.0;
					}
					if (nResult == 1)
					{
						lblResults2.Text = "양품";
						lblResults2.ForeColor = Color.Green;
					}
					else if (nResult > 1)
					{
						lblResults2.Text = "불량";
						lblResults2.ForeColor = Color.Red;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[DisplayResult2] " + exc.ToString());
			}
		}

		private bool ImageLoad(int nCamNo)
		{
			try
			{
				OpenFileDialog open = new OpenFileDialog();
				open.Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*";

				if (open.ShowDialog() == DialogResult.OK)
				{
					if (lstImage[nCamNo, nGrabLoopCnt[nCamNo] % 4] != null)
						lstImage[nCamNo, nGrabLoopCnt[nCamNo] % 4].Dispose();

					lstImage[nCamNo, nGrabLoopCnt[nCamNo] % 4] = new Bitmap(open.FileName);
					//lstImage[nCamNo, nGrabLoopCnt[nCamNo] % 4].RotateFlip(RotateFlipType.Rotate90FlipNone);
					return true;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ImageLoad] " + exc.ToString());
			}
			return false;
		}

		private bool ImageSave(int nCamNo)
		{
			try
			{
				SaveFileDialog save = new SaveFileDialog();
				save.Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*";
				if (save.ShowDialog() == DialogResult.OK)
				{
					lstImage[nCamNo, nGrabLoopCnt[nCamNo] % 4].Save(save.FileName, ImageFormat.Bmp);
					MessageBox.Show("저장하였습니다.", "확인");
					return true;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ImageSave] " + exc.ToString());
			}
			return false;
		}

		delegate void DisplayResultLabelCollback(object obj, int nCode);
		private void DisplayResultLabel(object obj, int nCode)
		{
			try
			{
				Label lbl = (Label)obj;
				if (lbl.InvokeRequired)
				{
					DisplayResultLabelCollback d = new DisplayResultLabelCollback(DisplayResultLabel);
					this.Invoke(d, new object[] { obj, nCode });
				}
				else
				{
					if (nCode == 1)
					{
						lbl.Text = "OK";
						lbl.ForeColor = Color.Green;
					}
					else if (nCode > 1)
					{
						lbl.Text = "NG";
						lbl.ForeColor = Color.Red;
					}
					else
					{
						lbl.Text = "ER";
						lbl.ForeColor = Color.Black;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[DisplayResultLabel] " + exc.ToString());
			}
		}

		private void btnPassword_Click(object sender, EventArgs e)
		{
			if (bAdminMode == false)
			{
				FrmPassword frm = new FrmPassword(this);
				if (frm.ShowDialog() == DialogResult.OK)
				{
					LockUI();
				}
				Saveini("Default.ini");
			}
			else
			{
				bAdminMode = false;
				LockUI();
			}
		}

		private void btnFindSaveDataPath_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.ShowNewFolderButton = true;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				txtSaveDataPath.Text = dlg.SelectedPath + "\\";
				paramIni.SaveDataPath = txtSaveDataPath.Text;
				Directory.CreateDirectory(paramIni.SaveDataPath);
				Saveini("Default.ini");
			}
		}

		private void btnShowReport_Click(object sender, EventArgs e)
		{
			FrmReport frm = new FrmReport(this);
			frm.ShowDialog();
		}

		private void chkUseSaveData_CheckedChanged(object sender, EventArgs e)
		{
			paramIni.IsSaveData = ((CheckBox)sender).Checked;
			Saveini("Default.ini");
		}

		private void btnFindNGImagePath_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.ShowNewFolderButton = true;
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				txtNGImagePath.Text = dlg.SelectedPath + "\\";
				paramIni.NGImagePath = txtNGImagePath.Text;
				Directory.CreateDirectory(paramIni.NGImagePath);
				Saveini("Default.ini");
			}
		}

		private void rdoUseNGImage_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton btn = (RadioButton)sender;
			int nSelect = int.Parse((string)btn.Tag);
			if (btn.Checked == true && nSelect != paramIni.IsNGImage)
			{
				paramIni.IsNGImage = nSelect;
				Saveini("Default.ini");
			}
		}

		private void txtModelName_TextChanged(object sender, EventArgs e)
		{
			ModelInfo.Name = txtModelName.Text.Trim();
		}
		#endregion

		#region /// Sub Viewer Controls #1 (표면 검사 #1) ///
		Pen penLime = new Pen(Color.Lime, 1);
		Pen penRed = new Pen(Color.Red, 1);
		Pen penRed2 = new Pen(Color.Red, 2);
		Pen penBlue = new Pen(Color.Blue, 1);
		Pen penDarkBlue = new Pen(Color.DarkBlue, 1);
		Pen penGreen = new Pen(Color.Green, 1);
		Pen penYellow = new Pen(Color.DarkOrange, 2);
		Pen penMagenta = new Pen(Color.Magenta, 1);
		Pen RosyBrown = new Pen(Color.RosyBrown, 1);
		Pen penPink = new Pen(Color.Pink, 1);
		Pen penPurple = new Pen(Color.Purple, 1);
		Pen penWhite = new Pen(Color.White, 1);
		Pen penBlack = new Pen(Color.Black, 1);
		Font font = new Font("굴림체", 12);
		Font font_s = new Font("굴림체", 8);
		Font fontNo = new Font("고딕체", 8);
		int nTargetIndex = 0;
		paramMask_Data paramTargetMask = null;
		int nRoiAdjustCode = 0;
		int nColor = 0;
		Point posRealMouse = new Point();
		Point posBefore = new Point();
		//double dbErrWidth = 0;
		//double dbErrHeight = 0;
		//int nErrArea = 0;

		private int GetRoiAdjustCode(Rectangle roi, int x, int y)
		{
			int nRet = 0;
			Rectangle rt = roi;
			rt.X -= 1;
			rt.Y -= 1;
			rt.Width += 2;
			rt.Height += 2;
			if (rt.Contains(x, y) == true)
			{
				if (x < rt.Left + 3 && y < rt.Top + 3)
					nRet = 1;
				else if (y < rt.Top + 3 && x > rt.Right - 4)
					nRet = 2;
				else if (x > rt.Right - 4 && y > rt.Bottom - 4)
					nRet = 3;
				else if (y > rt.Bottom - 4 && x < rt.Left + 3)
					nRet = 4;
				else if (x < rt.Left + 3)
					nRet = 5;
				else if (y < rt.Top + 3)
					nRet = 6;
				else if (x > rt.Right - 4)
					nRet = 7;
				else if (y > rt.Bottom - 4)
					nRet = 8;
				else
					nRet = 9;
			}
			return nRet;
		}

		private void ivCamViewer1_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam1_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				foreach (Rectangle rect in displayResult1.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 5;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font_s, new Point(5, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font_s.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult1.lstErrRect.Count)
					, font_s, new Point(5, nFontHeight), (displayResult1.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				nFontHeight = iv.Height - font_s.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult1.dbInspectTime), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font_s.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[0]), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivCamViewer1_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer1_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			Point pt1, pt2, ptCenter;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam1_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				rt = iv.RealToScreen(ModelInfo.Cam1_Data.AlignROI);
				g.DrawRectangle(RosyBrown, rt);
				ptCenter = iv.RealToScreen(ModelInfo.Cam1_Data.AlignCenter);
				pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
				pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
				g.DrawLine(RosyBrown, pt1, pt2);
				pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
				pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
				g.DrawLine(RosyBrown, pt1, pt2);

				foreach (paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
				{
					rt = iv.RealToScreen(mask.ROI);
					g.DrawRectangle(penYellow, rt);
					string str = mask.No.ToString();
					g.DrawString(str, fontNo, Brushes.YellowGreen, rt.Left - str.Length * 6 - 4, rt.Top - 5);
				}
				foreach (Rectangle rect in displayResult1.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 10;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font, new Point(10, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult1.lstErrRect.Count)
					, font, new Point(10, nFontHeight), (displayResult1.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				//  마우스 위치표시
				nFontHeight = iv.Height - font.Height;
				TextRenderer.DrawText(g, string.Format("X : {0}, Y : {1}, C : {2}", posRealMouse.X, posRealMouse.Y, nColor), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult1.dbInspectTime), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[0]), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer1_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer1_MouseDown(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;
			if (bAdminMode == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					foreach(paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 2;
							paramTargetMask = mask;
							posBefore = iv.RealMousePosition;
							break;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam1_Data.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 1;
							posBefore = iv.RealMousePosition;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam1_Data.AlignROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 3;
							posBefore = iv.RealMousePosition;
						}
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer1_MouseDown] " + exc.Message);
			}
		}

		private void ivMainViewer1_MouseMove(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			try
			{
				posRealMouse = iv.RealMousePosition;
				nColor = iv.Image.GetPixel(posRealMouse.X, posRealMouse.Y).R;
				if (nRoiAdjustCode > 0)
				{
					Point pos = iv.RealMousePosition;
					Rectangle roi;
					if (nTargetIndex == 1)
						roi = ModelInfo.Cam1_Data.ROI;
					else if (nTargetIndex == 2)
						roi = paramTargetMask.ROI;
					else
						roi = ModelInfo.Cam1_Data.AlignROI;
					switch (nRoiAdjustCode)
					{
						case 1:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 2:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							roi.Width += pos.X - posBefore.X;
							break;
						case 3:
							roi.Width += pos.X - posBefore.X;
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 4:
							roi.Height += pos.Y - posBefore.Y;
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 5:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 6:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 7:
							roi.Width += pos.X - posBefore.X;
							break;
						case 8:
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 9:
							roi.X += pos.X - posBefore.X;
							roi.Y += pos.Y - posBefore.Y;
							break;
					}
					if (nTargetIndex == 1)
						ModelInfo.Cam1_Data.ROI = Basic.CheckRoi(roi, iv.Image);
					else if (nTargetIndex == 2)
						paramTargetMask.ROI = Basic.CheckRoi(roi, iv.Image);
					else
						ModelInfo.Cam1_Data.AlignROI = Basic.CheckRoi(roi, iv.Image);

					posBefore = pos;
				}
				iv.Invalidate();
				if (e.Button == MouseButtons.None)
				{
					int code = 0;
					foreach (paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
						if (code > 0)
							break;
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam1_Data.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam1_Data.AlignROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
						iv.Cursor = Cursors.Default;
					else if (code == 1)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 2)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 3)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 4)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 5)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 6)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 7)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 8)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 9)
						iv.Cursor = Cursors.Hand;
					else if (code == 10)
						iv.Cursor = Cursors.Cross;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer1_MouseMove] " + exc.Message);
			}
		}

		private void ivMainViewer1_MouseUp(object sender, MouseEventArgs e)
		{
			if (bIsROIVisible == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					if (nRoiAdjustCode > 0)
					{
						nRoiAdjustCode = 0;
						nTargetIndex = 0;
						paramTargetMask = null;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer1_MouseUp] " + exc.Message);
			}
		}

		private void chkShowThreshold1_CheckedChanged(object sender, EventArgs e)
		{
			int nIndex = 0;
			if (chkShowThreshold1.Checked)
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
				{
					if (i <= trackThreshold1.Value)
						thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
					else
						thresPalette[nIndex][i] = Color.FromArgb(255, 0, 0);
				}
			}
			else
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
					thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
			}
			if (nView > 0)
			{
				ivMainViewer[nIndex].ChangePalette(thresPalette[nIndex]);
				ivMainViewer[nIndex].Invalidate();
			}
		}

		private void trackThreshold1_Scroll(object sender, EventArgs e)
		{
			try
			{
				txtThresLevel1.SetTextInt(trackThreshold1.Value);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[trackThreshold1_Scroll] " + exc.ToString());
			}
		}

		private void txtThresLevel1_TextNumberChanged(object sender, EventArgs e)
		{
			try
			{
				trackThreshold1.Value = txtThresLevel1.GetTextInt();
				chkShowThreshold1_CheckedChanged(null, null);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[txtThresLevel1_TextNumberChanged] " + exc.ToString());
				txtThresLevel1.SetTextInt(trackThreshold1.Value);
			}
		}

		private void txtExpose1_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.Expose = txtExpose1.GetTextInt();
			SetExpose(0, ModelInfo.Cam1_Data.Expose);
		}

		private void txtGain1_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.Gain = txtGain1.GetTextInt();
			SetGain(0, ModelInfo.Cam1_Data.Gain);
		}

		private void txtDelayCapture1_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.DelayCapture = txtDelayCapture1.GetTextInt();
			SetDelayCapture(0, ModelInfo.Cam1_Data.DelayCapture);
		}

		private void txtThreshold1_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.Threshold = txtThreshold1.GetTextInt();
		}

		private void txtDefectWidthMax1_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.DefectWidthMax = txtDefectWidthMax1.GetTextDouble();
		}

		private void txtDefectHeightMax1_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.DefectHeightMax = txtDefectHeightMax1.GetTextDouble();
		}

		private void RefreshMaskList1()
		{
			lstMask1.Items.Clear();
			foreach (paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
				lstMask1.Items.Add(mask.No.ToString());
		}

		private void btnMaskAdd1_Click(object sender, EventArgs e)
		{
			if (nView > 0 && ivMainViewer[nView - 1].Image != null)
			{
				paramMask_Data mask = new paramMask_Data();
				mask.No = 1;
				mask.ROI = ivMainViewer[nView - 1].ScreenToReal(new Rectangle(0, 0, 100, 100));

				foreach (paramMask_Data mi in ModelInfo.Cam1_Data.Masks)
				{
					if (mask.No <= mi.No)
						mask.No = mi.No + 1;
				}
				ModelInfo.Cam1_Data.Masks.Add(mask);
				RefreshMaskList1();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void lstMask1_MouseClick(object sender, MouseEventArgs e)
		{
			if (lstMask1.SelectedItems.Count > 0)
			{
				if (e.Button == MouseButtons.Right)
				{
					menuListMask1.Show(lstMask1, e.Location);
				}
			}
		}

		private void menuItemListMaskDelete1_Click(object sender, EventArgs e)
		{
			if (lstMask1.SelectedItems.Count > 0)
			{
				int no = int.Parse(lstMask1.SelectedItems[0].Text);
				lstMask1.Items.Remove(lstMask1.SelectedItems[0]);
				foreach (paramMask_Data mask in ModelInfo.Cam1_Data.Masks)
				{
					if (mask.No == no)
					{
						ModelInfo.Cam1_Data.Masks.Remove(mask);
						break;
					}
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void menuItemListMaskDeleteAll1_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("전부 삭제하시겠습니까?", "Delete All", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				lstMask1.Items.Clear();
				for (int i = ModelInfo.Cam1_Data.Masks.Count - 1; i >= 0; i--)
				{
					paramMask_Data mask = ModelInfo.Cam1_Data.Masks[i];
					ModelInfo.Cam1_Data.Masks.Remove(mask);
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void chkUseInspect1_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.UseInspect = chkUseInspect1.Checked;
		}

		private void btnRectClear1_Click(object sender, EventArgs e)
		{
			ModelInfo.Cam1_Data.ROI = new Rectangle(220, 130, 200, 230);
			ModelInfo.Cam1_Data.AlignROI = new Rectangle(450, 130, 50, 50);

			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnManualInspection1_Click(object sender, EventArgs e)
		{
			Stopwatch sw = new Stopwatch();
			sw.Reset();
			sw.Start();

			int nCode = Inspection1(nView - 1, nGrabLoopCnt[nView - 1], true);
			DisplayResultLabel(lblResult1, nCode);
			DisplayResultLabel(lblResultDetail1, nCode);

			sw.Stop();
			displayResult1.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

			// Display the new image
			ivCamViewer[nView - 1].Invalidate();
			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnImageLoad1_Click(object sender, EventArgs e)
		{
			if (ImageLoad(nView - 1) == true)
			{
				ivCamViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivCamViewer[nView - 1].Invalidate();
				ivMainViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void btnImageSave1_Click(object sender, EventArgs e)
		{
			ImageSave(nView - 1);
		}
		#endregion

		#region /// Sub Viewer Controls #2 (표면 검사 #2) ///
		private void ivCamViewer2_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam2_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				foreach (Rectangle rect in displayResult2.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 5;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font_s, new Point(5, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font_s.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult2.lstErrRect.Count)
					, font_s, new Point(5, nFontHeight), (displayResult2.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				nFontHeight = iv.Height - font_s.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult2.dbInspectTime), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font_s.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[1]), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivCamViewer2_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer2_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			Point pt1, pt2, ptCenter;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam2_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				rt = iv.RealToScreen(ModelInfo.Cam2_Data.AlignROI);
				g.DrawRectangle(RosyBrown, rt);
				ptCenter = iv.RealToScreen(ModelInfo.Cam2_Data.AlignCenter);
				pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
				pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
				g.DrawLine(RosyBrown, pt1, pt2);
				pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
				pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
				g.DrawLine(RosyBrown, pt1, pt2);

				foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
				{
					rt = iv.RealToScreen(mask.ROI);
					g.DrawRectangle(penYellow, rt);
					string str = mask.No.ToString();
					g.DrawString(str, fontNo, Brushes.YellowGreen, rt.Left - str.Length * 6 - 4, rt.Top - 5);
				}
				foreach (Rectangle rect in displayResult2.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 10;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font, new Point(10, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult2.lstErrRect.Count)
					, font, new Point(10, nFontHeight), (displayResult2.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				//  마우스 위치표시
				nFontHeight = iv.Height - font.Height;
				TextRenderer.DrawText(g, string.Format("X : {0}, Y : {1}, C : {2}", posRealMouse.X, posRealMouse.Y, nColor), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult2.dbInspectTime), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[1]), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer2_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer2_MouseDown(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;
			if (bAdminMode == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 2;
							paramTargetMask = mask;
							posBefore = iv.RealMousePosition;
							break;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam2_Data.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 1;
							posBefore = iv.RealMousePosition;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam2_Data.AlignROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 3;
							posBefore = iv.RealMousePosition;
						}
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer2_MouseDown] " + exc.Message);
			}
		}

		private void ivMainViewer2_MouseMove(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			try
			{
				posRealMouse = iv.RealMousePosition;
				nColor = iv.Image.GetPixel(posRealMouse.X, posRealMouse.Y).R;
				if (nRoiAdjustCode > 0)
				{
					Point pos = iv.RealMousePosition;
					Rectangle roi;
					if (nTargetIndex == 1)
						roi = ModelInfo.Cam2_Data.ROI;
					else if (nTargetIndex == 2)
						roi = paramTargetMask.ROI;
					else
						roi = ModelInfo.Cam2_Data.AlignROI;
					switch (nRoiAdjustCode)
					{
						case 1:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 2:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							roi.Width += pos.X - posBefore.X;
							break;
						case 3:
							roi.Width += pos.X - posBefore.X;
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 4:
							roi.Height += pos.Y - posBefore.Y;
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 5:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 6:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 7:
							roi.Width += pos.X - posBefore.X;
							break;
						case 8:
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 9:
							roi.X += pos.X - posBefore.X;
							roi.Y += pos.Y - posBefore.Y;
							break;
					}
					if (nTargetIndex == 1)
						ModelInfo.Cam2_Data.ROI = Basic.CheckRoi(roi, iv.Image);
					else if (nTargetIndex == 2)
						paramTargetMask.ROI = Basic.CheckRoi(roi, iv.Image);
					else
						ModelInfo.Cam2_Data.AlignROI = Basic.CheckRoi(roi, iv.Image);

					posBefore = pos;
				}
				iv.Invalidate();
				if (e.Button == MouseButtons.None)
				{
					int code = 0;
					foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
						if (code > 0)
							break;
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam2_Data.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam2_Data.AlignROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
						iv.Cursor = Cursors.Default;
					else if (code == 1)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 2)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 3)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 4)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 5)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 6)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 7)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 8)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 9)
						iv.Cursor = Cursors.Hand;
					else if (code == 10)
						iv.Cursor = Cursors.Cross;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer2_MouseMove] " + exc.Message);
			}
		}

		private void ivMainViewer2_MouseUp(object sender, MouseEventArgs e)
		{
			if (bIsROIVisible == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					if (nRoiAdjustCode > 0)
					{
						nRoiAdjustCode = 0;
						nTargetIndex = 0;
						paramTargetMask = null;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer2_MouseUp] " + exc.Message);
			}
		}

		private void chkShowThreshold2_CheckedChanged(object sender, EventArgs e)
		{
			int nIndex = 1;
			if (chkShowThreshold2.Checked)
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
				{
					if (i <= trackThreshold2.Value)
						thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
					else
						thresPalette[nIndex][i] = Color.FromArgb(255, 0, 0);
				}
			}
			else
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
					thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
			}
			if (nView > 0)
			{
				ivMainViewer[nIndex].ChangePalette(thresPalette[nIndex]);
				ivMainViewer[nIndex].Invalidate();
			}
		}

		private void trackThreshold2_Scroll(object sender, EventArgs e)
		{
			try
			{
				txtThresLevel2.SetTextInt(trackThreshold2.Value);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[trackThreshold2_Scroll] " + exc.ToString());
			}
		}

		private void txtThresLevel2_TextNumberChanged(object sender, EventArgs e)
		{
			try
			{
				trackThreshold2.Value = txtThresLevel2.GetTextInt();
				chkShowThreshold2_CheckedChanged(null, null);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[txtThresLevel2_TextNumberChanged] " + exc.ToString());
				txtThresLevel2.SetTextInt(trackThreshold2.Value);
			}
		}

		private void txtExpose2_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.Expose = txtExpose2.GetTextInt();
			SetExpose(1, ModelInfo.Cam2_Data.Expose);
		}

		private void txtGain2_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.Gain = txtGain2.GetTextInt();
			SetGain(1, ModelInfo.Cam2_Data.Gain);
		}

		private void txtDelayCapture2_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.DelayCapture = txtDelayCapture2.GetTextInt();
			SetDelayCapture(1, ModelInfo.Cam2_Data.DelayCapture);
		}

		private void txtThreshold2_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.Threshold = txtThreshold2.GetTextInt();
		}

		private void txtDefectWidthMax2_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.DefectWidthMax = txtDefectWidthMax2.GetTextDouble();
		}

		private void txtDefectHeightMax2_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.DefectHeightMax = txtDefectHeightMax2.GetTextDouble();
		}

		private void RefreshMaskList2()
		{
			lstMask2.Items.Clear();
			foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
				lstMask2.Items.Add(mask.No.ToString());
		}

		private void btnMaskAdd2_Click(object sender, EventArgs e)
		{
			if (nView > 0 && ivMainViewer[nView - 1].Image != null)
			{
				paramMask_Data mask = new paramMask_Data();
				mask.No = 1;
				mask.ROI = ivMainViewer[nView - 1].ScreenToReal(new Rectangle(0, 0, 100, 100));

				foreach (paramMask_Data mi in ModelInfo.Cam2_Data.Masks)
				{
					if (mask.No <= mi.No)
						mask.No = mi.No + 1;
				}
				ModelInfo.Cam2_Data.Masks.Add(mask);
				RefreshMaskList2();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void lstMask2_MouseClick(object sender, MouseEventArgs e)
		{
			if (lstMask2.SelectedItems.Count > 0)
			{
				if (e.Button == MouseButtons.Right)
				{
					menuListMask2.Show(lstMask2, e.Location);
				}
			}
		}

		private void menuItemListMaskDelete2_Click(object sender, EventArgs e)
		{
			if (lstMask2.SelectedItems.Count > 0)
			{
				int no = int.Parse(lstMask2.SelectedItems[0].Text);
				lstMask2.Items.Remove(lstMask2.SelectedItems[0]);
				foreach (paramMask_Data mask in ModelInfo.Cam2_Data.Masks)
				{
					if (mask.No == no)
					{
						ModelInfo.Cam2_Data.Masks.Remove(mask);
						break;
					}
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void menuItemListMaskDeleteAll2_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("전부 삭제하시겠습니까?", "Delete All", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				lstMask2.Items.Clear();
				for (int i = ModelInfo.Cam2_Data.Masks.Count - 1; i >= 0; i--)
				{
					paramMask_Data mask = ModelInfo.Cam2_Data.Masks[i];
					ModelInfo.Cam2_Data.Masks.Remove(mask);
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void chkUseInspect2_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.UseInspect = chkUseInspect2.Checked;
		}

		private void rdoGubunInspect4144_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton btn = (RadioButton)sender;
			if (btn.Checked == true)
				ModelInfo.GubunInspect4144 = int.Parse(btn.Tag.ToString());
		}

		private void btnRectClear2_Click(object sender, EventArgs e)
		{
			ModelInfo.Cam2_Data.ROI = new Rectangle(220, 130, 200, 230);
			ModelInfo.Cam2_Data.AlignROI = new Rectangle(450, 130, 50, 50);

			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnManualInspection2_Click(object sender, EventArgs e)
		{
			Stopwatch sw = new Stopwatch();
			sw.Reset();
			sw.Start();

			int nCode = 0;
			nCode = Inspection2(nView - 1, nGrabLoopCnt[nView - 1], true);
			DisplayResultLabel(lblResult2, nCode);
			DisplayResultLabel(lblResultDetail2, nCode);

			sw.Stop();
			displayResult2.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

			// Display the new image
			ivCamViewer[nView - 1].Invalidate();
			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnImageLoad2_Click(object sender, EventArgs e)
		{
			if (ImageLoad(nView - 1) == true)
			{
				ivCamViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivCamViewer[nView - 1].Invalidate();
				ivMainViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void btnImageSave2_Click(object sender, EventArgs e)
		{
			ImageSave(nView - 1);
		}
		#endregion

		#region /// Sub Viewer Controls #3 (표면 검사 #3) ///
		private void ivCamViewer3_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam3_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				foreach (Rectangle rect in displayResult3.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 5;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font_s, new Point(5, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font_s.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult3.lstErrRect.Count)
					, font_s, new Point(5, nFontHeight), (displayResult3.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				nFontHeight = iv.Height - font_s.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult3.dbInspectTime), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font_s.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[2]), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivCamViewer3_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer3_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			Point pt1, pt2, ptCenter;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam3_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				rt = iv.RealToScreen(ModelInfo.Cam3_Data.AlignROI);
				g.DrawRectangle(RosyBrown, rt);
				ptCenter = iv.RealToScreen(ModelInfo.Cam3_Data.AlignCenter);
				pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
				pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
				g.DrawLine(RosyBrown, pt1, pt2);
				pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
				pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
				g.DrawLine(RosyBrown, pt1, pt2);

				foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
				{
					rt = iv.RealToScreen(mask.ROI);
					g.DrawRectangle(penYellow, rt);
					string str = mask.No.ToString();
					g.DrawString(str, fontNo, Brushes.YellowGreen, rt.Left - str.Length * 6 - 4, rt.Top - 5);
				}
				foreach (Rectangle rect in displayResult3.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 10;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font, new Point(10, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult3.lstErrRect.Count)
					, font, new Point(10, nFontHeight), (displayResult3.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				nFontHeight = iv.Height - font.Height;
				TextRenderer.DrawText(g, string.Format("X : {0}, Y : {1}, C : {2}", posRealMouse.X, posRealMouse.Y, nColor), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult3.dbInspectTime), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[2]), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer3_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer3_MouseDown(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;
			if (bAdminMode == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 2;
							paramTargetMask = mask;
							posBefore = iv.RealMousePosition;
							break;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam3_Data.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 1;
							posBefore = iv.RealMousePosition;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam3_Data.AlignROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 3;
							posBefore = iv.RealMousePosition;
						}
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer3_MouseDown] " + exc.Message);
			}
		}

		private void ivMainViewer3_MouseMove(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			try
			{
				posRealMouse = iv.RealMousePosition;
				nColor = iv.Image.GetPixel(posRealMouse.X, posRealMouse.Y).R;
				if (nRoiAdjustCode > 0)
				{
					Point pos = iv.RealMousePosition;
					Rectangle roi;
					if (nTargetIndex == 1)
						roi = ModelInfo.Cam3_Data.ROI;
					else if (nTargetIndex == 2)
						roi = paramTargetMask.ROI;
					else
						roi = ModelInfo.Cam3_Data.AlignROI;
					switch (nRoiAdjustCode)
					{
						case 1:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 2:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							roi.Width += pos.X - posBefore.X;
							break;
						case 3:
							roi.Width += pos.X - posBefore.X;
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 4:
							roi.Height += pos.Y - posBefore.Y;
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 5:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 6:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 7:
							roi.Width += pos.X - posBefore.X;
							break;
						case 8:
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 9:
							roi.X += pos.X - posBefore.X;
							roi.Y += pos.Y - posBefore.Y;
							break;
					}
					if (nTargetIndex == 1)
						ModelInfo.Cam3_Data.ROI = Basic.CheckRoi(roi, iv.Image);
					else if (nTargetIndex == 2)
						paramTargetMask.ROI = Basic.CheckRoi(roi, iv.Image);
					else
						ModelInfo.Cam3_Data.AlignROI = Basic.CheckRoi(roi, iv.Image);

					posBefore = pos;
				}
				iv.Invalidate();
				if (e.Button == MouseButtons.None)
				{
					int code = 0;
					foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
						if (code > 0)
							break;
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam3_Data.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam3_Data.AlignROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
						iv.Cursor = Cursors.Default;
					else if (code == 1)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 2)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 3)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 4)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 5)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 6)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 7)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 8)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 9)
						iv.Cursor = Cursors.Hand;
					else if (code == 10)
						iv.Cursor = Cursors.Cross;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer3_MouseMove] " + exc.Message);
			}
		}

		private void ivMainViewer3_MouseUp(object sender, MouseEventArgs e)
		{
			if (bIsROIVisible == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					if (nRoiAdjustCode > 0)
					{
						nRoiAdjustCode = 0;
						nTargetIndex = 0;
						paramTargetMask = null;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer3_MouseUp] " + exc.Message);
			}
		}

		private void chkShowThreshold3_CheckedChanged(object sender, EventArgs e)
		{
			int nIndex = 2;
			if (chkShowThreshold3.Checked)
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
				{
					if (i <= trackThreshold3.Value)
						thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
					else
						thresPalette[nIndex][i] = Color.FromArgb(255, 0, 0);
				}
			}
			else
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
					thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
			}
			if (nView > 0)
			{
				ivMainViewer[nIndex].ChangePalette(thresPalette[nIndex]);
				ivMainViewer[nIndex].Invalidate();
			}
		}

		private void trackThreshold3_Scroll(object sender, EventArgs e)
		{
			try
			{
				txtThresLevel3.SetTextInt(trackThreshold3.Value);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[trackThreshold3_Scroll] " + exc.ToString());
			}
		}

		private void txtThresLevel3_TextNumberChanged(object sender, EventArgs e)
		{
			try
			{
				trackThreshold3.Value = txtThresLevel3.GetTextInt();
				chkShowThreshold3_CheckedChanged(null, null);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[txtThresLevel3_TextNumberChanged] " + exc.ToString());
				txtThresLevel3.SetTextInt(trackThreshold3.Value);
			}
		}

		private void txtExpose3_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.Expose = txtExpose3.GetTextInt();
			SetExpose(2, ModelInfo.Cam3_Data.Expose);
		}

		private void txtGain3_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.Gain = txtGain3.GetTextInt();
			SetGain(2, ModelInfo.Cam3_Data.Gain);
		}

		private void txtDelayCapture3_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.DelayCapture = txtDelayCapture3.GetTextInt();
			SetDelayCapture(2, ModelInfo.Cam3_Data.DelayCapture);
		}

		private void txtThreshold3_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.Threshold = txtThreshold3.GetTextInt();
		}

		private void txtDefectWidthMax3_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.DefectWidthMax = txtDefectWidthMax3.GetTextDouble();
		}

		private void txtDefectHeightMax3_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.DefectHeightMax = txtDefectHeightMax3.GetTextDouble();
		}

		private void RefreshMaskList3()
		{
			lstMask3.Items.Clear();
			foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
				lstMask3.Items.Add(mask.No.ToString());
		}

		private void btnMaskAdd3_Click(object sender, EventArgs e)
		{
			if (nView > 0 && ivMainViewer[nView - 1].Image != null)
			{
				paramMask_Data mask = new paramMask_Data();
				mask.No = 1;
				mask.ROI = ivMainViewer[nView - 1].ScreenToReal(new Rectangle(0, 0, 100, 100));

				foreach (paramMask_Data mi in ModelInfo.Cam3_Data.Masks)
				{
					if (mask.No <= mi.No)
						mask.No = mi.No + 1;
				}
				ModelInfo.Cam3_Data.Masks.Add(mask);
				RefreshMaskList3();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void lstMask3_MouseClick(object sender, MouseEventArgs e)
		{
			if (lstMask3.SelectedItems.Count > 0)
			{
				if (e.Button == MouseButtons.Right)
				{
					menuListMask3.Show(lstMask3, e.Location);
				}
			}
		}

		private void menuItemListMaskDelete3_Click(object sender, EventArgs e)
		{
			if (lstMask3.SelectedItems.Count > 0)
			{
				int no = int.Parse(lstMask3.SelectedItems[0].Text);
				lstMask3.Items.Remove(lstMask3.SelectedItems[0]);
				foreach (paramMask_Data mask in ModelInfo.Cam3_Data.Masks)
				{
					if (mask.No == no)
					{
						ModelInfo.Cam3_Data.Masks.Remove(mask);
						break;
					}
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void menuItemListMaskDeleteAll3_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("전부 삭제하시겠습니까?", "Delete All", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				lstMask3.Items.Clear();
				for (int i = ModelInfo.Cam3_Data.Masks.Count - 1; i >= 0; i--)
				{
					paramMask_Data mask = ModelInfo.Cam3_Data.Masks[i];
					ModelInfo.Cam3_Data.Masks.Remove(mask);
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void chkUseInspect3_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.UseInspect = chkUseInspect3.Checked;
		}

		private void btnRectClear3_Click(object sender, EventArgs e)
		{
			ModelInfo.Cam3_Data.ROI = new Rectangle(220, 130, 200, 230);
			ModelInfo.Cam3_Data.AlignROI = new Rectangle(450, 130, 50, 50);

			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnManualInspection3_Click(object sender, EventArgs e)
		{
			Stopwatch sw = new Stopwatch();
			sw.Reset();
			sw.Start();

			int nCode = Inspection3(nView - 1, nGrabLoopCnt[nView - 1], true);
			DisplayResultLabel(lblResult3, nCode);
			DisplayResultLabel(lblResultDetail3, nCode);

			sw.Stop();
			displayResult3.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

			// Display the new image
			ivCamViewer[nView - 1].Invalidate();
			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnImageLoad3_Click(object sender, EventArgs e)
		{
			if (ImageLoad(nView - 1) == true)
			{
				ivCamViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivCamViewer[nView - 1].Invalidate();
				ivMainViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void btnImageSave3_Click(object sender, EventArgs e)
		{
			ImageSave(nView - 1);
		}
		#endregion

		#region /// Sub Viewer Controls #4 (표면 검사 #4) ///
		private void ivCamViewer4_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam4_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				foreach (Rectangle rect in displayResult4.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 5;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font_s, new Point(5, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font_s.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult4.lstErrRect.Count)
					, font_s, new Point(5, nFontHeight), (displayResult4.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				nFontHeight = iv.Height - font_s.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult4.dbInspectTime), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font_s.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[3]), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivCamViewer4_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer4_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			Point pt1, pt2, ptCenter;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam4_Data.ROI);
				g.DrawRectangle(penBlue, rt);
				rt = iv.RealToScreen(ModelInfo.Cam4_Data.AlignROI);
				g.DrawRectangle(RosyBrown, rt);
				ptCenter = iv.RealToScreen(ModelInfo.Cam4_Data.AlignCenter);
				pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
				pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
				g.DrawLine(RosyBrown, pt1, pt2);
				pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
				pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
				g.DrawLine(RosyBrown, pt1, pt2);

				foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
				{
					rt = iv.RealToScreen(mask.ROI);
					g.DrawRectangle(penYellow, rt);
					string str = mask.No.ToString();
					g.DrawString(str, fontNo, Brushes.YellowGreen, rt.Left - str.Length * 6 - 4, rt.Top - 5);
				}
				foreach (Rectangle rect in displayResult4.lstErrRect)
				{
					rt = iv.RealToScreen(rect);
					g.DrawRectangle(penRed2, rt);
				}

				int nFontHeight = 10;
				TextRenderer.DrawText(g, bItemInverse ? "역방향" : "정방향", font, new Point(10, nFontHeight), (bItemInverse ? Color.Chocolate : Color.DarkGreen), Color.Black);
				nFontHeight += font.Height;
				TextRenderer.DrawText(g, string.Format("불량 수량 : {0}", displayResult4.lstErrRect.Count)
					, font, new Point(10, nFontHeight), (displayResult4.bResult ? Color.Yellow : Color.Tomato), Color.Black);

				nFontHeight = iv.Height - font.Height;
				TextRenderer.DrawText(g, string.Format("X : {0}, Y : {1}, C : {2}", posRealMouse.X, posRealMouse.Y, nColor), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult4.dbInspectTime), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[3]), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer4_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer4_MouseDown(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;
			if (bAdminMode == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 2;
							paramTargetMask = mask;
							posBefore = iv.RealMousePosition;
							break;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam4_Data.ROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 1;
							posBefore = iv.RealMousePosition;
						}
					}
					if (nTargetIndex == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam4_Data.AlignROI);
						nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
						if (nRoiAdjustCode > 0)
						{
							nTargetIndex = 3;
							posBefore = iv.RealMousePosition;
						}
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer4_MouseDown] " + exc.Message);
			}
		}

		private void ivMainViewer4_MouseMove(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			try
			{
				posRealMouse = iv.RealMousePosition;
				nColor = iv.Image.GetPixel(posRealMouse.X, posRealMouse.Y).R;
				if (nRoiAdjustCode > 0)
				{
					Point pos = iv.RealMousePosition;
					Rectangle roi;
					if (nTargetIndex == 1)
						roi = ModelInfo.Cam4_Data.ROI;
					else if (nTargetIndex == 2)
						roi = paramTargetMask.ROI;
					else
						roi = ModelInfo.Cam4_Data.AlignROI;
					switch (nRoiAdjustCode)
					{
						case 1:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 2:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							roi.Width += pos.X - posBefore.X;
							break;
						case 3:
							roi.Width += pos.X - posBefore.X;
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 4:
							roi.Height += pos.Y - posBefore.Y;
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 5:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 6:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 7:
							roi.Width += pos.X - posBefore.X;
							break;
						case 8:
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 9:
							roi.X += pos.X - posBefore.X;
							roi.Y += pos.Y - posBefore.Y;
							break;
					}
					if (nTargetIndex == 1)
						ModelInfo.Cam4_Data.ROI = Basic.CheckRoi(roi, iv.Image);
					else if (nTargetIndex == 2)
						paramTargetMask.ROI = Basic.CheckRoi(roi, iv.Image);
					else
						ModelInfo.Cam4_Data.AlignROI = Basic.CheckRoi(roi, iv.Image);

					posBefore = pos;
				}
				iv.Invalidate();
				if (e.Button == MouseButtons.None)
				{
					int code = 0;
					foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
					{
						Rectangle rt = iv.RealToScreen(mask.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
						if (code > 0)
							break;
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam4_Data.ROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
					{
						Rectangle rt = iv.RealToScreen(ModelInfo.Cam4_Data.AlignROI);
						code = GetRoiAdjustCode(rt, e.X, e.Y);
					}
					if (code == 0)
						iv.Cursor = Cursors.Default;
					else if (code == 1)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 2)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 3)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 4)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 5)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 6)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 7)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 8)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 9)
						iv.Cursor = Cursors.Hand;
					else if (code == 10)
						iv.Cursor = Cursors.Cross;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer4_MouseMove] " + exc.Message);
			}
		}

		private void ivMainViewer4_MouseUp(object sender, MouseEventArgs e)
		{
			if (bIsROIVisible == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					if (nRoiAdjustCode > 0)
					{
						nRoiAdjustCode = 0;
						nTargetIndex = 0;
						paramTargetMask = null;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer4_MouseUp] " + exc.Message);
			}
		}

		private void chkShowThreshold4_CheckedChanged(object sender, EventArgs e)
		{
			int nIndex = 3;
			if (chkShowThreshold4.Checked)
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
				{
					if (i <= trackThreshold4.Value)
						thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
					else
						thresPalette[nIndex][i] = Color.FromArgb(255, 0, 0);
				}
			}
			else
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
					thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
			}
			if (nView > 0)
			{
				ivMainViewer[nIndex].ChangePalette(thresPalette[nIndex]);
				ivMainViewer[nIndex].Invalidate();
			}
		}

		private void trackThreshold4_Scroll(object sender, EventArgs e)
		{
			try
			{
				txtThresLevel4.SetTextInt(trackThreshold4.Value);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[trackThreshold4_Scroll] " + exc.ToString());
			}
		}

		private void txtThresLevel4_TextNumberChanged(object sender, EventArgs e)
		{
			try
			{
				trackThreshold4.Value = txtThresLevel4.GetTextInt();
				chkShowThreshold4_CheckedChanged(null, null);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[txtThresLevel4_TextNumberChanged] " + exc.ToString());
				txtThresLevel4.SetTextInt(trackThreshold4.Value);
			}
		}

		private void txtExpose4_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.Expose = txtExpose4.GetTextInt();
			SetExpose(3, ModelInfo.Cam4_Data.Expose);
		}

		private void txtGain4_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.Gain = txtGain4.GetTextInt();
			SetGain(3, ModelInfo.Cam4_Data.Gain);
		}

		private void txtDelayCapture4_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.DelayCapture = txtDelayCapture4.GetTextInt();
			SetDelayCapture(3, ModelInfo.Cam4_Data.DelayCapture);
		}

		private void txtThreshold4_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.Threshold = txtThreshold4.GetTextInt();
		}

		private void txtDefectWidthMax4_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.DefectWidthMax = txtDefectWidthMax4.GetTextDouble();
		}

		private void txtDefectHeightMax4_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.DefectHeightMax = txtDefectHeightMax4.GetTextDouble();
		}

		private void RefreshMaskList4()
		{
			lstMask4.Items.Clear();
			foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
				lstMask4.Items.Add(mask.No.ToString());
		}

		private void btnMaskAdd4_Click(object sender, EventArgs e)
		{
			if (nView > 0 && ivMainViewer[nView - 1].Image != null)
			{
				paramMask_Data mask = new paramMask_Data();
				mask.No = 1;
				mask.ROI = ivMainViewer[nView - 1].ScreenToReal(new Rectangle(0, 0, 100, 100));

				foreach (paramMask_Data mi in ModelInfo.Cam4_Data.Masks)
				{
					if (mask.No <= mi.No)
						mask.No = mi.No + 1;
				}
				ModelInfo.Cam4_Data.Masks.Add(mask);
				RefreshMaskList4();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void lstMask4_MouseClick(object sender, MouseEventArgs e)
		{
			if (lstMask4.SelectedItems.Count > 0)
			{
				if (e.Button == MouseButtons.Right)
				{
					menuListMask4.Show(lstMask4, e.Location);
				}
			}
		}

		private void menuItemListMaskDelete4_Click(object sender, EventArgs e)
		{
			if (lstMask4.SelectedItems.Count > 0)
			{
				int no = int.Parse(lstMask4.SelectedItems[0].Text);
				lstMask4.Items.Remove(lstMask4.SelectedItems[0]);
				foreach (paramMask_Data mask in ModelInfo.Cam4_Data.Masks)
				{
					if (mask.No == no)
					{
						ModelInfo.Cam4_Data.Masks.Remove(mask);
						break;
					}
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void menuItemListMaskDeleteAll4_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("전부 삭제하시겠습니까?", "Delete All", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				lstMask4.Items.Clear();
				for (int i = ModelInfo.Cam4_Data.Masks.Count - 1; i >= 0; i--)
				{
					paramMask_Data mask = ModelInfo.Cam4_Data.Masks[i];
					ModelInfo.Cam4_Data.Masks.Remove(mask);
				}
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void chkUseInspect4_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.UseInspect = chkUseInspect4.Checked;
		}

		private void btnRectClear4_Click(object sender, EventArgs e)
		{
			ModelInfo.Cam4_Data.ROI = new Rectangle(220, 130, 200, 230);
			ModelInfo.Cam4_Data.AlignROI = new Rectangle(450, 130, 50, 50);

			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnManualInspection4_Click(object sender, EventArgs e)
		{
			Stopwatch sw = new Stopwatch();
			sw.Reset();
			sw.Start();

			int nCode = Inspection4(nView - 1, nGrabLoopCnt[nView - 1], true);
			DisplayResultLabel(lblResult4, nCode);
			DisplayResultLabel(lblResultDetail4, nCode);

			sw.Stop();
			displayResult4.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

			// Display the new image
			ivCamViewer[nView - 1].Invalidate();
			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnImageLoad4_Click(object sender, EventArgs e)
		{
			if (ImageLoad(nView - 1) == true)
			{
				ivCamViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivCamViewer[nView - 1].Invalidate();
				ivMainViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void btnImageSave4_Click(object sender, EventArgs e)
		{
			ImageSave(nView - 1);
		}
		#endregion

		#region /// Sub Viewer Controls #5 (상부 검사) ///
		private void ivCamViewer5_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			Point pt1, pt2, ptCenter;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam5_Data.ROI);
				g.DrawRectangle(penYellow, rt);
                if (displayResult5.dbRHPixel > 0)
				{
					rt = iv.RealToScreen(displayResult5.rectInner);
					g.DrawEllipse(penBlue, rt);
					pt1 = iv.RealToScreen(displayResult5.ptXL1);
					pt2 = iv.RealToScreen(displayResult5.ptXL2);
					g.DrawLine(penBlue, pt1, pt2);
					pt1 = iv.RealToScreen(displayResult5.ptYL1);
					pt2 = iv.RealToScreen(displayResult5.ptYL2);
					g.DrawLine(penBlue, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptCenter);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penYellow, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penYellow, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptXL1);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);

					pt1 = iv.RealToScreen(displayResult5.ptRH2);
					pt2 = iv.RealToScreen(displayResult5.ptLH2);
					g.DrawLine(penGreen, pt1, pt2);
					pt1 = pt2;
					pt2 = iv.RealToScreen(displayResult5.ptLH);
					g.DrawLine(penGreen, pt1, pt2);
					pt1 = pt2;
					pt2 = iv.RealToScreen(displayResult5.ptRH);
					g.DrawLine(penGreen, pt1, pt2);
					pt1 = pt2;
					pt2 = iv.RealToScreen(displayResult5.ptRH2);
					g.DrawLine(penGreen, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptLH);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(RosyBrown, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(RosyBrown, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptRH);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptLH2);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(RosyBrown, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(RosyBrown, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptRH2);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptLHEnd);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(RosyBrown, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(RosyBrown, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptRHEnd);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);
					if (ModelInfo.Cam5_Data.UseP == true)
					{
						ptCenter = iv.RealToScreen(displayResult5.ptPEnd1);
						pt1 = new Point(ptCenter.X - 5, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 5, ptCenter.Y);
						g.DrawLine(penPink, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 5);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 5);
						g.DrawLine(penPink, pt1, pt2);
						ptCenter = iv.RealToScreen(displayResult5.ptPEnd2);
						pt1 = new Point(ptCenter.X - 5, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 5, ptCenter.Y);
						g.DrawLine(penPink, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 5);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 5);
						g.DrawLine(penPink, pt1, pt2);
						pt1 = iv.RealToScreen(displayResult5.ptPEnd1);
						pt2 = iv.RealToScreen(displayResult5.ptPEnd2);
						g.DrawLine(penPink, pt1, pt2);
					}
					ptCenter = iv.RealToScreen(displayResult5.ptWLEnd1);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penPurple, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penPurple, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptWLEnd2);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penPurple, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penPurple, pt1, pt2);
					g.DrawLine(penPurple, iv.RealToScreen(displayResult5.ptWLEnd1), ptCenter);

					//foreach (Point pt in displayResult1.lstPoint)
					//{
					//    pt1 = iv.RealToScreen(pt);
					//    g.DrawLine(penLime, pt1.X, pt1.Y, pt1.X + 1, pt1.Y);
					//}
					int nTAngleCount = ModelInfo.Cam5_Data.TAngleCount;
					for (int i = 0; i < nTAngleCount; i++)
					{
						ptCenter = iv.RealToScreen(displayResult5.ptTPoint1[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						ptCenter = iv.RealToScreen(displayResult5.ptTPoint2[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
					}
					for (int i = 0; i < 2; i++)
					{
						ptCenter = iv.RealToScreen(displayResult5.ptT21[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						ptCenter = iv.RealToScreen(displayResult5.ptT22[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
					}

					int nFontHeight = 5;
					TextRenderer.DrawText(g, string.Format("X축 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbYLValue
						, displayResult5.dbYLPixel), font_s, new Point(5, nFontHeight), (displayResult5.bYL == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("Y축 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbXLValue
						, displayResult5.dbXLPixel), font_s, new Point(5, nFontHeight), (displayResult5.bXL == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("L길이 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbRHValue
						, displayResult5.dbRHPixel), font_s, new Point(5, nFontHeight), (displayResult5.bRH == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("R길이 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbLHValue
						, displayResult5.dbLHPixel), font_s, new Point(5, nFontHeight), (displayResult5.bLH == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("T : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbTValue
						, displayResult5.dbTPixel), font_s, new Point(5, nFontHeight), (displayResult5.bT == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("T Max: {0:0.000}mm, Min: {1:0.000}mm", displayResult5.dbTValueMax
						, displayResult5.dbTValueMin), font_s, new Point(5, nFontHeight), (displayResult5.bT == true ? Color.Yellow : Color.Tomato), Color.Black);
					if (ModelInfo.Cam5_Data.UseP == true)
					{
						nFontHeight += font_s.Height;
						TextRenderer.DrawText(g, string.Format("STOP핀 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbPValue
							, displayResult5.dbPPixel), font_s, new Point(5, nFontHeight), (displayResult5.bP == true ? Color.Yellow : Color.Tomato), Color.Black);
					}
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("T2-1 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbT2Value[0]
						, displayResult5.dbT2Pixel[0]), font_s, new Point(5, nFontHeight), (displayResult5.bT2 == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("T2-2 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbT2Value[1]
						, displayResult5.dbT2Pixel[1]), font_s, new Point(5, nFontHeight), (displayResult5.bT2 == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font_s.Height;
					TextRenderer.DrawText(g, string.Format("날개간격 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbWLValue
						, displayResult5.dbWLPixel), font_s, new Point(5, nFontHeight), (displayResult5.bWL == true ? Color.Yellow : Color.Tomato), Color.Black);
					//nFontHeight += font_s.Height;
					//TextRenderer.DrawText(g, string.Format("R각 : {0:0.000}˚", displayResult5.dbRValue), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);

					nFontHeight = iv.Height - font_s.Height;
					TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult5.dbInspectTime), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
					nFontHeight -= font_s.Height;
					TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[4]), font_s, new Point(5, nFontHeight), Color.Yellow, Color.Black);
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivCamViewer5_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer5_Paint(object sender, PaintEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			Graphics g = e.Graphics;
			Rectangle rt;
			Point pt1, pt2, ptCenter;
			int nFontHeight = 10;
			try
			{
				rt = iv.RealToScreen(ModelInfo.Cam5_Data.ROI);
				g.DrawRectangle(penYellow, rt);
				if (displayResult5.dbRHPixel > 0)
				{
					rt = iv.RealToScreen(displayResult5.rectInner);
					g.DrawEllipse(penBlue, rt);
					pt1 = iv.RealToScreen(displayResult5.ptXL1);
					pt2 = iv.RealToScreen(displayResult5.ptXL2);
					g.DrawLine(penBlue, pt1, pt2);
					pt1 = iv.RealToScreen(displayResult5.ptYL1);
					pt2 = iv.RealToScreen(displayResult5.ptYL2);
					g.DrawLine(penBlue, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptCenter);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penYellow, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penYellow, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptXL1);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);

					pt1 = iv.RealToScreen(displayResult5.ptRH2);
					pt2 = iv.RealToScreen(displayResult5.ptLH2);
					g.DrawLine(penGreen, pt1, pt2);
					pt1 = pt2;
					pt2 = iv.RealToScreen(displayResult5.ptLH);
					g.DrawLine(penGreen, pt1, pt2);
					pt1 = pt2;
					pt2 = iv.RealToScreen(displayResult5.ptRH);
					g.DrawLine(penGreen, pt1, pt2);
					pt1 = pt2;
					pt2 = iv.RealToScreen(displayResult5.ptRH2);
					g.DrawLine(penGreen, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptLH);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(RosyBrown, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(RosyBrown, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptRH);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptLH2);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(RosyBrown, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(RosyBrown, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptRH2);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);

					ptCenter = iv.RealToScreen(displayResult5.ptLHEnd);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(RosyBrown, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(RosyBrown, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptRHEnd);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penRed2, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penRed2, pt1, pt2);
					if (ModelInfo.Cam5_Data.UseP == true)
					{
						ptCenter = iv.RealToScreen(displayResult5.ptPEnd1);
						pt1 = new Point(ptCenter.X - 5, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 5, ptCenter.Y);
						g.DrawLine(penPink, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 5);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 5);
						g.DrawLine(penPink, pt1, pt2);
						ptCenter = iv.RealToScreen(displayResult5.ptPEnd2);
						pt1 = new Point(ptCenter.X - 5, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 5, ptCenter.Y);
						g.DrawLine(penPink, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 5);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 5);
						g.DrawLine(penPink, pt1, pt2);
						pt1 = iv.RealToScreen(displayResult5.ptPEnd1);
						pt2 = iv.RealToScreen(displayResult5.ptPEnd2);
						g.DrawLine(penPink, pt1, pt2);
					}
					ptCenter = iv.RealToScreen(displayResult5.ptWLEnd1);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penPurple, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penPurple, pt1, pt2);
					ptCenter = iv.RealToScreen(displayResult5.ptWLEnd2);
					pt1 = new Point(ptCenter.X - 10, ptCenter.Y);
					pt2 = new Point(ptCenter.X + 10, ptCenter.Y);
					g.DrawLine(penPurple, pt1, pt2);
					pt1 = new Point(ptCenter.X, ptCenter.Y - 10);
					pt2 = new Point(ptCenter.X, ptCenter.Y + 10);
					g.DrawLine(penPurple, pt1, pt2);
					g.DrawLine(penPurple, iv.RealToScreen(displayResult5.ptWLEnd1), ptCenter);

					//foreach (Point pt in displayResult1.lstPoint)
					//{
					//    pt1 = iv.RealToScreen(pt);
					//    g.DrawLine(penLime, pt1.X, pt1.Y, pt1.X + 1, pt1.Y);
					//}
					int nTAngleCount = ModelInfo.Cam5_Data.TAngleCount;
					for (int i = 0; i < nTAngleCount; i++)
					{
						ptCenter = iv.RealToScreen(displayResult5.ptTPoint1[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						ptCenter = iv.RealToScreen(displayResult5.ptTPoint2[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bTPointDetail[i] ? penLime : penRed2, pt1, pt2);
					}
					for (int i = 0; i < 2; i++)
					{
						ptCenter = iv.RealToScreen(displayResult5.ptT21[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bT2 ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bT2 ? penLime : penRed2, pt1, pt2);
						ptCenter = iv.RealToScreen(displayResult5.ptT22[i]);
						pt1 = new Point(ptCenter.X - 3, ptCenter.Y);
						pt2 = new Point(ptCenter.X + 3, ptCenter.Y);
						g.DrawLine(displayResult5.bT2 ? penLime : penRed2, pt1, pt2);
						pt1 = new Point(ptCenter.X, ptCenter.Y - 3);
						pt2 = new Point(ptCenter.X, ptCenter.Y + 3);
						g.DrawLine(displayResult5.bT2 ? penLime : penRed2, pt1, pt2);
					}

					TextRenderer.DrawText(g, string.Format("X축 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbYLValue
						, displayResult5.dbYLPixel), font, new Point(10, nFontHeight), (displayResult5.bYL == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("Y축 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbXLValue
						, displayResult5.dbXLPixel), font, new Point(10, nFontHeight), (displayResult5.bXL == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("L길이 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbRHValue
						, displayResult5.dbRHPixel), font, new Point(10, nFontHeight), (displayResult5.bRH == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("R길이 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbLHValue
						, displayResult5.dbLHPixel), font, new Point(10, nFontHeight), (displayResult5.bLH == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("T : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbTValue
						, displayResult5.dbTPixel), font, new Point(10, nFontHeight), (displayResult5.bT == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("T Max: {0:0.000}mm, Min: {1:0.000}mm", displayResult5.dbTValueMax
						, displayResult5.dbTValueMin), font, new Point(10, nFontHeight), (displayResult5.bT == true ? Color.Yellow : Color.Tomato), Color.Black);
					if (ModelInfo.Cam5_Data.UseP == true)
					{
						nFontHeight += font.Height;
						TextRenderer.DrawText(g, string.Format("STOP핀 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbPValue
							, displayResult5.dbPPixel), font, new Point(10, nFontHeight), (displayResult5.bP == true ? Color.Yellow : Color.Tomato), Color.Black);
					}
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("T2-1 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbT2Value[0]
						, displayResult5.dbT2Pixel[0]), font, new Point(10, nFontHeight), (displayResult5.bT2 == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("T2-2 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbT2Value[1]
						, displayResult5.dbT2Pixel[1]), font, new Point(10, nFontHeight), (displayResult5.bT2 == true ? Color.Yellow : Color.Tomato), Color.Black);
					nFontHeight += font.Height;
					TextRenderer.DrawText(g, string.Format("날개간격 : {0:0.000}mm ({1:0.000}pt)", displayResult5.dbWLValue
						, displayResult5.dbWLPixel), font, new Point(10, nFontHeight), (displayResult5.bWL == true ? Color.Yellow : Color.Tomato), Color.Black);
					//nFontHeight += font.Height;
					//TextRenderer.DrawText(g, string.Format("R각 : {0:0.000}˚", displayResult5.dbRValue), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				}

				nFontHeight = iv.Height - font.Height;
				TextRenderer.DrawText(g, string.Format("X : {0}, Y : {1}, C : {2}", posRealMouse.X, posRealMouse.Y, nColor), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Inspect Time : {0:0.000} sec", displayResult5.dbInspectTime), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
				nFontHeight -= font.Height;
				TextRenderer.DrawText(g, string.Format("Index : {0}", nGrabLoopCnt[4]), font, new Point(10, nFontHeight), Color.Yellow, Color.Black);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer5_Paint] " + exc.Message);
			}
		}

		private void ivMainViewer5_MouseDown(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;
			if (bAdminMode == false)
				return;

			try
			{
				if (e.Button == MouseButtons.Left)
				{
					Rectangle rt = iv.RealToScreen(ModelInfo.Cam5_Data.ROI);
					nRoiAdjustCode = GetRoiAdjustCode(rt, e.X, e.Y);
					if (nRoiAdjustCode > 0)
					{
						posBefore = iv.RealMousePosition;
					}
				}
				else if (e.Button == MouseButtons.Right)
				{
					if (nRoiAdjustCode == 0)
					{
						posBefore = iv.RealMousePosition;
						nRoiAdjustCode = 10;
					}
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer5_MouseDown] " + exc.Message);
			}
		}

		private void ivMainViewer5_MouseMove(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			if (bIsROIVisible == false)
				return;
			if (iv.Image == null)
				return;

			try
			{
				posRealMouse = iv.RealMousePosition;
				nColor = iv.Image.GetPixel(posRealMouse.X, posRealMouse.Y).R;
				if (nRoiAdjustCode == 10)
				{
					Point pos = iv.RealMousePosition;
					Rectangle rect = iv.ScreenRectangle;
					rect.X -= pos.X - posBefore.X;
					rect.Y -= pos.Y - posBefore.Y;
					rect = Basic.CheckRoi(rect, iv.Image);
					iv.ScreenRectangle = rect;
					posBefore = iv.RealMousePosition;
				}
				else if (nRoiAdjustCode > 0)
				{
					Point pos = iv.RealMousePosition;
					Rectangle roi = ModelInfo.Cam5_Data.ROI;
					switch (nRoiAdjustCode)
					{
						case 1:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 2:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							roi.Width += pos.X - posBefore.X;
							break;
						case 3:
							roi.Width += pos.X - posBefore.X;
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 4:
							roi.Height += pos.Y - posBefore.Y;
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 5:
							roi.Width -= pos.X - posBefore.X;
							roi.X += pos.X - posBefore.X;
							break;
						case 6:
							roi.Height -= pos.Y - posBefore.Y;
							roi.Y += pos.Y - posBefore.Y;
							break;
						case 7:
							roi.Width += pos.X - posBefore.X;
							break;
						case 8:
							roi.Height += pos.Y - posBefore.Y;
							break;
						case 9:
							roi.X += pos.X - posBefore.X;
							roi.Y += pos.Y - posBefore.Y;
							break;
					}
					ModelInfo.Cam5_Data.ROI = Basic.CheckRoi(roi, iv.Image);

					posBefore = pos;
				}
				if (e.Button == MouseButtons.None)
				{
					Rectangle rt = iv.RealToScreen(ModelInfo.Cam5_Data.ROI);
					int code = GetRoiAdjustCode(rt, e.X, e.Y);
					if (code == 0)
						iv.Cursor = Cursors.Default;
					else if (code == 1)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 2)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 3)
						iv.Cursor = Cursors.SizeNWSE;
					else if (code == 4)
						iv.Cursor = Cursors.SizeNESW;
					else if (code == 5)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 6)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 7)
						iv.Cursor = Cursors.SizeWE;
					else if (code == 8)
						iv.Cursor = Cursors.SizeNS;
					else if (code == 9)
						iv.Cursor = Cursors.Hand;
					else if (code == 10)
						iv.Cursor = Cursors.Cross;
				}
				iv.Invalidate();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer5_MouseMove] " + exc.Message);
			}
		}

		private void ivMainViewer5_MouseUp(object sender, MouseEventArgs e)
		{
			if (bIsROIVisible == false)
				return;

			try
			{
				nRoiAdjustCode = 0;
				nTargetIndex = 0;
				paramTargetMask = null;
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ivMainViewer5_MouseUp] " + exc.Message);
			}
		}

		private void ivMainViewer5_Load(object sender, EventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			iv.MouseWheel += new MouseEventHandler(ctlMainViewer_MouseWheel);
		}

		private void ivMainViewer5_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			iv.FullImageView();
			iv.Invalidate();
		}

		private void ctlMainViewer_MouseWheel(object sender, MouseEventArgs e)
		{
			CImageViewer iv = (CImageViewer)sender;
			try
			{
				int count = e.Delta / 120;

				if (Control.ModifierKeys == Keys.Shift)
				{
					Rectangle rt = iv.ScreenRectangle;
					rt.Width += count * 200;
					if (rt.Width > iv.Image.Width)
						rt.Width = iv.Image.Width;
					if (rt.Width < iv.Width / 4)
						rt.Width = iv.Width / 4;
					rt.Height = rt.Width * iv.Height / iv.Width;
					rt.X -= (rt.Width - iv.ScreenRectangle.Width) / 2;
					rt.Y -= (rt.Height - iv.ScreenRectangle.Height) / 2;
					rt = Basic.CheckRoi(rt, iv.Image);
					iv.ScreenRectangle = rt;
				}
				else
				{
					Rectangle rt = iv.ScreenRectangle;
					rt.Y -= count * 200;
					if (rt.Y < 0)
					{
						rt.Y = 0;
					}
					if (rt.Bottom > iv.Image.Height)
					{
						rt.Y += iv.Image.Height - rt.Bottom;
					}
					iv.ScreenRectangle = rt;
				}
				iv.Invalidate();
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[ctlMainViewer_MouseWheel] " + exc.Message);
			}
		}

		private void chkShowThreshold5_CheckedChanged(object sender, EventArgs e)
		{
			int nIndex = 4;
			if (chkShowThreshold5.Checked)
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
				{
					if (i <= trackThreshold5.Value)
						thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
					else
						thresPalette[nIndex][i] = Color.FromArgb(255, 0, 0);
				}
			}
			else
			{
				// Build bitmap palette Y8
				for (int i = 0; i < 256; i++)
					thresPalette[nIndex][i] = Color.FromArgb(i, i, i);
			}
			if (nView > 0)
			{
				ivMainViewer[nIndex].ChangePalette(thresPalette[nIndex]);
				ivMainViewer[nIndex].Invalidate();
			}
		}

		private void trackThreshold5_Scroll(object sender, EventArgs e)
		{
			try
			{
				txtThresLevel5.SetTextInt(trackThreshold5.Value);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[trackThreshold5_Scroll] " + exc.ToString());
			}
		}

		private void txtThresLevel5_TextNumberChanged(object sender, EventArgs e)
		{
			try
			{
				trackThreshold5.Value = txtThresLevel5.GetTextInt();
				chkShowThreshold5_CheckedChanged(null, null);
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[txtThresLevel5_TextNumberChanged] " + exc.ToString());
				txtThresLevel5.SetTextInt(trackThreshold5.Value);
			}
		}

		private void btnResetROI5_Click(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.ROI = new Rectangle(100, 100, 100, 100);
			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnCalcMmPerPixel5_Click(object sender, EventArgs e)
		{
			if (txtRealLength5.GetTextDouble() > 0)
			{
				if (displayResult5.dbYLPixel > 0)
				{
					ModelInfo.Cam5_Data.RealLength = txtRealLength5.GetTextDouble();
					ModelInfo.Cam5_Data.CalcLength = displayResult5.dbYLPixel;
					ModelInfo.Cam5_Data.MmPerPixel = ModelInfo.Cam5_Data.RealLength / ModelInfo.Cam5_Data.CalcLength;
					txtMmPerPixel5.SetTextDouble(ModelInfo.Cam5_Data.MmPerPixel);
				}
				else
				{
					MessageBox.Show("단위 교정(Calibration)을 할 수 없습니다.\r\n제품 이미지를 체크하신 후, [수동 검사] 버튼을 눌러주세요."
						, "오류", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				}
			}
			else
			{
				MessageBox.Show("X축 내경 지름 실측 길이 값을 넣어주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Stop);
			}
		}

		private void txtThreshold5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.Threshold = txtThreshold5.GetTextInt();
		}

		private void chkUseInspect5_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseInspect = chkUseInspect5.Checked;
		}

		private void chkUseXLYL5_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseXLYL = chkUseXLYL5.Checked;
		}

		private void chkUseLHRH5_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseLHRH = chkUseLHRH5.Checked;
		}

		private void chkUseT5_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseT = chkUseT5.Checked;
		}

		private void chkUseP5_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseP = chkUseP5.Checked;
		}

		private void chkUseT25_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseT2 = chkUseT25.Checked;
		}

		private void chkUseWL5_CheckedChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.UseWL = chkUseWL5.Checked;
		}

		private FrmDescription5 frmDescription5 = new FrmDescription5();
		private void labelDescription_MouseEnter(object sender, EventArgs e)
		{
			frmDescription5.Location = new Point((this.Width) / 2 - 50, (this.Height - frmDescription5.Height) / 2);
			frmDescription5.Show();
		}

		private void labelDescription_MouseLeave(object sender, EventArgs e)
		{
			frmDescription5.Hide();
		}

		private void txtXLMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.XLMax = txtXLMax5.GetTextDouble();
		}

		private void txtXL5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.XL = txtXL5.GetTextDouble();
		}

		private void txtXLMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.XLMin = txtXLMin5.GetTextDouble();
		}

		private void txtYLMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.YLMax = txtYLMax5.GetTextDouble();
		}

		private void txtYL5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.YL = txtYL5.GetTextDouble();
		}

		private void txtYLMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.YLMin = txtYLMin5.GetTextDouble();
		}

		private void txtLHMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.LHMax = txtLHMax5.GetTextDouble();
		}

		private void txtLH5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.LH = txtLH5.GetTextDouble();
		}

		private void txtLHMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.LHMin = txtLHMin5.GetTextDouble();
		}

		private void txtRHMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.RHMax = txtRHMax5.GetTextDouble();
		}

		private void txtRH5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.RH = txtRH5.GetTextDouble();
		}

		private void txtRHMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.RHMin = txtRHMin5.GetTextDouble();
		}

		private void txtTMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TMax = txtTMax5.GetTextDouble();
		}

		private void txtT5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.T = txtT5.GetTextDouble();
		}

		private void txtTMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TMin = txtTMin5.GetTextDouble();
		}

		private void txtPMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.PMax = txtPMax5.GetTextDouble();
		}

		private void txtP5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.P = txtP5.GetTextDouble();
		}

		private void txtPMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.PMin = txtPMin5.GetTextDouble();
		}

		private void txtT2Max5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.T2Max = txtT2Max5.GetTextDouble();
		}

		private void txtT25_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.T2 = txtT25.GetTextDouble();
		}

		private void txtT2Min5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.T2Min = txtT2Min5.GetTextDouble();
		}

		private void txtWLMax5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.WLMax = txtWLMax5.GetTextDouble();
		}

		private void txtWL5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.WL = txtWL5.GetTextDouble();
		}

		private void txtWLMin5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.WLMin = txtWLMin5.GetTextDouble();
		}

		private void txtXLOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.XLOffset = txtXLOffset5.GetTextDouble();
		}

		private void txtYLOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.YLOffset = txtYLOffset5.GetTextDouble();
		}

		private void txtLHOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.LHOffset = txtLHOffset5.GetTextDouble();
		}

		private void txtRHOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.RHOffset = txtRHOffset5.GetTextDouble();
		}

		private void txtTOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TOffset = txtTOffset5.GetTextDouble();
		}

		private void txtPOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.POffset = txtPOffset5.GetTextDouble();
		}

		private void txtT2Offset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.T2Offset = txtT2Offset5.GetTextDouble();
		}

		private void txtWLOffset5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.WLOffset = txtWLOffset5.GetTextDouble();
		}

		private void btnAutoOffset5_Click(object sender, EventArgs e)
		{
			if (ModelInfo.Cam5_Data.MmPerPixel > 0)
			{
				if (displayResult5.dbXLPixel > 0)
				{
					double dbXL = Math.Round(displayResult5.dbXLPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbYL = Math.Round(displayResult5.dbYLPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbLH = Math.Round(displayResult5.dbLHPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbRH = Math.Round(displayResult5.dbRHPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbT = Math.Round(displayResult5.dbTPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbP = Math.Round(displayResult5.dbPPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbT2 = Math.Round((displayResult5.dbT2Pixel[0] + displayResult5.dbT2Pixel[1]) / 2.0 * ModelInfo.Cam5_Data.MmPerPixel, 3);
					double dbWL = Math.Round(displayResult5.dbWLPixel * ModelInfo.Cam5_Data.MmPerPixel, 3);

					//dbXL = Math.Round(dbXL + ((ModelInfo.Cam5_Data.XL - dbXL) * 3.0 / 10.0), 3);
					//dbYL = Math.Round(dbYL + ((ModelInfo.Cam5_Data.YL - dbYL) * 3.0 / 10.0), 3);
					//dbLH = Math.Round(dbLH + ((ModelInfo.Cam5_Data.LH - dbLH) * 3.0 / 10.0), 3);
					//dbRH = Math.Round(dbRH + ((ModelInfo.Cam5_Data.RH - dbRH) * 3.0 / 10.0), 3);
					//dbT = Math.Round(dbT + ((ModelInfo.Cam5_Data.T - dbT) * 3.0 / 10.0), 3);
					//dbP = Math.Round(dbP + ((ModelInfo.Cam5_Data.P - dbP) * 3.0 / 10.0), 3);
					//dbT2 = Math.Round(dbT2 + ((ModelInfo.Cam5_Data.T2 - dbT2) * 3.0 / 10.0), 3);
					//dbWL = Math.Round(dbWL + ((ModelInfo.Cam5_Data.WL - dbWL) * 3.0 / 10.0), 3);
					//dbR = Math.Round(dbR + ((ModelInfo.Cam5_Data.R - dbR) * 3.0 / 10.0), 3);

					txtXLOffset5.SetTextDouble(ModelInfo.Cam5_Data.XL - dbXL);
					txtYLOffset5.SetTextDouble(ModelInfo.Cam5_Data.YL - dbYL);
					txtLHOffset5.SetTextDouble(ModelInfo.Cam5_Data.LH - dbLH);
					txtRHOffset5.SetTextDouble(ModelInfo.Cam5_Data.RH - dbRH);
					txtTOffset5.SetTextDouble(ModelInfo.Cam5_Data.T - dbT);
					txtPOffset5.SetTextDouble(ModelInfo.Cam5_Data.P - dbP);
					txtT2Offset5.SetTextDouble(ModelInfo.Cam5_Data.T2 - dbT2);
					txtWLOffset5.SetTextDouble(ModelInfo.Cam5_Data.WL - dbWL);
				}
				else
					MessageBox.Show("픽셀값이 없습니다.\r\n[수동검사]를 한번 수행해 주십시오.");
			}
			else
				MessageBox.Show("단위 환산을 먼저 수행하셔야 합니다.");
		}

		private void txtTAngleStart5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TAngleStart = txtTAngleStart5.GetTextDouble();
		}

		private void txtTAngleGap5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TAngleGap = txtTAngleGap5.GetTextDouble();
		}

		private void txtTAngleCnt5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TAngleCount = txtTAngleCnt5.GetTextInt();
		}

		private void txtTMaxDetail5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TMaxDetail = txtTMaxDetail5.GetTextDouble();
		}

		private void txtTMinDetail5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.TMinDetail = txtTMinDetail5.GetTextDouble();
		}

		private void txtT2AngleStart5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.T2AngleStart = txtT2AngleStart5.GetTextDouble();
		}

		private void txtAngleInspectPos5_TextNumberChanged(object sender, EventArgs e)
		{
			ModelInfo.Cam5_Data.AngleInspectPos = txtAngleInspectPos5.GetTextInt();
		}

		private void btnManualInspection5_Click(object sender, EventArgs e)
		{
			Stopwatch sw = new Stopwatch();
			sw.Reset();
			sw.Start();

			int nCode = Inspection5(nView - 1, nGrabLoopCnt[nView - 1], true);
			DisplayResultLabel(lblResult5, nCode);
			DisplayResultLabel(lblResultDetail5, nCode);

			sw.Stop();
			displayResult5.dbInspectTime = sw.ElapsedMilliseconds / 1000.0;

			// Display the new image
			ivCamViewer[nView - 1].Invalidate();
			ivMainViewer[nView - 1].Invalidate();
		}

		private void btnImageLoad5_Click(object sender, EventArgs e)
		{
			if (ImageLoad(nView - 1) == true)
			{
				if (lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Width == 3296)
				{
					Bitmap temp = lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4];
					Bitmap tbmp = Basic.BitmapResizeBilinear(lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4]);
					lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4] = tbmp;
					temp.Dispose();
				}
				ivCamViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivCamViewer[nView - 1].Invalidate();
				ivMainViewer[nView - 1].Image = (Bitmap)lstImage[nView - 1, nGrabLoopCnt[nView - 1] % 4].Clone();
				ivMainViewer[nView - 1].Invalidate();
			}
		}

		private void btnImageSave5_Click(object sender, EventArgs e)
		{
			ImageSave(nView - 1);
		}
		#endregion
	}
}
