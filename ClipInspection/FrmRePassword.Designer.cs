namespace ClipInspection
{
	partial class FrmRePassword
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
			this.btnOK = new System.Windows.Forms.Button();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.lblPassword = new System.Windows.Forms.Label();
			this.txtNewPassword = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.txtNewPasswordConfirm = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnOK
			// 
			this.btnOK.Location = new System.Drawing.Point(84, 93);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(66, 23);
			this.btnOK.TabIndex = 7;
			this.btnOK.Text = "변 경";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(84, 12);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(89, 21);
			this.txtPassword.TabIndex = 1;
			// 
			// lblPassword
			// 
			this.lblPassword.AutoSize = true;
			this.lblPassword.Location = new System.Drawing.Point(21, 15);
			this.lblPassword.Name = "lblPassword";
			this.lblPassword.Size = new System.Drawing.Size(57, 12);
			this.lblPassword.TabIndex = 5;
			this.lblPassword.Text = "이전 암호";
			// 
			// txtNewPassword
			// 
			this.txtNewPassword.Location = new System.Drawing.Point(84, 39);
			this.txtNewPassword.Name = "txtNewPassword";
			this.txtNewPassword.PasswordChar = '*';
			this.txtNewPassword.Size = new System.Drawing.Size(89, 21);
			this.txtNewPassword.TabIndex = 8;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(21, 42);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(45, 12);
			this.label1.TabIndex = 9;
			this.label1.Text = "새 암호";
			// 
			// txtNewPasswordConfirm
			// 
			this.txtNewPasswordConfirm.Location = new System.Drawing.Point(84, 66);
			this.txtNewPasswordConfirm.Name = "txtNewPasswordConfirm";
			this.txtNewPasswordConfirm.PasswordChar = '*';
			this.txtNewPasswordConfirm.Size = new System.Drawing.Size(89, 21);
			this.txtNewPasswordConfirm.TabIndex = 10;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(21, 69);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(57, 12);
			this.label2.TabIndex = 11;
			this.label2.Text = "암호 확인";
			// 
			// FrmRePassword
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(191, 127);
			this.Controls.Add(this.txtNewPasswordConfirm);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.txtNewPassword);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.lblPassword);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "FrmRePassword";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "암호 변경";
			this.Load += new System.EventHandler(this.FrmRePassword_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.Label lblPassword;
		private System.Windows.Forms.TextBox txtNewPassword;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtNewPasswordConfirm;
		private System.Windows.Forms.Label label2;
	}
}