namespace ClipInspection
{
	partial class FrmReport
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
			this.lstReport = new System.Windows.Forms.ListView();
			this.lstStatistics = new System.Windows.Forms.ListView();
			this.SuspendLayout();
			// 
			// lstReport
			// 
			this.lstReport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.lstReport.FullRowSelect = true;
			this.lstReport.GridLines = true;
			this.lstReport.LabelEdit = true;
			this.lstReport.Location = new System.Drawing.Point(12, 59);
			this.lstReport.MultiSelect = false;
			this.lstReport.Name = "lstReport";
			this.lstReport.Size = new System.Drawing.Size(664, 397);
			this.lstReport.TabIndex = 1;
			this.lstReport.UseCompatibleStateImageBehavior = false;
			this.lstReport.View = System.Windows.Forms.View.Details;
			// 
			// lstStatistics
			// 
			this.lstStatistics.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.lstStatistics.FullRowSelect = true;
			this.lstStatistics.GridLines = true;
			this.lstStatistics.LabelEdit = true;
			this.lstStatistics.Location = new System.Drawing.Point(12, 462);
			this.lstStatistics.MultiSelect = false;
			this.lstStatistics.Name = "lstStatistics";
			this.lstStatistics.Size = new System.Drawing.Size(664, 93);
			this.lstStatistics.TabIndex = 2;
			this.lstStatistics.UseCompatibleStateImageBehavior = false;
			this.lstStatistics.View = System.Windows.Forms.View.Details;
			// 
			// FrmReport
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(688, 567);
			this.Controls.Add(this.lstStatistics);
			this.Controls.Add(this.lstReport);
			this.Name = "FrmReport";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "리포트";
			this.TopMost = true;
			this.Load += new System.EventHandler(this.FrmReport_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView lstReport;
		private System.Windows.Forms.ListView lstStatistics;
	}
}