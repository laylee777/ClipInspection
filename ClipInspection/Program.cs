using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ClipInspection
{
	static class Program
	{
		/// <summary>
		/// 해당 응용 프로그램의 주 진입점입니다.
		/// </summary>
		[STAThread]
		static void Main()
		{
			int cnt = 0;
			string strCur = Process.GetCurrentProcess().ProcessName;
			if (strCur.IndexOf('.') >= 0)
				strCur = strCur.Substring(0, strCur.IndexOf('.'));
			foreach (Process proc in Process.GetProcesses())
			{
				string str = proc.ProcessName;
				if (str.IndexOf('.') >= 0)
					str = str.Substring(0, str.IndexOf('.'));
				if (str == strCur)
					cnt++;
				if (cnt > 1)
				{
					// 프로세스 목록에 의한 다중실행 방지
					MessageBox.Show("이미 실행되고 있습니다.\r\n화면에 없을 경우, [작업관리자]에서 강제 종료 후 다시 실행해 주십시오.", "다중실행방지");
					return;
				}
			}
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FrmMain());
		}
	}
}
