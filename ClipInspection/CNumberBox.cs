using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IVMCommon;

namespace IVMCommon
{
	public partial class CNumberBox : UserControl
	{
		int nPrecision = 3;
		string txt = "";

		public event System.EventHandler TextNumberChanged = null;

		public CNumberBox()
		{
			InitializeComponent();
		}

		private void CNumberBox_Load(object sender, EventArgs e)
		{
			this.Size = textBox1.Size;
			textBox1.Text = txt;
			textBox1.TextAlign = HorizontalAlignment.Right;
		}

		private void CNumberBox_FontChanged(object sender, EventArgs e)
		{
			textBox1.Font = this.Font;
		}

		private void CNumberBox_BackColorChanged(object sender, EventArgs e)
		{
			textBox1.BackColor = this.BackColor;
		}

		private void CNumberBox_ForeColorChanged(object sender, EventArgs e)
		{
			textBox1.ForeColor = this.ForeColor;
		}

		private void CTextBox_Resize(object sender, EventArgs e)
		{
			this.Size = textBox1.Size;
		}

		private void textBox1_SizeChanged(object sender, EventArgs e)
		{
			this.Size = textBox1.Size;
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			try
			{
				if (textBox1.Text != "")
					double.Parse(textBox1.Text);
				txt = textBox1.Text;
			}
			catch
			{
				int ss = textBox1.SelectionStart;
				textBox1.Text = txt;
				textBox1.SelectionStart = ss - 1;
			}
			if (TextNumberChanged != null)
				TextNumberChanged(sender, e);
		}

		private void textBox1_Enter(object sender, EventArgs e)
		{
			try
			{
				if (textBox1.Text != "" && double.Parse(textBox1.Text) == 0)
					textBox1.Text = "";
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message);
			}
		}

		delegate void SetTextIntCollback(int value);
		public void SetTextInt(int value)
		{
			try
			{
				if (textBox1.InvokeRequired)
				{
					SetTextIntCollback d = new SetTextIntCollback(SetTextInt);
					this.Invoke(d, new object[] { value });
				}
				else
				{
					txt = value.ToString();
					textBox1.Text = txt;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SetTextInt] " + exc.ToString());
			}
		}

		delegate int GetTextIntCollback();
		public int GetTextInt()
		{
			try
			{
				if (textBox1.InvokeRequired)
				{
					GetTextIntCollback d = new GetTextIntCollback(GetTextInt);
					return (int)this.Invoke(d);
				}
				else
				{
					if (textBox1.Text != "")
						return (int)(double.Parse(textBox1.Text));
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SetTextInt] " + exc.ToString());
			}
			return 0;
		}

		delegate void SetTextDoubleCollback(double value);
		public void SetTextDouble(double value)
		{
			try
			{
				if (textBox1.InvokeRequired)
				{
					SetTextDoubleCollback d = new SetTextDoubleCollback(SetTextDouble);
					this.Invoke(d, new object[] { value });
				}
				else
				{
					string fmt = "0.";
					for (int i = 0; i < nPrecision; i++)
						fmt += "0";
					txt = value.ToString(fmt);
					textBox1.Text = txt;
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[SetTextDouble] " + exc.ToString());
			}
		}

		delegate double GetTextDoubleCollback();
		public double GetTextDouble()
		{
			try
			{
				if (textBox1.InvokeRequired)
				{
					GetTextDoubleCollback d = new GetTextDoubleCollback(GetTextDouble);
					return (double)this.Invoke(d);
				}
				else
				{
					if (textBox1.Text != "")
						return double.Parse(textBox1.Text);
				}
			}
			catch (Exception exc)
			{
				Log.AddLogMessage(Log.LogType.ERROR, 0, "[GetTextDouble] " + exc.ToString());
			}
			return 0;
		}

		public HorizontalAlignment TextAlign
		{
			get { return textBox1.TextAlign; }
			set { textBox1.TextAlign = value; }
		}

		public bool ReadOnly
		{
			get { return textBox1.ReadOnly; }
			set { textBox1.ReadOnly = value; }
		}

		public int Precision
		{
			get { return nPrecision; }
			set { nPrecision = value; }
		}

	}
}
