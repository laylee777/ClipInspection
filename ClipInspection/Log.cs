using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace IVMCommon
{
	public class Log
	{
		public enum LogType { CRITICAL, ERROR, INFORMATION, RESUME, START, STOP, SUSPEND, TRANSFER, VERBOSE, WARNING };

		private static TraceSource tsSource;
		private static StreamWriter[] swLogFile;
		private static TextWriterTraceListener[] traceListener;
		private static string[] strFileName;
		private static int nCounter;
		private static int nSelectListener;
		private static int nCntListener;
		private static int nDivideFileSize;

		public static void InitializeLog(string str)
		{
			nCounter = 0;
			nSelectListener = 0;
			nCntListener = 0;
			// 여기의 사이즈를 바꾸면 된다.. 
			// 단위는 Byte 이다.
			nDivideFileSize = 1000000;
			
			strFileName = new string[2];
			swLogFile = new StreamWriter[2];
			traceListener = new TextWriterTraceListener[2]; 
			
			tsSource = new TraceSource(str);
			tsSource.Listeners.Clear();
			
			SetLogFileName(nSelectListener);
			InitTraceListener(nSelectListener);
			
			tsSource.Switch.Level = SourceLevels.All;
		}

		private static void SetLogFileName(int index)
		{
            Directory.CreateDirectory(@"C:\IVM\LOG");
			DateTime dt = DateTime.Now;
			strFileName[index] = Path.Combine("C:\\IVM\\LOG", string.Format("Log_{0:00}월{1:00}일{2:00}시{3:00}분{4:00}초.txt", dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second));
		}

		private static void InitTraceListener(int index)
		{
			swLogFile[index] = new StreamWriter(strFileName[index], false, Encoding.Default);
			traceListener[index] = new TextWriterTraceListener(swLogFile[index]);
			traceListener[index].TraceOutputOptions |= TraceOptions.DateTime | TraceOptions.ThreadId;

			if (nCntListener >= 2)
			{
				tsSource.Listeners.RemoveAt(index);
			}
			tsSource.Listeners.Insert(index, traceListener[index]);

			nCntListener++;
		}

		public static void CloseLog()
		{
			tsSource.Listeners[nSelectListener].Flush();
			tsSource.Listeners.Clear();
			tsSource.Close();
		}

		public static void AddLogMessage(LogType type, int id, string strtmp)
		{
			switch (type)
			{
				case LogType.CRITICAL:
					tsSource.TraceEvent(TraceEventType.Critical, id, strtmp);
					break;
				case LogType.ERROR:
					tsSource.TraceEvent(TraceEventType.Error, id, strtmp);
					break;
				case LogType.INFORMATION:
					tsSource.TraceEvent(TraceEventType.Information, id, strtmp);
					break;
				case LogType.RESUME:
					tsSource.TraceEvent(TraceEventType.Resume, id, strtmp);
					break;
				case LogType.START:
					tsSource.TraceEvent(TraceEventType.Start, id, strtmp);
					break;
				case LogType.STOP:
					tsSource.TraceEvent(TraceEventType.Stop, id, strtmp);
					break;
				case LogType.SUSPEND:
					tsSource.TraceEvent(TraceEventType.Suspend, id, strtmp);
					break;
				case LogType.TRANSFER:
					tsSource.TraceEvent(TraceEventType.Transfer, id, strtmp);
					break;
				case LogType.VERBOSE:
					tsSource.TraceEvent(TraceEventType.Verbose, id, strtmp);
					break;
				case LogType.WARNING:
					tsSource.TraceEvent(TraceEventType.Warning, id, strtmp);
					break;
				default:
					tsSource.TraceEvent(TraceEventType.Error, id, strtmp);
					break;
			}

			nCounter++;

			if (nCounter % 20 == 0)
			{
				FlushLogFile();
			}
		}

		public static void FlushLogFile()
		{
			tsSource.Flush();
			nCounter = 0;

			DateTime dt = DateTime.Now;
			FileInfo fiLogFile = new FileInfo(strFileName[nSelectListener]);

			// 여기의 != dt.Day 를 바꾸면 된다. Ex) dt.Hour, dt.Minute, 
			// 설마 dt.Month, dt.Year 이걸로 바꾸는 사람은...
			if (fiLogFile.Length >= nDivideFileSize || fiLogFile.LastWriteTime.Day != dt.Day)
			{
				SaveLogFile();
			}
		}

		private static void SaveLogFile()
		{
			SetLogFileName(1 - nSelectListener);
			InitTraceListener(1 - nSelectListener);

			nSelectListener = 1 - nSelectListener;

			traceListener[1 - nSelectListener].Flush();
			traceListener[1 - nSelectListener].Close();
			swLogFile[1 - nSelectListener].Close();
		}
	}
}
