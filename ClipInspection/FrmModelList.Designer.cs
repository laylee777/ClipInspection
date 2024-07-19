namespace ClipInspection
{
	partial class FrmModelList
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
			this.lvModelList = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.txtNo = new System.Windows.Forms.TextBox();
			this.txtName = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnModify = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.btnSelect = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnModifyNo = new System.Windows.Forms.Button();
			this.txtModifyNo = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// lvModelList
			// 
			this.lvModelList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.lvModelList.FullRowSelect = true;
			this.lvModelList.GridLines = true;
			this.lvModelList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvModelList.HideSelection = false;
			this.lvModelList.Location = new System.Drawing.Point(12, 12);
			this.lvModelList.MultiSelect = false;
			this.lvModelList.Name = "lvModelList";
			this.lvModelList.Size = new System.Drawing.Size(289, 208);
			this.lvModelList.TabIndex = 0;
			this.lvModelList.UseCompatibleStateImageBehavior = false;
			this.lvModelList.View = System.Windows.Forms.View.Details;
			this.lvModelList.SelectedIndexChanged += new System.EventHandler(this.lvModelList_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "번호";
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "제품명";
			this.columnHeader2.Width = 200;
			// 
			// txtNo
			// 
			this.txtNo.Location = new System.Drawing.Point(54, 17);
			this.txtNo.Name = "txtNo";
			this.txtNo.ReadOnly = true;
			this.txtNo.Size = new System.Drawing.Size(52, 21);
			this.txtNo.TabIndex = 1;
			// 
			// txtName
			// 
			this.txtName.Location = new System.Drawing.Point(54, 44);
			this.txtName.Name = "txtName";
			this.txtName.Size = new System.Drawing.Size(228, 21);
			this.txtName.TabIndex = 2;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.txtModifyNo);
			this.groupBox1.Controls.Add(this.btnModifyNo);
			this.groupBox1.Controls.Add(this.btnModify);
			this.groupBox1.Controls.Add(this.btnAdd);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.txtName);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.txtNo);
			this.groupBox1.Location = new System.Drawing.Point(308, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(298, 124);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "상세 보기";
			// 
			// btnModify
			// 
			this.btnModify.Location = new System.Drawing.Point(161, 86);
			this.btnModify.Name = "btnModify";
			this.btnModify.Size = new System.Drawing.Size(75, 23);
			this.btnModify.TabIndex = 4;
			this.btnModify.Text = "수정";
			this.btnModify.UseVisualStyleBackColor = true;
			this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Location = new System.Drawing.Point(54, 86);
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
			this.label2.Location = new System.Drawing.Point(7, 47);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(41, 12);
			this.label2.TabIndex = 2;
			this.label2.Text = "제품명";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(7, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "번호";
			// 
			// btnSelect
			// 
			this.btnSelect.Location = new System.Drawing.Point(362, 173);
			this.btnSelect.Name = "btnSelect";
			this.btnSelect.Size = new System.Drawing.Size(75, 23);
			this.btnSelect.TabIndex = 4;
			this.btnSelect.Text = "선택";
			this.btnSelect.UseVisualStyleBackColor = true;
			this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
			// 
			// btnClose
			// 
			this.btnClose.Location = new System.Drawing.Point(469, 173);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 5;
			this.btnClose.Text = "닫기";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// btnModifyNo
			// 
			this.btnModifyNo.Location = new System.Drawing.Point(149, 15);
			this.btnModifyNo.Name = "btnModifyNo";
			this.btnModifyNo.Size = new System.Drawing.Size(75, 23);
			this.btnModifyNo.TabIndex = 5;
			this.btnModifyNo.Text = "번호 변경";
			this.btnModifyNo.UseVisualStyleBackColor = true;
			this.btnModifyNo.Click += new System.EventHandler(this.btnModifyNo_Click);
			// 
			// txtModifyNo
			// 
			this.txtModifyNo.Location = new System.Drawing.Point(230, 17);
			this.txtModifyNo.Name = "txtModifyNo";
			this.txtModifyNo.Size = new System.Drawing.Size(52, 21);
			this.txtModifyNo.TabIndex = 6;
			// 
			// FrmModelList
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(619, 234);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.btnSelect);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.lvModelList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FrmModelList";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "제품 선택 창";
			this.Load += new System.EventHandler(this.FrmModelList_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView lvModelList;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.TextBox txtNo;
		private System.Windows.Forms.TextBox txtName;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnModify;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnSelect;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.TextBox txtModifyNo;
		private System.Windows.Forms.Button btnModifyNo;
	}
}