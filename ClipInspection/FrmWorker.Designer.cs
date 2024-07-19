namespace ClipInspection
{
	partial class FrmWorker
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
			this.lvWorkerList = new System.Windows.Forms.ListView();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnSelect = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnModify = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtName = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// lvWorkerList
			// 
			this.lvWorkerList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3});
			this.lvWorkerList.FullRowSelect = true;
			this.lvWorkerList.GridLines = true;
			this.lvWorkerList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvWorkerList.HideSelection = false;
			this.lvWorkerList.Location = new System.Drawing.Point(12, 12);
			this.lvWorkerList.MultiSelect = false;
			this.lvWorkerList.Name = "lvWorkerList";
			this.lvWorkerList.Size = new System.Drawing.Size(236, 208);
			this.lvWorkerList.TabIndex = 1;
			this.lvWorkerList.UseCompatibleStateImageBehavior = false;
			this.lvWorkerList.View = System.Windows.Forms.View.Details;
			this.lvWorkerList.SelectedIndexChanged += new System.EventHandler(this.lvWorkerList_SelectedIndexChanged);
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "작업자 이름";
			this.columnHeader3.Width = 200;
			// 
			// btnClose
			// 
			this.btnClose.Location = new System.Drawing.Point(425, 173);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 8;
			this.btnClose.Text = "닫기";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// btnSelect
			// 
			this.btnSelect.Location = new System.Drawing.Point(318, 173);
			this.btnSelect.Name = "btnSelect";
			this.btnSelect.Size = new System.Drawing.Size(75, 23);
			this.btnSelect.TabIndex = 7;
			this.btnSelect.Text = "선택";
			this.btnSelect.UseVisualStyleBackColor = true;
			this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.btnModify);
			this.groupBox1.Controls.Add(this.btnAdd);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.txtName);
			this.groupBox1.Location = new System.Drawing.Point(264, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(298, 124);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "상세 보기";
			// 
			// btnModify
			// 
			this.btnModify.Location = new System.Drawing.Point(161, 74);
			this.btnModify.Name = "btnModify";
			this.btnModify.Size = new System.Drawing.Size(75, 23);
			this.btnModify.TabIndex = 4;
			this.btnModify.Text = "수정";
			this.btnModify.UseVisualStyleBackColor = true;
			this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Location = new System.Drawing.Point(54, 74);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 3;
			this.btnAdd.Text = "추가";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(7, 35);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(29, 12);
			this.label2.TabIndex = 2;
			this.label2.Text = "이름";
			// 
			// txtName
			// 
			this.txtName.Location = new System.Drawing.Point(54, 32);
			this.txtName.Name = "txtName";
			this.txtName.Size = new System.Drawing.Size(228, 21);
			this.txtName.TabIndex = 2;
			// 
			// FrmWorker
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(577, 234);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.btnSelect);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.lvWorkerList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FrmWorker";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "작업자 선택 창";
			this.Load += new System.EventHandler(this.FrmWorker_Load);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmWorker_FormClosing);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView lvWorkerList;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnSelect;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnModify;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtName;
	}
}