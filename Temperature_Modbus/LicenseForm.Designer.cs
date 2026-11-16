namespace Temperature_Modbus
{
    partial class LicenseForm
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
            dgvLicenses = new DataGridView();
            lblLicenseStatus = new Label();
            btnOpenLicense = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvLicenses).BeginInit();
            SuspendLayout();
            // 
            // dgvLicenses
            // 
            dgvLicenses.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvLicenses.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLicenses.Location = new Point(20, 20);
            dgvLicenses.Name = "dgvLicenses";
            dgvLicenses.Size = new Size(890, 450);
            dgvLicenses.TabIndex = 0;
            // 
            // lblLicenseStatus
            // 
            lblLicenseStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblLicenseStatus.AutoSize = true;
            lblLicenseStatus.BorderStyle = BorderStyle.Fixed3D;
            lblLicenseStatus.Font = new Font("맑은 고딕", 15F, FontStyle.Bold);
            lblLicenseStatus.Location = new Point(20, 505);
            lblLicenseStatus.Name = "lblLicenseStatus";
            lblLicenseStatus.Size = new Size(161, 30);
            lblLicenseStatus.TabIndex = 1;
            lblLicenseStatus.Text = "선택된 라이선스";
            // 
            // btnOpenLicense
            // 
            btnOpenLicense.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOpenLicense.Font = new Font("맑은 고딕", 15F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btnOpenLicense.Location = new Point(721, 485);
            btnOpenLicense.Name = "btnOpenLicense";
            btnOpenLicense.Size = new Size(189, 50);
            btnOpenLicense.TabIndex = 2;
            btnOpenLicense.Text = "선택한 원문 열기";
            btnOpenLicense.UseVisualStyleBackColor = true;
            btnOpenLicense.Click += btnOpenLicense_Click;
            // 
            // LicenseForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(934, 561);
            Controls.Add(btnOpenLicense);
            Controls.Add(lblLicenseStatus);
            Controls.Add(dgvLicenses);
            Name = "LicenseForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "오픈소스 라이선스";
            Shown += LicenseForm_Shown;
            ((System.ComponentModel.ISupportInitialize)dgvLicenses).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvLicenses;
        private Label lblLicenseStatus;
        private Button btnOpenLicense;
    }
}