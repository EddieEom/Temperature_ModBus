namespace Temperature_Modbus
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlStatus = new Panel();
            txtDevicePort = new TextBox();
            lblDevicePort = new Label();
            txtDeviceIp = new TextBox();
            lblDeviceIp = new Label();
            lblCurrentTime = new Label();
            lblDbStatus = new Label();
            lblDeviceStatus = new Label();
            lblTitle = new Label();
            pnlTemperature = new Panel();
            lblAlarmSummary = new Label();
            lblSampleRule = new Label();
            lblThreshold = new Label();
            lblTemperature = new Label();
            lblTemperatureState = new Label();
            lblSensorName = new Label();
            btnConnect = new Button();
            btnExit = new Button();
            pnlGraph = new Panel();
            plotTemperature = new ScottPlot.WinForms.FormsPlot();
            lblGraphTitle = new Label();
            pnlAlarms = new Panel();
            btnAlarmHistory = new Button();
            dgvActiveAlarms = new DataGridView();
            colOccurredAt = new DataGridViewTextBoxColumn();
            colCategory = new DataGridViewTextBoxColumn();
            colDescription = new DataGridViewTextBoxColumn();
            colAcknowledge = new DataGridViewButtonColumn();
            colKakaoStatus = new DataGridViewTextBoxColumn();
            lblActiveAlarmTitle = new Label();
            btnLicenses = new Button();
            pnlStatus.SuspendLayout();
            pnlTemperature.SuspendLayout();
            pnlGraph.SuspendLayout();
            pnlAlarms.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvActiveAlarms).BeginInit();
            SuspendLayout();
            // 
            // pnlStatus
            // 
            pnlStatus.Controls.Add(txtDevicePort);
            pnlStatus.Controls.Add(lblDevicePort);
            pnlStatus.Controls.Add(txtDeviceIp);
            pnlStatus.Controls.Add(lblDeviceIp);
            pnlStatus.Controls.Add(lblCurrentTime);
            pnlStatus.Controls.Add(lblDbStatus);
            pnlStatus.Controls.Add(lblDeviceStatus);
            pnlStatus.Location = new Point(24, 65);
            pnlStatus.Name = "pnlStatus";
            pnlStatus.Size = new Size(1232, 48);
            pnlStatus.TabIndex = 0;
            // 
            // txtDevicePort
            // 
            txtDevicePort.Location = new Point(800, 18);
            txtDevicePort.Name = "txtDevicePort";
            txtDevicePort.Size = new Size(98, 25);
            txtDevicePort.TabIndex = 7;
            // 
            // lblDevicePort
            // 
            lblDevicePort.AutoSize = true;
            lblDevicePort.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
            lblDevicePort.Location = new Point(742, 19);
            lblDevicePort.Name = "lblDevicePort";
            lblDevicePort.Size = new Size(52, 21);
            lblDevicePort.TabIndex = 6;
            lblDevicePort.Text = "Port :";
            // 
            // txtDeviceIp
            // 
            txtDeviceIp.Location = new Point(568, 19);
            txtDeviceIp.Name = "txtDeviceIp";
            txtDeviceIp.Size = new Size(160, 25);
            txtDeviceIp.TabIndex = 5;
            // 
            // lblDeviceIp
            // 
            lblDeviceIp.AutoSize = true;
            lblDeviceIp.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblDeviceIp.Location = new Point(489, 18);
            lblDeviceIp.Name = "lblDeviceIp";
            lblDeviceIp.Size = new Size(73, 21);
            lblDeviceIp.TabIndex = 4;
            lblDeviceIp.Text = "장비 IP :";
            // 
            // lblCurrentTime
            // 
            lblCurrentTime.AutoSize = true;
            lblCurrentTime.BackColor = Color.White;
            lblCurrentTime.BorderStyle = BorderStyle.FixedSingle;
            lblCurrentTime.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold);
            lblCurrentTime.Location = new Point(916, 0);
            lblCurrentTime.Name = "lblCurrentTime";
            lblCurrentTime.Padding = new Padding(10, 8, 10, 8);
            lblCurrentTime.Size = new Size(316, 43);
            lblCurrentTime.TabIndex = 3;
            lblCurrentTime.Text = "현재 시각: 0000-00-00 00:00:00";
            // 
            // lblDbStatus
            // 
            lblDbStatus.AutoSize = true;
            lblDbStatus.BackColor = Color.White;
            lblDbStatus.BorderStyle = BorderStyle.FixedSingle;
            lblDbStatus.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold);
            lblDbStatus.Location = new Point(261, 0);
            lblDbStatus.Name = "lblDbStatus";
            lblDbStatus.Padding = new Padding(10, 8, 10, 8);
            lblDbStatus.Size = new Size(129, 43);
            lblDbStatus.TabIndex = 1;
            lblDbStatus.Text = "DB: 미연결";
            // 
            // lblDeviceStatus
            // 
            lblDeviceStatus.AutoSize = true;
            lblDeviceStatus.BackColor = Color.White;
            lblDeviceStatus.BorderStyle = BorderStyle.FixedSingle;
            lblDeviceStatus.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold);
            lblDeviceStatus.Location = new Point(3, 0);
            lblDeviceStatus.Name = "lblDeviceStatus";
            lblDeviceStatus.Padding = new Padding(10, 8, 10, 8);
            lblDeviceStatus.Size = new Size(228, 43);
            lblDeviceStatus.TabIndex = 0;
            lblDeviceStatus.Text = "장비 통신: TCP 미연결";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.BorderStyle = BorderStyle.Fixed3D;
            lblTitle.Font = new Font("맑은 고딕", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTitle.Location = new Point(24, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(197, 32);
            lblTitle.TabIndex = 2;
            lblTitle.Text = "온도 센서 모니터링";
            // 
            // pnlTemperature
            // 
            pnlTemperature.BackColor = Color.White;
            pnlTemperature.BorderStyle = BorderStyle.FixedSingle;
            pnlTemperature.Controls.Add(lblAlarmSummary);
            pnlTemperature.Controls.Add(lblSampleRule);
            pnlTemperature.Controls.Add(lblThreshold);
            pnlTemperature.Controls.Add(lblTemperature);
            pnlTemperature.Controls.Add(lblTemperatureState);
            pnlTemperature.Controls.Add(lblSensorName);
            pnlTemperature.Location = new Point(24, 125);
            pnlTemperature.Name = "pnlTemperature";
            pnlTemperature.Size = new Size(390, 300);
            pnlTemperature.TabIndex = 3;
            // 
            // lblAlarmSummary
            // 
            lblAlarmSummary.AutoSize = true;
            lblAlarmSummary.Location = new Point(18, 220);
            lblAlarmSummary.Name = "lblAlarmSummary";
            lblAlarmSummary.Size = new Size(136, 19);
            lblAlarmSummary.TabIndex = 5;
            lblAlarmSummary.Text = "활성 고온 에러 없음";
            // 
            // lblSampleRule
            // 
            lblSampleRule.AutoSize = true;
            lblSampleRule.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lblSampleRule.Location = new Point(18, 175);
            lblSampleRule.Name = "lblSampleRule";
            lblSampleRule.Size = new Size(166, 15);
            lblSampleRule.TabIndex = 4;
            lblSampleRule.Text = "각각 정상 수신 3회 연속 확인";
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lblThreshold.Location = new Point(18, 150);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(226, 15);
            lblThreshold.TabIndex = 3;
            lblThreshold.Text = "고온 발생 ≥ 80.0℃ / 정상 복귀 ≤ 40.0℃";
            // 
            // lblTemperature
            // 
            lblTemperature.AutoSize = true;
            lblTemperature.Font = new Font("맑은 고딕", 36F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTemperature.Location = new Point(18, 65);
            lblTemperature.Name = "lblTemperature";
            lblTemperature.Size = new Size(140, 65);
            lblTemperature.TabIndex = 2;
            lblTemperature.Text = "— ℃";
            // 
            // lblTemperatureState
            // 
            lblTemperatureState.AutoSize = true;
            lblTemperatureState.BackColor = Color.FromArgb(235, 239, 244);
            lblTemperatureState.Font = new Font("맑은 고딕", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 129);
            lblTemperatureState.Location = new Point(310, 20);
            lblTemperatureState.Name = "lblTemperatureState";
            lblTemperatureState.Padding = new Padding(6, 3, 6, 3);
            lblTemperatureState.Size = new Size(46, 23);
            lblTemperatureState.TabIndex = 1;
            lblTemperatureState.Text = "대기";
            // 
            // lblSensorName
            // 
            lblSensorName.AutoSize = true;
            lblSensorName.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblSensorName.Location = new Point(18, 20);
            lblSensorName.Name = "lblSensorName";
            lblSensorName.Size = new Size(124, 20);
            lblSensorName.TabIndex = 0;
            lblSensorName.Text = "PT100 온도 센서";
            // 
            // btnConnect
            // 
            btnConnect.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnConnect.BackColor = Color.FromArgb(35, 90, 210);
            btnConnect.Cursor = Cursors.Hand;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.Font = new Font("맑은 고딕", 15.75F, FontStyle.Bold);
            btnConnect.ForeColor = Color.White;
            btnConnect.Location = new Point(24, 700);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(606, 76);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "통신 연결";
            btnConnect.UseVisualStyleBackColor = false;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnExit
            // 
            btnExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExit.BackColor = Color.FromArgb(70, 78, 90);
            btnExit.Cursor = Cursors.Hand;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("맑은 고딕", 15.75F, FontStyle.Bold);
            btnExit.ForeColor = Color.White;
            btnExit.Location = new Point(650, 700);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(606, 76);
            btnExit.TabIndex = 6;
            btnExit.Text = "종료";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += btnExit_Click;
            // 
            // pnlGraph
            // 
            pnlGraph.BackColor = Color.White;
            pnlGraph.BorderStyle = BorderStyle.FixedSingle;
            pnlGraph.Controls.Add(plotTemperature);
            pnlGraph.Controls.Add(lblGraphTitle);
            pnlGraph.Location = new Point(430, 125);
            pnlGraph.Name = "pnlGraph";
            pnlGraph.Size = new Size(826, 300);
            pnlGraph.TabIndex = 7;
            // 
            // plotTemperature
            // 
            plotTemperature.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            plotTemperature.Location = new Point(18, 43);
            plotTemperature.Name = "plotTemperature";
            plotTemperature.Size = new Size(785, 230);
            plotTemperature.TabIndex = 1;
            // 
            // lblGraphTitle
            // 
            lblGraphTitle.AutoSize = true;
            lblGraphTitle.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblGraphTitle.Location = new Point(18, 20);
            lblGraphTitle.Name = "lblGraphTitle";
            lblGraphTitle.Size = new Size(109, 20);
            lblGraphTitle.TabIndex = 0;
            lblGraphTitle.Text = "최근 온도 추이";
            // 
            // pnlAlarms
            // 
            pnlAlarms.BackColor = Color.White;
            pnlAlarms.Controls.Add(btnAlarmHistory);
            pnlAlarms.Controls.Add(dgvActiveAlarms);
            pnlAlarms.Controls.Add(lblActiveAlarmTitle);
            pnlAlarms.Location = new Point(24, 445);
            pnlAlarms.Name = "pnlAlarms";
            pnlAlarms.Size = new Size(1232, 230);
            pnlAlarms.TabIndex = 8;
            // 
            // btnAlarmHistory
            // 
            btnAlarmHistory.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAlarmHistory.Location = new Point(1060, 5);
            btnAlarmHistory.Name = "btnAlarmHistory";
            btnAlarmHistory.Size = new Size(150, 40);
            btnAlarmHistory.TabIndex = 2;
            btnAlarmHistory.Text = "알람 이력 조회";
            btnAlarmHistory.UseVisualStyleBackColor = true;
            btnAlarmHistory.Click += btnAlarmHistory_Click;
            // 
            // dgvActiveAlarms
            // 
            dgvActiveAlarms.AllowUserToAddRows = false;
            dgvActiveAlarms.AllowUserToDeleteRows = false;
            dgvActiveAlarms.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvActiveAlarms.BackgroundColor = Color.White;
            dgvActiveAlarms.BorderStyle = BorderStyle.None;
            dgvActiveAlarms.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvActiveAlarms.Columns.AddRange(new DataGridViewColumn[] { colOccurredAt, colCategory, colDescription, colAcknowledge, colKakaoStatus });
            dgvActiveAlarms.Location = new Point(18, 48);
            dgvActiveAlarms.MultiSelect = false;
            dgvActiveAlarms.Name = "dgvActiveAlarms";
            dgvActiveAlarms.ReadOnly = true;
            dgvActiveAlarms.RowHeadersVisible = false;
            dgvActiveAlarms.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvActiveAlarms.Size = new Size(1192, 160);
            dgvActiveAlarms.TabIndex = 1;
            dgvActiveAlarms.CellContentClick += dgvActiveAlarms_CellContentClick;
            // 
            // colOccurredAt
            // 
            colOccurredAt.FillWeight = 20F;
            colOccurredAt.HeaderText = "발생 시각";
            colOccurredAt.Name = "colOccurredAt";
            colOccurredAt.ReadOnly = true;
            // 
            // colCategory
            // 
            colCategory.FillWeight = 15F;
            colCategory.HeaderText = "분류";
            colCategory.Name = "colCategory";
            colCategory.ReadOnly = true;
            // 
            // colDescription
            // 
            colDescription.FillWeight = 35F;
            colDescription.HeaderText = "내용";
            colDescription.Name = "colDescription";
            colDescription.ReadOnly = true;
            // 
            // colAcknowledge
            // 
            colAcknowledge.FillWeight = 10F;
            colAcknowledge.HeaderText = "사용자 확인";
            colAcknowledge.Name = "colAcknowledge";
            colAcknowledge.ReadOnly = true;
            colAcknowledge.Resizable = DataGridViewTriState.True;
            colAcknowledge.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // colKakaoStatus
            // 
            colKakaoStatus.FillWeight = 20F;
            colKakaoStatus.HeaderText = "카카오 메시지";
            colKakaoStatus.Name = "colKakaoStatus";
            colKakaoStatus.ReadOnly = true;
            // 
            // lblActiveAlarmTitle
            // 
            lblActiveAlarmTitle.AutoSize = true;
            lblActiveAlarmTitle.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblActiveAlarmTitle.Location = new Point(18, 14);
            lblActiveAlarmTitle.Name = "lblActiveAlarmTitle";
            lblActiveAlarmTitle.Size = new Size(74, 20);
            lblActiveAlarmTitle.TabIndex = 0;
            lblActiveAlarmTitle.Text = "활성 알람";
            // 
            // btnLicenses
            // 
            btnLicenses.Location = new Point(1173, 20);
            btnLicenses.Name = "btnLicenses";
            btnLicenses.Size = new Size(79, 32);
            btnLicenses.TabIndex = 9;
            btnLicenses.Text = "License";
            btnLicenses.UseVisualStyleBackColor = true;
            btnLicenses.Click += btnLicenses_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(240, 244, 249);
            ClientSize = new Size(1264, 781);
            Controls.Add(btnLicenses);
            Controls.Add(pnlAlarms);
            Controls.Add(pnlGraph);
            Controls.Add(btnExit);
            Controls.Add(btnConnect);
            Controls.Add(pnlTemperature);
            Controls.Add(lblTitle);
            Controls.Add(pnlStatus);
            Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SIG-5560 온도 모니터링";
            pnlStatus.ResumeLayout(false);
            pnlStatus.PerformLayout();
            pnlTemperature.ResumeLayout(false);
            pnlTemperature.PerformLayout();
            pnlGraph.ResumeLayout(false);
            pnlGraph.PerformLayout();
            pnlAlarms.ResumeLayout(false);
            pnlAlarms.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvActiveAlarms).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlStatus;
        private Label lblTitle;
        private Label lblCurrentTime;
        private Label lblDbStatus;
        private Label lblDeviceStatus;
        private Panel pnlTemperature;
        private Label lblAlarmSummary;
        private Label lblSampleRule;
        private Label lblThreshold;
        private Label lblTemperature;
        private Label lblTemperatureState;
        private Label lblSensorName;
        private Button btnConnect;
        private Button btnExit;
        private Panel pnlGraph;
        private Label lblGraphTitle;
        private Panel pnlAlarms;
        private DataGridView dgvActiveAlarms;
        private Label lblActiveAlarmTitle;
        private ScottPlot.WinForms.FormsPlot plotTemperature;
        private TextBox txtDeviceIp;
        private Label lblDeviceIp;
        private Label lblDevicePort;
        private TextBox txtDevicePort;
        private DataGridViewTextBoxColumn colOccurredAt;
        private DataGridViewTextBoxColumn colCategory;
        private DataGridViewTextBoxColumn colDescription;
        private DataGridViewButtonColumn colAcknowledge;
        private DataGridViewTextBoxColumn colKakaoStatus;
        private Button btnAlarmHistory;
        private Button btnLicenses;
    }
}
