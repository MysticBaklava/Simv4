namespace ModbusSimV1
{
    partial class Form1
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
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.flowRegisters = new System.Windows.Forms.FlowLayoutPanel();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpConnection = new System.Windows.Forms.GroupBox();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnRefreshPorts = new System.Windows.Forms.Button();
            this.cmbPort = new System.Windows.Forms.ComboBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRead = new System.Windows.Forms.Button();
            this.btnWrite = new System.Windows.Forms.Button();
            this.grpSelectedRegister = new System.Windows.Forms.GroupBox();
            this.selectedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSelectedRegisterName = new System.Windows.Forms.Label();
            this.txtSelectedRegisterInfo = new System.Windows.Forms.TextBox();
            this.grpAutomation = new System.Windows.Forms.GroupBox();
            this.automationLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chkAutomationEnabled = new System.Windows.Forms.CheckBox();
            this.lblEvent = new System.Windows.Forms.Label();
            this.cmbEvent = new System.Windows.Forms.ComboBox();
            this.lblActivity = new System.Windows.Forms.Label();
            this.lstActivity = new System.Windows.Forms.ListBox();
            this.lblRxTx = new System.Windows.Forms.Label();
            this.lstRxTx = new System.Windows.Forms.ListBox();
            this.layoutRoot.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpConnection.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.grpSelectedRegister.SuspendLayout();
            this.selectedLayout.SuspendLayout();
            this.grpAutomation.SuspendLayout();
            this.automationLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 2;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 64F));
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.layoutRoot.Controls.Add(this.leftLayout, 0, 0);
            this.layoutRoot.Controls.Add(this.rightLayout, 1, 0);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(0, 0);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 1;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Size = new System.Drawing.Size(1100, 843);
            this.layoutRoot.TabIndex = 0;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.headerPanel, 0, 0);
            this.leftLayout.Controls.Add(this.flowRegisters, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(3, 3);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(698, 837);
            this.leftLayout.TabIndex = 0;
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(56)))));
            this.headerPanel.Controls.Add(this.label1);
            this.headerPanel.Controls.Add(this.lblTitle);
            this.headerPanel.Location = new System.Drawing.Point(3, 3);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(16, 20, 16, 16);
            this.headerPanel.Size = new System.Drawing.Size(692, 40);
            this.headerPanel.TabIndex = 0;
            this.headerPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.headerPanel_Paint);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            this.label1.Location = new System.Drawing.Point(491, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(192, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Baudrate: 9600, 8E1, SlaveAddr=1";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(6, 3);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(226, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Vector Controller Modbus Simulator";
            // 
            // flowRegisters
            // 
            this.flowRegisters.AutoScroll = true;
            this.flowRegisters.BackColor = System.Drawing.Color.White;
            this.flowRegisters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowRegisters.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowRegisters.Font = new System.Drawing.Font("Open Sans", 9.75F);
            this.flowRegisters.Location = new System.Drawing.Point(3, 49);
            this.flowRegisters.Name = "flowRegisters";
            this.flowRegisters.Padding = new System.Windows.Forms.Padding(12, 8, 20, 12);
            this.flowRegisters.Size = new System.Drawing.Size(692, 785);
            this.flowRegisters.TabIndex = 1;
            this.flowRegisters.WrapContents = false;
            this.flowRegisters.Paint += new System.Windows.Forms.PaintEventHandler(this.flowRegisters_Paint);
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpConnection, 0, 0);
            this.rightLayout.Controls.Add(this.buttonPanel, 0, 1);
            this.rightLayout.Controls.Add(this.grpSelectedRegister, 0, 2);
            this.rightLayout.Controls.Add(this.grpAutomation, 0, 3);
            this.rightLayout.Controls.Add(this.lblActivity, 0, 4);
            this.rightLayout.Controls.Add(this.lstActivity, 0, 5);
            this.rightLayout.Controls.Add(this.lblRxTx, 0, 6);
            this.rightLayout.Controls.Add(this.lstRxTx, 0, 7);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(707, 3);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 8;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.rightLayout.Size = new System.Drawing.Size(390, 837);
            this.rightLayout.TabIndex = 1;
            // 
            // grpConnection
            // 
            this.grpConnection.Controls.Add(this.lblConnectionStatus);
            this.grpConnection.Controls.Add(this.btnDisconnect);
            this.grpConnection.Controls.Add(this.btnConnect);
            this.grpConnection.Controls.Add(this.btnRefreshPorts);
            this.grpConnection.Controls.Add(this.cmbPort);
            this.grpConnection.Controls.Add(this.lblPort);
            this.grpConnection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConnection.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpConnection.Location = new System.Drawing.Point(3, 3);
            this.grpConnection.Name = "grpConnection";
            this.grpConnection.Padding = new System.Windows.Forms.Padding(12, 16, 12, 12);
            this.grpConnection.Size = new System.Drawing.Size(384, 152);
            this.grpConnection.TabIndex = 0;
            this.grpConnection.TabStop = false;
            this.grpConnection.Text = "Connection";
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblConnectionStatus.Location = new System.Drawing.Point(15, 116);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(140, 17);
            this.lblConnectionStatus.TabIndex = 5;
            this.lblConnectionStatus.Text = "Status: Disconnected";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnDisconnect.Location = new System.Drawing.Point(199, 74);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(160, 34);
            this.btnDisconnect.TabIndex = 4;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnConnect.Location = new System.Drawing.Point(15, 74);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(160, 34);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnRefreshPorts
            // 
            this.btnRefreshPorts.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnRefreshPorts.Location = new System.Drawing.Point(292, 34);
            this.btnRefreshPorts.Name = "btnRefreshPorts";
            this.btnRefreshPorts.Size = new System.Drawing.Size(67, 28);
            this.btnRefreshPorts.TabIndex = 2;
            this.btnRefreshPorts.Text = "Refresh";
            this.btnRefreshPorts.UseVisualStyleBackColor = true;
            this.btnRefreshPorts.Click += new System.EventHandler(this.btnRefreshPorts_Click);
            // 
            // cmbPort
            // 
            this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPort.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cmbPort.FormattingEnabled = true;
            this.cmbPort.Location = new System.Drawing.Point(65, 34);
            this.cmbPort.Name = "cmbPort";
            this.cmbPort.Size = new System.Drawing.Size(221, 25);
            this.cmbPort.TabIndex = 1;
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblPort.Location = new System.Drawing.Point(15, 37);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(42, 17);
            this.lblPort.TabIndex = 0;
            this.lblPort.Text = "PORT:";
            // 
            // buttonPanel
            // 
            this.buttonPanel.AutoSize = true;
            this.buttonPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonPanel.Controls.Add(this.btnRead);
            this.buttonPanel.Controls.Add(this.btnWrite);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.buttonPanel.Location = new System.Drawing.Point(3, 161);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(384, 153);
            this.buttonPanel.TabIndex = 1;
            this.buttonPanel.WrapContents = false;
            // 
            // btnRead
            //
            this.btnRead.Enabled = false;
            this.btnRead.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnRead.Location = new System.Drawing.Point(3, 3);
            this.btnRead.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(225, 40);
            this.btnRead.TabIndex = 1;
            this.btnRead.Text = "READ ONCE";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            //
            // btnWrite
            //
            this.btnWrite.Enabled = false;
            this.btnWrite.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnWrite.Location = new System.Drawing.Point(3, 53);
            this.btnWrite.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.btnWrite.Name = "btnWrite";
            this.btnWrite.Size = new System.Drawing.Size(225, 40);
            this.btnWrite.TabIndex = 2;
            this.btnWrite.Text = "WRITE ONCE";
            this.btnWrite.UseVisualStyleBackColor = true;
            this.btnWrite.Click += new System.EventHandler(this.btnWrite_Click);
            // 
            // grpSelectedRegister
            // 
            this.grpSelectedRegister.Controls.Add(this.selectedLayout);
            this.grpSelectedRegister.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSelectedRegister.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpSelectedRegister.Location = new System.Drawing.Point(3, 320);
            this.grpSelectedRegister.Name = "grpSelectedRegister";
            this.grpSelectedRegister.Padding = new System.Windows.Forms.Padding(12, 16, 12, 12);
            this.grpSelectedRegister.Size = new System.Drawing.Size(384, 162);
            this.grpSelectedRegister.TabIndex = 2;
            this.grpSelectedRegister.TabStop = false;
            this.grpSelectedRegister.Text = "Selected Register";
            // 
            // selectedLayout
            // 
            this.selectedLayout.ColumnCount = 1;
            this.selectedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.selectedLayout.Controls.Add(this.lblSelectedRegisterName, 0, 0);
            this.selectedLayout.Controls.Add(this.txtSelectedRegisterInfo, 0, 1);
            this.selectedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectedLayout.Location = new System.Drawing.Point(12, 34);
            this.selectedLayout.Name = "selectedLayout";
            this.selectedLayout.RowCount = 2;
            this.selectedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.selectedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.selectedLayout.Size = new System.Drawing.Size(360, 116);
            this.selectedLayout.TabIndex = 0;
            // 
            // lblSelectedRegisterName
            // 
            this.lblSelectedRegisterName.AutoSize = true;
            this.lblSelectedRegisterName.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblSelectedRegisterName.Location = new System.Drawing.Point(3, 0);
            this.lblSelectedRegisterName.Name = "lblSelectedRegisterName";
            this.lblSelectedRegisterName.Size = new System.Drawing.Size(160, 20);
            this.lblSelectedRegisterName.TabIndex = 0;
            this.lblSelectedRegisterName.Text = "No register selected";
            // 
            // txtSelectedRegisterInfo
            // 
            this.txtSelectedRegisterInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtSelectedRegisterInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSelectedRegisterInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSelectedRegisterInfo.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtSelectedRegisterInfo.Location = new System.Drawing.Point(3, 23);
            this.txtSelectedRegisterInfo.Multiline = true;
            this.txtSelectedRegisterInfo.Name = "txtSelectedRegisterInfo";
            this.txtSelectedRegisterInfo.ReadOnly = true;
            this.txtSelectedRegisterInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSelectedRegisterInfo.Size = new System.Drawing.Size(354, 90);
            this.txtSelectedRegisterInfo.TabIndex = 1;
            // 
            // grpAutomation
            // 
            this.grpAutomation.Controls.Add(this.automationLayout);
            this.grpAutomation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAutomation.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.grpAutomation.Location = new System.Drawing.Point(3, 488);
            this.grpAutomation.Name = "grpAutomation";
            this.grpAutomation.Padding = new System.Windows.Forms.Padding(12, 16, 12, 12);
            this.grpAutomation.Size = new System.Drawing.Size(384, 159);
            this.grpAutomation.TabIndex = 3;
            this.grpAutomation.TabStop = false;
            this.grpAutomation.Text = "Automation";
            // 
            // automationLayout
            // 
            this.automationLayout.ColumnCount = 2;
            this.automationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.automationLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.automationLayout.Controls.Add(this.chkAutomationEnabled, 0, 0);
            this.automationLayout.SetColumnSpan(this.chkAutomationEnabled, 2);
            this.automationLayout.Controls.Add(this.lblEvent, 0, 1);
            this.automationLayout.Controls.Add(this.cmbEvent, 1, 1);
            this.automationLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.automationLayout.Location = new System.Drawing.Point(12, 34);
            this.automationLayout.Name = "automationLayout";
            this.automationLayout.RowCount = 2;
            this.automationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.automationLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.automationLayout.Size = new System.Drawing.Size(360, 113);
            this.automationLayout.TabIndex = 0;
            // 
            // chkAutomationEnabled
            // 
            this.chkAutomationEnabled.AutoSize = true;
            this.chkAutomationEnabled.Checked = true;
            this.chkAutomationEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutomationEnabled.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.chkAutomationEnabled.Location = new System.Drawing.Point(3, 3);
            this.chkAutomationEnabled.Name = "chkAutomationEnabled";
            this.chkAutomationEnabled.Size = new System.Drawing.Size(122, 21);
            this.chkAutomationEnabled.TabIndex = 0;
            this.chkAutomationEnabled.Text = "Run automation";
            this.chkAutomationEnabled.UseVisualStyleBackColor = true;
            this.chkAutomationEnabled.CheckedChanged += new System.EventHandler(this.chkAutomationEnabled_CheckedChanged);
            // 
            // lblEvent
            // 
            this.lblEvent.AutoSize = true;
            this.lblEvent.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblEvent.Location = new System.Drawing.Point(3, 27);
            this.lblEvent.Name = "lblEvent";
            this.lblEvent.Size = new System.Drawing.Size(41, 17);
            this.lblEvent.TabIndex = 1;
            this.lblEvent.Text = "Event";
            // 
            // cmbEvent
            // 
            this.cmbEvent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbEvent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEvent.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cmbEvent.FormattingEnabled = true;
            this.cmbEvent.Location = new System.Drawing.Point(165, 30);
            this.cmbEvent.Name = "cmbEvent";
            this.cmbEvent.Size = new System.Drawing.Size(192, 25);
            this.cmbEvent.TabIndex = 2;
            this.cmbEvent.SelectedIndexChanged += new System.EventHandler(this.CmbMachineEvent_SelectedIndexChanged);
            // 
            // lblActivity
            // 
            this.lblActivity.AutoSize = true;
            this.lblActivity.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblActivity.Location = new System.Drawing.Point(3, 653);
            this.lblActivity.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.lblActivity.Name = "lblActivity";
            this.lblActivity.Size = new System.Drawing.Size(94, 19);
            this.lblActivity.TabIndex = 4;
            this.lblActivity.Text = "Activity Log";
            //
            // lstActivity
            //
            this.lstActivity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstActivity.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lstActivity.FormattingEnabled = true;
            this.lstActivity.IntegralHeight = false;
            this.lstActivity.ItemHeight = 17;
            this.lstActivity.Location = new System.Drawing.Point(3, 675);
            this.lstActivity.Name = "lstActivity";
            this.lstActivity.Size = new System.Drawing.Size(384, 83);
            this.lstActivity.TabIndex = 5;
            this.lstActivity.SelectedIndexChanged += new System.EventHandler(this.lstActivity_SelectedIndexChanged);
            //
            // lblRxTx
            //
            this.lblRxTx.AutoSize = true;
            this.lblRxTx.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblRxTx.Location = new System.Drawing.Point(3, 761);
            this.lblRxTx.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.lblRxTx.Name = "lblRxTx";
            this.lblRxTx.Size = new System.Drawing.Size(99, 19);
            this.lblRxTx.TabIndex = 6;
            this.lblRxTx.Text = "RX/TX Log (hex)";
            //
            // lstRxTx
            //
            this.lstRxTx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstRxTx.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.lstRxTx.FormattingEnabled = true;
            this.lstRxTx.IntegralHeight = false;
            this.lstRxTx.HorizontalScrollbar = true;
            this.lstRxTx.ItemHeight = 15;
            this.lstRxTx.Location = new System.Drawing.Point(3, 783);
            this.lstRxTx.Name = "lstRxTx";
            this.lstRxTx.Size = new System.Drawing.Size(384, 51);
            this.lstRxTx.TabIndex = 7;
            this.lstRxTx.SelectedIndexChanged += new System.EventHandler(this.lstRxTx_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(244)))));
            this.ClientSize = new System.Drawing.Size(1100, 843);
            this.Controls.Add(this.layoutRoot);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Vector Controller Modbus Simulator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.layoutRoot.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.rightLayout.ResumeLayout(false);
            this.rightLayout.PerformLayout();
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.grpSelectedRegister.ResumeLayout(false);
            this.selectedLayout.ResumeLayout(false);
            this.selectedLayout.PerformLayout();
            this.grpAutomation.ResumeLayout(false);
            this.automationLayout.ResumeLayout(false);
            this.automationLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowRegisters;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnRefreshPorts;
        private System.Windows.Forms.ComboBox cmbPort;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.Button btnWrite;
        private System.Windows.Forms.Label lblSelectedRegisterName;
        private System.Windows.Forms.TextBox txtSelectedRegisterInfo;
        private System.Windows.Forms.GroupBox grpAutomation;
        private System.Windows.Forms.CheckBox chkAutomationEnabled;
        private System.Windows.Forms.Label lblActivity;
        private System.Windows.Forms.ListBox lstActivity;
        private System.Windows.Forms.Label lblRxTx;
        private System.Windows.Forms.ListBox lstRxTx;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.GroupBox grpSelectedRegister;
        private System.Windows.Forms.TableLayoutPanel selectedLayout;
        private System.Windows.Forms.TableLayoutPanel automationLayout;
        private System.Windows.Forms.Label lblEvent;
        private System.Windows.Forms.ComboBox cmbEvent;
        private System.Windows.Forms.Label label1;
    }
}
