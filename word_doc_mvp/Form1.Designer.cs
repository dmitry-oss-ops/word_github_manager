namespace word_doc_mvp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.grpGitHub = new System.Windows.Forms.GroupBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.txtToken = new System.Windows.Forms.TextBox();
            this.lblToken = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtRepo = new System.Windows.Forms.TextBox();
            this.lblRepo = new System.Windows.Forms.Label();
            this.grpDocument = new System.Windows.Forms.GroupBox();
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.lblSelectedFile = new System.Windows.Forms.Label();
            this.txtBranchName = new System.Windows.Forms.TextBox();
            this.lblBranch = new System.Windows.Forms.Label();
            this.txtCommitMessage = new System.Windows.Forms.TextBox();
            this.lblCommitMsg = new System.Windows.Forms.Label();
            this.btnProcess = new System.Windows.Forms.Button();
            this.grpDownload = new System.Windows.Forms.GroupBox();
            this.lblBranchSelect = new System.Windows.Forms.Label();
            this.cmbBranches = new System.Windows.Forms.ComboBox();
            this.btnRefreshBranches = new System.Windows.Forms.Button();
            this.btnDownloadDocx = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.lblLog = new System.Windows.Forms.Label();
            this.linkPR = new System.Windows.Forms.LinkLabel();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.grpGitHub.SuspendLayout();
            this.grpDocument.SuspendLayout();
            this.grpDownload.SuspendLayout();
            this.SuspendLayout();

            // grpGitHub
            this.grpGitHub.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGitHub.Controls.Add(this.btnTestConnection);
            this.grpGitHub.Controls.Add(this.txtToken);
            this.grpGitHub.Controls.Add(this.lblToken);
            this.grpGitHub.Controls.Add(this.txtUsername);
            this.grpGitHub.Controls.Add(this.lblUsername);
            this.grpGitHub.Controls.Add(this.txtRepo);
            this.grpGitHub.Controls.Add(this.lblRepo);
            this.grpGitHub.Location = new System.Drawing.Point(12, 12);
            this.grpGitHub.Name = "grpGitHub";
            this.grpGitHub.Size = new System.Drawing.Size(760, 110);
            this.grpGitHub.TabIndex = 0;
            this.grpGitHub.TabStop = false;
            this.grpGitHub.Text = "GitHub Settings";

            // lblRepo
            this.lblRepo.AutoSize = true;
            this.lblRepo.Location = new System.Drawing.Point(10, 25);
            this.lblRepo.Name = "lblRepo";
            this.lblRepo.Size = new System.Drawing.Size(60, 13);
            this.lblRepo.Text = "Repository:";

            // txtRepo
            this.txtRepo.Location = new System.Drawing.Point(100, 22);
            this.txtRepo.Name = "txtRepo";
            this.txtRepo.Size = new System.Drawing.Size(200, 20);

            // lblUsername
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(10, 55);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(60, 13);
            this.lblUsername.Text = "Username:";

            // txtUsername
            this.txtUsername.Location = new System.Drawing.Point(100, 52);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(200, 20);

            // lblToken
            this.lblToken.AutoSize = true;
            this.lblToken.Location = new System.Drawing.Point(320, 25);
            this.lblToken.Name = "lblToken";
            this.lblToken.Size = new System.Drawing.Size(40, 13);
            this.lblToken.Text = "Token:";

            // txtToken
            this.txtToken.Location = new System.Drawing.Point(380, 22);
            this.txtToken.Name = "txtToken";
            this.txtToken.Size = new System.Drawing.Size(260, 20);
            this.txtToken.UseSystemPasswordChar = true;

            // btnTestConnection
            this.btnTestConnection.Location = new System.Drawing.Point(380, 50);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(130, 26);
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);

            // grpDocument
            this.grpDocument.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpDocument.Controls.Add(this.btnSelectFile);
            this.grpDocument.Controls.Add(this.lblSelectedFile);
            this.grpDocument.Controls.Add(this.txtBranchName);
            this.grpDocument.Controls.Add(this.lblBranch);
            this.grpDocument.Controls.Add(this.txtCommitMessage);
            this.grpDocument.Controls.Add(this.lblCommitMsg);
            this.grpDocument.Controls.Add(this.btnProcess);
            this.grpDocument.Location = new System.Drawing.Point(12, 130);
            this.grpDocument.Name = "grpDocument";
            this.grpDocument.Size = new System.Drawing.Size(760, 130);
            this.grpDocument.TabIndex = 1;
            this.grpDocument.TabStop = false;
            this.grpDocument.Text = "Normalize and Push";

            // btnSelectFile
            this.btnSelectFile.Location = new System.Drawing.Point(10, 22);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(120, 26);
            this.btnSelectFile.Text = "Select DOCX File...";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);

            // lblSelectedFile
            this.lblSelectedFile.AutoSize = true;
            this.lblSelectedFile.ForeColor = System.Drawing.Color.DimGray;
            this.lblSelectedFile.Location = new System.Drawing.Point(140, 28);
            this.lblSelectedFile.Name = "lblSelectedFile";
            this.lblSelectedFile.Size = new System.Drawing.Size(100, 13);
            this.lblSelectedFile.Text = "No file selected";

            // lblBranch
            this.lblBranch.AutoSize = true;
            this.lblBranch.Location = new System.Drawing.Point(10, 60);
            this.lblBranch.Name = "lblBranch";
            this.lblBranch.Size = new System.Drawing.Size(75, 13);
            this.lblBranch.Text = "Branch Name:";

            // txtBranchName
            this.txtBranchName.Location = new System.Drawing.Point(100, 57);
            this.txtBranchName.Name = "txtBranchName";
            this.txtBranchName.Size = new System.Drawing.Size(300, 20);

            // lblCommitMsg
            this.lblCommitMsg.AutoSize = true;
            this.lblCommitMsg.Location = new System.Drawing.Point(10, 90);
            this.lblCommitMsg.Name = "lblCommitMsg";
            this.lblCommitMsg.Size = new System.Drawing.Size(90, 13);
            this.lblCommitMsg.Text = "Commit Message:";

            // txtCommitMessage
            this.txtCommitMessage.Location = new System.Drawing.Point(100, 87);
            this.txtCommitMessage.Name = "txtCommitMessage";
            this.txtCommitMessage.Size = new System.Drawing.Size(300, 20);
            this.txtCommitMessage.Text = "Update document";

            // btnProcess
            this.btnProcess.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnProcess.Location = new System.Drawing.Point(520, 55);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(220, 50);
            this.btnProcess.Text = "Normalize && Create PR";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);

            // grpDownload
            this.grpDownload.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grpDownload.Controls.Add(this.lblBranchSelect);
            this.grpDownload.Controls.Add(this.cmbBranches);
            this.grpDownload.Controls.Add(this.btnRefreshBranches);
            this.grpDownload.Controls.Add(this.btnDownloadDocx);
            this.grpDownload.Location = new System.Drawing.Point(12, 268);
            this.grpDownload.Name = "grpDownload";
            this.grpDownload.Size = new System.Drawing.Size(760, 80);
            this.grpDownload.TabIndex = 2;
            this.grpDownload.TabStop = false;
            this.grpDownload.Text = "Download and Reconstruct DOCX";

            // lblBranchSelect
            this.lblBranchSelect.AutoSize = true;
            this.lblBranchSelect.Location = new System.Drawing.Point(10, 30);
            this.lblBranchSelect.Name = "lblBranchSelect";
            this.lblBranchSelect.Size = new System.Drawing.Size(44, 13);
            this.lblBranchSelect.Text = "Branch:";

            // cmbBranches
            this.cmbBranches.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBranches.Location = new System.Drawing.Point(100, 27);
            this.cmbBranches.Name = "cmbBranches";
            this.cmbBranches.Size = new System.Drawing.Size(250, 21);

            // btnRefreshBranches
            this.btnRefreshBranches.Location = new System.Drawing.Point(360, 25);
            this.btnRefreshBranches.Name = "btnRefreshBranches";
            this.btnRefreshBranches.Size = new System.Drawing.Size(80, 26);
            this.btnRefreshBranches.Text = "Refresh";
            this.btnRefreshBranches.UseVisualStyleBackColor = true;
            this.btnRefreshBranches.Click += new System.EventHandler(this.btnRefreshBranches_Click);

            // btnDownloadDocx
            this.btnDownloadDocx.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnDownloadDocx.Location = new System.Drawing.Point(520, 18);
            this.btnDownloadDocx.Name = "btnDownloadDocx";
            this.btnDownloadDocx.Size = new System.Drawing.Size(220, 40);
            this.btnDownloadDocx.Text = "Download && Save as DOCX";
            this.btnDownloadDocx.UseVisualStyleBackColor = true;
            this.btnDownloadDocx.Click += new System.EventHandler(this.btnDownloadDocx_Click);

            // lblLog
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(12, 356);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(25, 13);
            this.lblLog.Text = "Log:";

            // rtbLog
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.rtbLog.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this.rtbLog.Location = new System.Drawing.Point(12, 373);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(760, 190);
            this.rtbLog.TabIndex = 3;
            this.rtbLog.Text = "";

            // linkPR
            this.linkPR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left)));
            this.linkPR.AutoSize = true;
            this.linkPR.Location = new System.Drawing.Point(12, 570);
            this.linkPR.Name = "linkPR";
            this.linkPR.Size = new System.Drawing.Size(0, 13);
            this.linkPR.TabIndex = 4;
            this.linkPR.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkPR_LinkClicked);

            // openFileDialog
            this.openFileDialog.Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*";
            this.openFileDialog.Title = "Select a DOCX file";

            // saveFileDialog
            this.saveFileDialog.Filter = "Word Documents (*.docx)|*.docx";
            this.saveFileDialog.Title = "Save reconstructed DOCX";
            this.saveFileDialog.FileName = "document.docx";

            // Form1
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 598);
            this.Controls.Add(this.grpGitHub);
            this.Controls.Add(this.grpDocument);
            this.Controls.Add(this.grpDownload);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.linkPR);
            this.MinimumSize = new System.Drawing.Size(600, 580);
            this.Name = "Form1";
            this.Text = "DOCX Normalizer - Version Control Pipeline";
            this.grpGitHub.ResumeLayout(false);
            this.grpGitHub.PerformLayout();
            this.grpDocument.ResumeLayout(false);
            this.grpDocument.PerformLayout();
            this.grpDownload.ResumeLayout(false);
            this.grpDownload.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox grpGitHub;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.TextBox txtToken;
        private System.Windows.Forms.Label lblToken;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtRepo;
        private System.Windows.Forms.Label lblRepo;
        private System.Windows.Forms.GroupBox grpDocument;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Label lblSelectedFile;
        private System.Windows.Forms.TextBox txtBranchName;
        private System.Windows.Forms.Label lblBranch;
        private System.Windows.Forms.TextBox txtCommitMessage;
        private System.Windows.Forms.Label lblCommitMsg;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.GroupBox grpDownload;
        private System.Windows.Forms.Label lblBranchSelect;
        private System.Windows.Forms.ComboBox cmbBranches;
        private System.Windows.Forms.Button btnRefreshBranches;
        private System.Windows.Forms.Button btnDownloadDocx;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.LinkLabel linkPR;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
    }
}
