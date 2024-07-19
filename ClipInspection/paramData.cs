using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ClipInspection
{
	public class paramModelInfo
	{
		int nNo;
		string sName;

		public int No
		{
			get { return nNo; }
			set { nNo = value; }
		}

		public string Name
		{
			get { return sName; }
			set { sName = value; }
		}

		paramCam1_Data pCam1_Data;
		public paramCam1_Data Cam1_Data
		{
			get { return pCam1_Data; }
			set { pCam1_Data = value; }
		}

		paramCam2_Data pCam2_Data;
		public paramCam2_Data Cam2_Data
		{
			get { return pCam2_Data; }
			set { pCam2_Data = value; }
		}

		paramCam3_Data pCam3_Data;
		public paramCam3_Data Cam3_Data
		{
			get { return pCam3_Data; }
			set { pCam3_Data = value; }
		}

		paramCam4_Data pCam4_Data;
		public paramCam4_Data Cam4_Data
		{
			get { return pCam4_Data; }
			set { pCam4_Data = value; }
		}

		paramCam5_Data pCam5_Data;
		public paramCam5_Data Cam5_Data
		{
			get { return pCam5_Data; }
			set { pCam5_Data = value; }
		}

		int nGubunInspect4144;
		public int GubunInspect4144	// 0 : 무시, 1 : 41파이만 표면검사, 2 : 44파이만 표면검사
		{
			get { return nGubunInspect4144; }
			set { nGubunInspect4144 = value; }
		}

		public paramModelInfo()
		{
		}
	}

	public class paramCam1_Data
	{
		int nCamNo;
		public int CamNo
		{
			get { return nCamNo; }
			set { nCamNo = value; }
		}

		bool bUseInspect;
		public bool UseInspect
		{
			get { return bUseInspect; }
			set { bUseInspect = value; }
		}

		int nExpose;
		public int Expose
		{
			get { return nExpose; }
			set { nExpose = value; }
		}

		int nGain;
		public int Gain
		{
			get { return nGain; }
			set { nGain = value; }
		}

		int nDelayCapture;
		public int DelayCapture
		{
			get { return nDelayCapture; }
			set { nDelayCapture = value; }
		}

		int nThreshold;
		public int Threshold
		{
			get { return nThreshold; }
			set { nThreshold = value; }
		}

		Rectangle rtROI;
		public Rectangle ROI
		{
			get { return rtROI; }
			set { rtROI = value; }
		}

		double dbDefectWidthMax;
		public double DefectWidthMax
		{
			get { return dbDefectWidthMax; }
			set { dbDefectWidthMax = value; }
		}

		double dbDefectHeightMax;
		public double DefectHeightMax
		{
			get { return dbDefectHeightMax; }
			set { dbDefectHeightMax = value; }
		}

		List<paramMask_Data> pMask_Data = new List<paramMask_Data>();
		public List<paramMask_Data> Masks
		{
			get { return pMask_Data; }
			set { pMask_Data = value; }
		}

		Rectangle rtAlignROI;
		public Rectangle AlignROI
		{
			get { return rtAlignROI; }
			set { rtAlignROI = value; }
		}

		Point ptAlignCenter;
		public Point AlignCenter
		{
			get { return ptAlignCenter; }
			set { ptAlignCenter = value; }
		}

		public paramCam1_Data()
		{
		}
	}

	public class paramCam2_Data
	{
		int nCamNo;
		public int CamNo
		{
			get { return nCamNo; }
			set { nCamNo = value; }
		}

		bool bUseInspect;
		public bool UseInspect
		{
			get { return bUseInspect; }
			set { bUseInspect = value; }
		}

		int nExpose;
		public int Expose
		{
			get { return nExpose; }
			set { nExpose = value; }
		}

		int nGain;
		public int Gain
		{
			get { return nGain; }
			set { nGain = value; }
		}

		int nDelayCapture;
		public int DelayCapture
		{
			get { return nDelayCapture; }
			set { nDelayCapture = value; }
		}

		int nThreshold;
		public int Threshold
		{
			get { return nThreshold; }
			set { nThreshold = value; }
		}

		Rectangle rtROI;
		public Rectangle ROI
		{
			get { return rtROI; }
			set { rtROI = value; }
		}

		double dbDefectWidthMax;
		public double DefectWidthMax
		{
			get { return dbDefectWidthMax; }
			set { dbDefectWidthMax = value; }
		}

		double dbDefectHeightMax;
		public double DefectHeightMax
		{
			get { return dbDefectHeightMax; }
			set { dbDefectHeightMax = value; }
		}

		List<paramMask_Data> pMask_Data = new List<paramMask_Data>();
		public List<paramMask_Data> Masks
		{
			get { return pMask_Data; }
			set { pMask_Data = value; }
		}

		Rectangle rtAlignROI;
		public Rectangle AlignROI
		{
			get { return rtAlignROI; }
			set { rtAlignROI = value; }
		}

		Point ptAlignCenter;
		public Point AlignCenter
		{
			get { return ptAlignCenter; }
			set { ptAlignCenter = value; }
		}

		public paramCam2_Data()
		{
		}
	}

	public class paramCam3_Data
	{
		int nCamNo;
		public int CamNo
		{
			get { return nCamNo; }
			set { nCamNo = value; }
		}

		bool bUseInspect;
		public bool UseInspect
		{
			get { return bUseInspect; }
			set { bUseInspect = value; }
		}

		int nExpose;
		public int Expose
		{
			get { return nExpose; }
			set { nExpose = value; }
		}

		int nGain;
		public int Gain
		{
			get { return nGain; }
			set { nGain = value; }
		}

		int nDelayCapture;
		public int DelayCapture
		{
			get { return nDelayCapture; }
			set { nDelayCapture = value; }
		}

		int nThreshold;
		public int Threshold
		{
			get { return nThreshold; }
			set { nThreshold = value; }
		}

		Rectangle rtROI;
		public Rectangle ROI
		{
			get { return rtROI; }
			set { rtROI = value; }
		}

		double dbDefectWidthMax;
		public double DefectWidthMax
		{
			get { return dbDefectWidthMax; }
			set { dbDefectWidthMax = value; }
		}

		double dbDefectHeightMax;
		public double DefectHeightMax
		{
			get { return dbDefectHeightMax; }
			set { dbDefectHeightMax = value; }
		}

		List<paramMask_Data> pMask_Data = new List<paramMask_Data>();
		public List<paramMask_Data> Masks
		{
			get { return pMask_Data; }
			set { pMask_Data = value; }
		}

		Rectangle rtAlignROI;
		public Rectangle AlignROI
		{
			get { return rtAlignROI; }
			set { rtAlignROI = value; }
		}

		Point ptAlignCenter;
		public Point AlignCenter
		{
			get { return ptAlignCenter; }
			set { ptAlignCenter = value; }
		}

		public paramCam3_Data()
		{
		}
	}

	public class paramCam4_Data
	{
		int nCamNo;
		public int CamNo
		{
			get { return nCamNo; }
			set { nCamNo = value; }
		}

		bool bUseInspect;
		public bool UseInspect
		{
			get { return bUseInspect; }
			set { bUseInspect = value; }
		}

		int nExpose;
		public int Expose
		{
			get { return nExpose; }
			set { nExpose = value; }
		}

		int nGain;
		public int Gain
		{
			get { return nGain; }
			set { nGain = value; }
		}

		int nDelayCapture;
		public int DelayCapture
		{
			get { return nDelayCapture; }
			set { nDelayCapture = value; }
		}

		int nThreshold;
		public int Threshold
		{
			get { return nThreshold; }
			set { nThreshold = value; }
		}

		Rectangle rtROI;
		public Rectangle ROI
		{
			get { return rtROI; }
			set { rtROI = value; }
		}

		double dbDefectWidthMax;
		public double DefectWidthMax
		{
			get { return dbDefectWidthMax; }
			set { dbDefectWidthMax = value; }
		}

		double dbDefectHeightMax;
		public double DefectHeightMax
		{
			get { return dbDefectHeightMax; }
			set { dbDefectHeightMax = value; }
		}

		List<paramMask_Data> pMask_Data = new List<paramMask_Data>();
		public List<paramMask_Data> Masks
		{
			get { return pMask_Data; }
			set { pMask_Data = value; }
		}

		Rectangle rtAlignROI;
		public Rectangle AlignROI
		{
			get { return rtAlignROI; }
			set { rtAlignROI = value; }
		}

		Point ptAlignCenter;
		public Point AlignCenter
		{
			get { return ptAlignCenter; }
			set { ptAlignCenter = value; }
		}

		public paramCam4_Data()
		{
		}
	}

	public class paramMask_Data
	{
		int nNo;
		public int No
		{
			get { return nNo; }
			set { nNo = value; }
		}

		Rectangle rtROI;
		public Rectangle ROI
		{
			get { return rtROI; }
			set { rtROI = value; }
		}

		public paramMask_Data()
		{
		}
	}

	public class paramCam5_Data
	{
		int nCamNo;
		public int CamNo
		{
			get { return nCamNo; }
			set { nCamNo = value; }
		}

		bool bUseInspect;
		public bool UseInspect
		{
			get { return bUseInspect; }
			set { bUseInspect = value; }
		}

		bool bUseXLYL;
		public bool UseXLYL
		{
			get { return bUseXLYL; }
			set { bUseXLYL = value; }
		}

		bool bUseLHRH;
		public bool UseLHRH
		{
			get { return bUseLHRH; }
			set { bUseLHRH = value; }
		}

		bool bUseT;
		public bool UseT
		{
			get { return bUseT; }
			set { bUseT = value; }
		}

		bool bUseP;	// P : Projection
		public bool UseP
		{
			get { return bUseP; }
			set { bUseP = value; }
		}

		bool bUseT2;
		public bool UseT2
		{
			get { return bUseT2; }
			set { bUseT2 = value; }
		}

		bool bUseWL;
		public bool UseWL
		{
			get { return bUseWL; }
			set { bUseWL = value; }
		}

		int nThreshold;
		public int Threshold
		{
			get { return nThreshold; }
			set { nThreshold = value; }
		}

		Rectangle rtROI;
		public Rectangle ROI
		{
			get { return rtROI; }
			set { rtROI = value; }
		}

		double dbXLMax;
		public double XLMax
		{
			get { return dbXLMax; }
			set { dbXLMax = value; }
		}

		double dbXL;
		public double XL
		{
			get { return dbXL; }
			set { dbXL = value; }
		}

		double dbXLMin;
		public double XLMin
		{
			get { return dbXLMin; }
			set { dbXLMin = value; }
		}

		double dbYLMax;
		public double YLMax
		{
			get { return dbYLMax; }
			set { dbYLMax = value; }
		}

		double dbYL;
		public double YL
		{
			get { return dbYL; }
			set { dbYL = value; }
		}

		double dbYLMin;
		public double YLMin
		{
			get { return dbYLMin; }
			set { dbYLMin = value; }
		}

		double dbLHMax;
		public double LHMax
		{
			get { return dbLHMax; }
			set { dbLHMax = value; }
		}

		double dbLH;
		public double LH
		{
			get { return dbLH; }
			set { dbLH = value; }
		}

		double dbLHMin;
		public double LHMin
		{
			get { return dbLHMin; }
			set { dbLHMin = value; }
		}

		double dbRHMax;
		public double RHMax
		{
			get { return dbRHMax; }
			set { dbRHMax = value; }
		}

		double dbRH;
		public double RH
		{
			get { return dbRH; }
			set { dbRH = value; }
		}

		double dbRHMin;
		public double RHMin
		{
			get { return dbRHMin; }
			set { dbRHMin = value; }
		}

		double dbTMax;
		public double TMax
		{
			get { return dbTMax; }
			set { dbTMax = value; }
		}

		double dbT;
		public double T
		{
			get { return dbT; }
			set { dbT = value; }
		}

		double dbTMin;
		public double TMin
		{
			get { return dbTMin; }
			set { dbTMin = value; }
		}

		double dbPMax;
		public double PMax
		{
			get { return dbPMax; }
			set { dbPMax = value; }
		}

		double dbP;
		public double P
		{
			get { return dbP; }
			set { dbP = value; }
		}

		double dbPMin;
		public double PMin
		{
			get { return dbPMin; }
			set { dbPMin = value; }
		}

		double dbT2Max;
		public double T2Max
		{
			get { return dbT2Max; }
			set { dbT2Max = value; }
		}

		double dbT2;
		public double T2
		{
			get { return dbT2; }
			set { dbT2 = value; }
		}

		double dbT2Min;
		public double T2Min
		{
			get { return dbT2Min; }
			set { dbT2Min = value; }
		}

		double dbWLMax;
		public double WLMax
		{
			get { return dbWLMax; }
			set { dbWLMax = value; }
		}

		double dbWL;
		public double WL
		{
			get { return dbWL; }
			set { dbWL = value; }
		}

		double dbWLMin;
		public double WLMin
		{
			get { return dbWLMin; }
			set { dbWLMin = value; }
		}

		double dbXLOffset;
		public double XLOffset
		{
			get { return dbXLOffset; }
			set { dbXLOffset = value; }
		}

		double dbYLOffset;
		public double YLOffset
		{
			get { return dbYLOffset; }
			set { dbYLOffset = value; }
		}

		double dbLHOffset;
		public double LHOffset
		{
			get { return dbLHOffset; }
			set { dbLHOffset = value; }
		}

		double dbRHOffset;
		public double RHOffset
		{
			get { return dbRHOffset; }
			set { dbRHOffset = value; }
		}

		double dbTOffset;
		public double TOffset
		{
			get { return dbTOffset; }
			set { dbTOffset = value; }
		}

		double dbPOffset;
		public double POffset
		{
			get { return dbPOffset; }
			set { dbPOffset = value; }
		}

		double dbT2Offset;
		public double T2Offset
		{
			get { return dbT2Offset; }
			set { dbT2Offset = value; }
		}

		double dbWLOffset;
		public double WLOffset
		{
			get { return dbWLOffset; }
			set { dbWLOffset = value; }
		}

		double dbTAngleStart;
		public double TAngleStart
		{
			get { return dbTAngleStart; }
			set { dbTAngleStart = value; }
		}

		double dbTAngleGap;
		public double TAngleGap
		{
			get { return dbTAngleGap; }
			set { dbTAngleGap = value; }
		}

		int nTAngleCount;
		public int TAngleCount
		{
			get { return nTAngleCount; }
			set { nTAngleCount = value; }
		}

		double dbTMaxDetail;
		public double TMaxDetail
		{
			get { return dbTMaxDetail; }
			set { dbTMaxDetail = value; }
		}

		double dbTMinDetail;
		public double TMinDetail
		{
			get { return dbTMinDetail; }
			set { dbTMinDetail = value; }
		}

		double dbT2AngleStart;
		public double T2AngleStart
		{
			get { return dbT2AngleStart; }
			set { dbT2AngleStart = value; }
		}

		int nAngleInspectPos;
		public int AngleInspectPos
		{
			get { return nAngleInspectPos; }
			set { nAngleInspectPos = value; }
		}

		double dbRealLength;
		public double RealLength
		{
			get { return dbRealLength; }
			set { dbRealLength = value; }
		}

		double dbCalcLength;
		public double CalcLength
		{
			get { return dbCalcLength; }
			set { dbCalcLength = value; }
		}

		double dbMmPerPixel;
		public double MmPerPixel
		{
			get { return dbMmPerPixel; }
			set { dbMmPerPixel = value; }
		}

		int nViewUnit;
		public int ViewUnit
		{
			get { return nViewUnit; }
			set { nViewUnit = value; }
		}

		public paramCam5_Data()
		{
		}
	}

	public class paramResult1
	{
		public List<Rectangle> lstErrRect = new List<Rectangle>();
		public List<int> lstErrArea = new List<int>();
		public double dbInspectTime;
		public bool bResult;
	}

	public class paramResult2
	{
		public List<Rectangle> lstErrRect = new List<Rectangle>();
		public List<int> lstErrArea = new List<int>();
		public double dbInspectTime;
		public bool bResult;
	}

	public class paramResult3
	{
		public List<Rectangle> lstErrRect = new List<Rectangle>();
		public List<int> lstErrArea = new List<int>();
		public double dbInspectTime;
		public bool bResult;
	}

	public class paramResult4
	{
		public List<Rectangle> lstErrRect = new List<Rectangle>();
		public List<int> lstErrArea = new List<int>();
		public double dbInspectTime;
		public bool bResult;
	}

	public class paramResult5
	{
		public Rectangle rectInner;
		public Point ptCenter;
		public Rectangle rectOuter;
		public Point ptOutCenter;
		public Point ptLH;
		public Point ptRH;
		public Point ptLH2;
		public Point ptRH2;
		public Point ptLHEnd;
		public Point ptRHEnd;
		public Point ptPEnd1;
		public Point ptPEnd2;
		public Point ptWLEnd1;
		public Point ptWLEnd2;
		public Point[] ptT21 = new Point[2];
		public Point[] ptT22 = new Point[2];
		public double dbLHAngle;
		public double dbRHAngle;
		public double dbCenterAngle;
		//public List<Point> lstPoint = new List<Point>();
		//public List<Point> lstTPoint = new List<Point>();
		public Point[] ptTPoint1 = null;
		public Point[] ptTPoint2 = null;
		public bool[] bTPointDetail = null;
		public Point ptXL1;
		public Point ptXL2;
		public Point ptYL1;
		public Point ptYL2;

		public double dbXLPixel;
		public double dbYLPixel;
		public double dbLHPixel;
		public double dbRHPixel;
		public double dbTPixel;
		public double dbPPixel;
		public double[] dbT2Pixel = new double[2];
		public double dbWLPixel;
		public double dbRValueReal;

		public double dbXLValue;
		public double dbYLValue;
		public double dbLHValue;
		public double dbRHValue;
		public double dbTValue;
		public double dbTValueMax;
		public double dbTValueMin;
		public double dbPValue;
		public double[] dbT2Value = new double[2];
		public double dbWLValue;
		public double dbRValue;

		public int nXLNGCnt;
		public int nYLNGCnt;
		public int nLHNGCnt;
		public int nRHNGCnt;
		public int nTNGCnt;
		public int nPNGCnt;
		public int nT2NGCnt;
		public int nWLNGCnt;

		public bool bXL;
		public bool bYL;
		public bool bLH;
		public bool bRH;
		public bool bT;
		public bool bP;
		public bool bT2;
		public bool bWL;

		public double dbInspectTime;
		public bool bResult;
	}
}
