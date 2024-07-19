namespace ClipInspection
{
	partial class FrmPassword
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
			this.btnRepassword = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnOK
			// 
			this.btnOK.Location = new System.Drawing.Point(84, 39);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(66, 23);
			this.btnOK.TabIndex = 7;
			this.btnOK.Text = "확 인";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(61, 12);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(89, 21);
			this.txtPassword.TabIndex = 1;
			// 
			// lblPassword
			// 
			this.lblPassword.AutoSize = true;
			this.lblPassword.Location = new System.Drawing.Point(26, 15);
			this.lblPassword.Name = "lblPassword";
			this.lblPassword.Size = new System.Drawing.Size(29, 12);
			this.lblPassword.TabIndex = 5;
			this.lblPassword.Text = "암호";
			// 
			// btnRepassword
			// 
			this.btnRepassword.Location = new System.Drawing.Point(12, 39);
			this.btnRepassword.Name = "btnRepassword";
			this.btnRepassword.Size = new System.Drawing.Size(66, 23);
			this.btnRepassword.TabIndex = 8;
			this.btnRepassword.Text = "암호 변경";
			this.btnRepassword.UseVisualStyleBackColor = true;
			this.btnRepassword.Click += new System.EventHandler(this.btnRepassword_Click);
			// 
			// FrmPassword
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(167, 75);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.btnRepassword);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.lblPassword);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "FrmPassword";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "암호 확인";
			this.Load += new System.EventHandler(this.FrmPassword_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.Label lblPassword;
		private System.Windows.Forms.Button btnRepassword;
	}
}