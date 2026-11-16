namespace Temperature_Modbus
{
    partial class AlarmHistoryForm
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
            lblHistoryTitle = new Label();
            lblToDate = new Label();
            lblAlarmType = new Label();
            dtpFromDate = new DateTimePicker();
            dtpToDate = new DateTimePicker();
            cmbAlarmType = new ComboBox();
            btnSearch = new Button();
            btnClose = new Button();
            pnlSearch = new Panel();
            lblFromDate = new Label();
            dgvAlarmHistory = new DataGridView();
            colHistoryAlarmId = new DataGridViewTextBoxColumn();
            colHistoryDeviceName = new DataGridViewTextBoxColumn();
            colHistoryCategory = new DataGridViewTextBoxColumn();
            colHistoryMessage = new DataGridViewTextBoxColumn();
            colHistoryTemperature = new DataGridViewTextBoxColumn();
            colHistoryOccurredAt = new DataGridViewTextBoxColumn();
            colHistoryAcknowledgedAt = new DataGridViewTextBoxColumn();
            colHistoryRecoveredAt = new DataGridViewTextBoxColumn();
            colHistoryKakaoStatus = new DataGridViewTextBoxColumn();
            lblHistoryCount = new Label();
            pnlSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAlarmHistory).BeginInit();
            SuspendLayout();
            // 
            // lblHistoryTitle
            // 
            lblHistoryTitle.AutoSize = true;
            lblHistoryTitle.BorderStyle = BorderStyle.Fixed3D;
            lblHistoryTitle.Font = new Font("맑은 고딕", 18F, FontStyle.Bold);
            lblHistoryTitle.Location = new Point(20, 20);
            lblHistoryTitle.Name = "lblHistoryTitle";
            lblHistoryTitle.Size = new Size(176, 34);
            lblHistoryTitle.TabIndex = 0;
            lblHistoryTitle.Text = "알람 이력 조회";
            // 
            // lblToDate
            // 
            lblToDate.AutoSize = true;
            lblToDate.Location = new Point(265, 30);
            lblToDate.Name = "lblToDate";
            lblToDate.Size = new Size(51, 19);
            lblToDate.TabIndex = 1;
            lblToDate.Text = "종료일";
            // 
            // lblAlarmType
            // 
            lblAlarmType.AutoSize = true;
            lblAlarmType.Location = new Point(510, 30);
            lblAlarmType.Name = "lblAlarmType";
            lblAlarmType.Size = new Size(70, 19);
            lblAlarmType.TabIndex = 2;
            lblAlarmType.Text = "알람 종류";
            // 
            // dtpFromDate
            // 
            dtpFromDate.CustomFormat = "yyyy-MM-dd";
            dtpFromDate.Format = DateTimePickerFormat.Custom;
            dtpFromDate.Location = new Point(75, 26);
            dtpFromDate.Name = "dtpFromDate";
            dtpFromDate.Size = new Size(175, 25);
            dtpFromDate.TabIndex = 3;
            // 
            // dtpToDate
            // 
            dtpToDate.CustomFormat = "yyyy-MM-dd";
            dtpToDate.Format = DateTimePickerFormat.Custom;
            dtpToDate.Location = new Point(325, 26);
            dtpToDate.Name = "dtpToDate";
            dtpToDate.Size = new Size(175, 25);
            dtpToDate.TabIndex = 4;
            // 
            // cmbAlarmType
            // 
            cmbAlarmType.FormattingEnabled = true;
            cmbAlarmType.Items.AddRange(new object[] { "전체 ", "고온", "센서 오류 ", "통신 끊김" });
            cmbAlarmType.Location = new Point(580, 26);
            cmbAlarmType.Name = "cmbAlarmType";
            cmbAlarmType.Size = new Size(160, 25);
            cmbAlarmType.TabIndex = 5;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(1246, 19);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(125, 42);
            btnSearch.TabIndex = 6;
            btnSearch.Text = "조회";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(1275, 612);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(125, 35);
            btnClose.TabIndex = 7;
            btnClose.Text = "닫기";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // pnlSearch
            // 
            pnlSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlSearch.BackColor = Color.White;
            pnlSearch.Controls.Add(lblFromDate);
            pnlSearch.Controls.Add(lblToDate);
            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(lblAlarmType);
            pnlSearch.Controls.Add(cmbAlarmType);
            pnlSearch.Controls.Add(dtpFromDate);
            pnlSearch.Controls.Add(dtpToDate);
            pnlSearch.Location = new Point(20, 75);
            pnlSearch.Name = "pnlSearch";
            pnlSearch.Size = new Size(1385, 85);
            pnlSearch.TabIndex = 8;
            // 
            // lblFromDate
            // 
            lblFromDate.AutoSize = true;
            lblFromDate.Location = new Point(15, 30);
            lblFromDate.Name = "lblFromDate";
            lblFromDate.Size = new Size(51, 19);
            lblFromDate.TabIndex = 0;
            lblFromDate.Text = "시작일";
            // 
            // dgvAlarmHistory
            // 
            dgvAlarmHistory.AllowUserToAddRows = false;
            dgvAlarmHistory.AllowUserToDeleteRows = false;
            dgvAlarmHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvAlarmHistory.BackgroundColor = Color.White;
            dgvAlarmHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAlarmHistory.Columns.AddRange(new DataGridViewColumn[] { colHistoryAlarmId, colHistoryDeviceName, colHistoryCategory, colHistoryMessage, colHistoryTemperature, colHistoryOccurredAt, colHistoryAcknowledgedAt, colHistoryRecoveredAt, colHistoryKakaoStatus });
            dgvAlarmHistory.Location = new Point(20, 166);
            dgvAlarmHistory.MultiSelect = false;
            dgvAlarmHistory.Name = "dgvAlarmHistory";
            dgvAlarmHistory.ReadOnly = true;
            dgvAlarmHistory.RowHeadersVisible = false;
            dgvAlarmHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAlarmHistory.Size = new Size(1385, 440);
            dgvAlarmHistory.TabIndex = 9;
            // 
            // colHistoryAlarmId
            // 
            colHistoryAlarmId.HeaderText = "알람 번호";
            colHistoryAlarmId.Name = "colHistoryAlarmId";
            colHistoryAlarmId.ReadOnly = true;
            colHistoryAlarmId.Width = 80;
            // 
            // colHistoryDeviceName
            // 
            colHistoryDeviceName.HeaderText = "장비명";
            colHistoryDeviceName.Name = "colHistoryDeviceName";
            colHistoryDeviceName.ReadOnly = true;
            colHistoryDeviceName.Width = 150;
            // 
            // colHistoryCategory
            // 
            colHistoryCategory.HeaderText = "분류";
            colHistoryCategory.Name = "colHistoryCategory";
            colHistoryCategory.ReadOnly = true;
            // 
            // colHistoryMessage
            // 
            colHistoryMessage.HeaderText = "내용";
            colHistoryMessage.Name = "colHistoryMessage";
            colHistoryMessage.ReadOnly = true;
            colHistoryMessage.Width = 320;
            // 
            // colHistoryTemperature
            // 
            colHistoryTemperature.HeaderText = "발생 온도 (℃)";
            colHistoryTemperature.Name = "colHistoryTemperature";
            colHistoryTemperature.ReadOnly = true;
            colHistoryTemperature.Width = 125;
            // 
            // colHistoryOccurredAt
            // 
            colHistoryOccurredAt.HeaderText = "발생 시각";
            colHistoryOccurredAt.Name = "colHistoryOccurredAt";
            colHistoryOccurredAt.ReadOnly = true;
            colHistoryOccurredAt.Width = 165;
            // 
            // colHistoryAcknowledgedAt
            // 
            colHistoryAcknowledgedAt.HeaderText = "사용자 확인 시각";
            colHistoryAcknowledgedAt.Name = "colHistoryAcknowledgedAt";
            colHistoryAcknowledgedAt.ReadOnly = true;
            colHistoryAcknowledgedAt.Width = 165;
            // 
            // colHistoryRecoveredAt
            // 
            colHistoryRecoveredAt.HeaderText = "복귀 시각";
            colHistoryRecoveredAt.Name = "colHistoryRecoveredAt";
            colHistoryRecoveredAt.ReadOnly = true;
            colHistoryRecoveredAt.Width = 165;
            // 
            // colHistoryKakaoStatus
            // 
            colHistoryKakaoStatus.HeaderText = "카카오 발송";
            colHistoryKakaoStatus.Name = "colHistoryKakaoStatus";
            colHistoryKakaoStatus.ReadOnly = true;
            colHistoryKakaoStatus.Width = 110;
            // 
            // lblHistoryCount
            // 
            lblHistoryCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblHistoryCount.AutoSize = true;
            lblHistoryCount.BorderStyle = BorderStyle.Fixed3D;
            lblHistoryCount.FlatStyle = FlatStyle.Flat;
            lblHistoryCount.Font = new Font("맑은 고딕", 13F, FontStyle.Bold);
            lblHistoryCount.Location = new Point(20, 610);
            lblHistoryCount.Name = "lblHistoryCount";
            lblHistoryCount.Size = new Size(126, 27);
            lblHistoryCount.TabIndex = 10;
            lblHistoryCount.Text = "조회 결과 0건";
            // 
            // AlarmHistoryForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(240, 244, 249);
            ClientSize = new Size(1409, 661);
            Controls.Add(lblHistoryCount);
            Controls.Add(dgvAlarmHistory);
            Controls.Add(pnlSearch);
            Controls.Add(btnClose);
            Controls.Add(lblHistoryTitle);
            Font = new Font("맑은 고딕", 10F);
            MinimumSize = new Size(1200, 590);
            Name = "AlarmHistoryForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "알람 이력 조회";
            pnlSearch.ResumeLayout(false);
            pnlSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAlarmHistory).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblHistoryTitle;
        private Label lblToDate;
        private Label lblAlarmType;
        private DateTimePicker dtpFromDate;
        private DateTimePicker dtpToDate;
        private ComboBox cmbAlarmType;
        private Button btnSearch;
        private Button btnClose;
        private Panel pnlSearch;
        private Label lblFromDate;
        private DataGridView dgvAlarmHistory;
        private Label lblHistoryCount;
        private DataGridViewTextBoxColumn colHistoryAlarmId;
        private DataGridViewTextBoxColumn colHistoryDeviceName;
        private DataGridViewTextBoxColumn colHistoryCategory;
        private DataGridViewTextBoxColumn colHistoryMessage;
        private DataGridViewTextBoxColumn colHistoryTemperature;
        private DataGridViewTextBoxColumn colHistoryOccurredAt;
        private DataGridViewTextBoxColumn colHistoryAcknowledgedAt;
        private DataGridViewTextBoxColumn colHistoryRecoveredAt;
        private DataGridViewTextBoxColumn colHistoryKakaoStatus;
    }
}