using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClipInspection
{
	public class paramINI
	{
		string strNGImagePath;
		public string NGImagePath
		{
			get { return strNGImagePath; }
			set { strNGImagePath = value; }
		}

		int nIsNGImage;
		public int IsNGImage
		{
			get { return nIsNGImage; }
			set { nIsNGImage = value; }
		}

		string strSaveDataPath;
		public string SaveDataPath
		{
			get { return strSaveDataPath; }
			set { strSaveDataPath = value; }
		}

		bool bIsSaveData;
		public bool IsSaveData
		{
			get { return bIsSaveData; }
			set { bIsSaveData = value; }
		}

		string strConfigPath;
		public string ConfigPath
		{
			get { return strConfigPath; }
			set { strConfigPath = value; }
		}

		int nNGImageAliveDays;
		public int NGImageAliveDays
		{
			get { return nNGImageAliveDays; }
			set { nNGImageAliveDays = value; }
		}

		int nLastModelNo;
		public int LastModelNo
		{
			get { return nLastModelNo; }
			set { nLastModelNo = value; }
		}

		string strPassword;
		public string Password
		{
			get { return strPassword; }
			set { strPassword = value; }
		}

		List<paramCamInfo> camInfo = new List<paramCamInfo>();
		public List<paramCamInfo> CamInfoCol
		{
			get { return camInfo; }
			set { camInfo = value; }
		}

		public paramINI()
		{
		}
	}

	public class paramCamInfo
	{
		string strDisplayName;
		public string DisplayName
		{
			get { return strDisplayName; }
			set { strDisplayName = value; }
		}

		int nDriveIndex;
		public int DriveIndex
		{
			get { return nDriveIndex; }
			set { nDriveIndex = value; }
		}

		string strConnector;
		public string ConnectorName
		{
			get { return strConnector; }
			set { strConnector = value; }
		}

		string strCamFile;
		public string CamFile
		{
			get { return strCamFile; }
			set { strCamFile = value; }
		}

		int nSurfaceCount;
		public int SurfaceCount
		{
			get { return nSurfaceCount; }
			set { nSurfaceCount = value; }
		}

		string strColorFormat;
		public string ColorFormat
		{
			get { return strColorFormat; }
			set { strColorFormat = value; }
		}

		string strBoardTopology;
		public string BoardTopology
		{
			get { return strBoardTopology; }
			set { strBoardTopology = value; }
		}

		double dbMmPerPixel;
		public double MmPerPixel
		{
			get { return dbMmPerPixel; }
			set { dbMmPerPixel = value; }
		}

		public paramCamInfo()
		{
		}
	}
}
