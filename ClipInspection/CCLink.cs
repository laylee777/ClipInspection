using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace Mitsubishi
{
	class CCLink
	{
		public static short DEVTYPE_B = 23;
		public static short DEVTYPE_W = 24;
		public static short DEVTYPE_D = 13;

		[DllImport("MDFUNC32.dll")]
		public static extern short mdOpen(short Channel, short Mode, out Int32 Path);
		[DllImport("MDFUNC32.dll")]
		public static extern short mdClose(Int32 Path);
		[DllImport("MDFUNC32.dll")]
		public static extern short mdSend(Int32 Path, short Stno, short Devtype, short devno, ref short size, ref short buf);
		[DllImport("MDFUNC32.dll")]
		public static extern short mdReceive(Int32 Path, short Stno, short Devtype, short devno, ref short size, ref short buf);
		[DllImport("MDFUNC32.dll")]
		public static extern short mdDevSet(Int32 Path, short Stno, short Devtype, short devno);
		[DllImport("MDFUNC32.dll")]
		public static extern short mdDevRst(Int32 Path, short Stno, short Devtype, short devno);

		[DllImport("kernel32")]
		public static extern UInt32 GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport("kernel32")]
		public static extern UInt32 WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

		public Int32 nPath;								//CC link오픈할 때 쓰여 지는 값...
		public short nChannel;
		public short nStationNo;							//Station No.
		public ushort nDeviceType;						//device type
		public short nDeviceNo;								//device No.
		private short nSize;									//data size
		private short[] nSendBuf = new short[10];
		private short[] nRecvBuf = new short[10];
		private string strPath = @"CC-LINK.ivm";

		public CCLink(string path)
		{
			strPath = path;
			Load();
		}

		public bool portOpen()
		{
			bool bSuccess;

			bSuccess = mdOpen(nChannel, nStationNo, out nPath) > 0 ? true : false;
			return bSuccess;
		}

		public void portClose()
		{
			mdClose(nPath);
		}

		public bool dataSendBit(Int32 path, short sno, short devNo, bool data)
		{
			bool bSuccess;
			if (data == true)
				bSuccess = mdDevSet(path, sno, DEVTYPE_B, devNo) > 0 ? true : false;
			else
				bSuccess = mdDevRst(path, sno, DEVTYPE_B, devNo) > 0 ? true : false;
			return bSuccess;
		}

		//Mutex mutexDataSend = new Mutex();
		public bool dataSend(Int32 path, short sno, short devType, short devNo, short size, short[] sendBuf)
		{
			//mutexDataSend.WaitOne();
			bool bSuccess;
			if (devType == DEVTYPE_W)
				size *= 2;
			bSuccess = mdDevSet(path, sno, devType, devNo) > 0 ? true : false;
			bSuccess = mdSend(path, sno, devType, devNo, ref size, ref sendBuf[0]) > 0 ? true : false;
			//mutexDataSend.ReleaseMutex();
			return bSuccess;
		}

		//Mutex mutexDataReceive = new Mutex();
		public bool dataReceive(Int32 path, short sno, short devType, short devNo, short size, short[] revBuf)
		{
			//mutexDataReceive.WaitOne();
			bool bSuccess;
			if (devType == DEVTYPE_W || devType == DEVTYPE_D)
				size *= 2;
			bSuccess = mdDevSet(path, sno, devType, devNo) > 0 ? true : false;
			bSuccess = mdReceive(path, sno, devType, devNo, ref size, ref revBuf[0]) > 0 ? true : false;
			//mutexDataReceive.WaitOne();
			return bSuccess;
		}

		public void Save()
		{
			string str;

			str = string.Format("{0}", nChannel);
			WritePrivateProfileString("CC-Link Data", "CHANNEL", str, strPath);
			str = string.Format("{0}", nSize);
			WritePrivateProfileString("CC-Link Data", "DATA SIZE", str, strPath);
			str = string.Format("{0}", nStationNo);
			WritePrivateProfileString("CC-Link Data", "STATION NO", str, strPath);
			str = string.Format("{0}", nDeviceNo);
			WritePrivateProfileString("CC-Link Data", "DEIVCE NO", str, strPath);
			str = string.Format("{0}", nDeviceType);
			WritePrivateProfileString("CC-Link Data", "DEIVCE TYPE", str, strPath);
		}

		public void Load()
		{
			nChannel = (short)GetPrivateProfileInt("CC-Link Data", "CHANNEL", 0, strPath);
			nSize = (short)GetPrivateProfileInt("CC-Link Data", "DATA SIZE", 0, strPath);
			nStationNo = (short)GetPrivateProfileInt("CC-Link Data", "STATION NO", 0, strPath);
			nDeviceNo = (short)GetPrivateProfileInt("CC-Link Data", "DEIVCE NO", 0, strPath);
			nDeviceType = (ushort)GetPrivateProfileInt("CC-Link Data", "DEVICE TYPE", 0, strPath);
		}
	}
}
