namespace IVMCommon
{
	partial class FrmJustAMoment
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.lblMsg = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// lblMsg
			// 
			this.lblMsg.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblMsg.Font = new System.Drawing.Font("굴림", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
			this.lblMsg.Location = new System.Drawing.Point(12, 9);
			this.lblMsg.Name = "lblMsg";
			this.lblMsg.Size = new System.Drawing.Size(726, 64);
			this.lblMsg.TabIndex = 0;
			this.lblMsg.Text = "초기화중입니다. 잠시만 기다려 주십시오.";
			this.lblMsg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.lblMsg.UseWaitCursor = true;
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Navy;
			this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(749, 84);
			this.label1.TabIndex = 1;
			// 
			// FrmJustAMoment
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(749, 84);
			this.Controls.Add(this.lblMsg);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "FrmJustAMoment";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "잠시만 기다려주십시오.";
			this.TopMost = true;
			this.UseWaitCursor = true;
			this.Load += new System.EventHandler(this.FrmJustAMoment_Load);
			this.Shown += new System.EventHandler(this.FrmJustAMoment_Shown);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label lblMsg;
		private System.Windows.Forms.Label label1;
	}
}