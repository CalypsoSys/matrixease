
namespace MatrixEase.Tester
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
            _testsLst = new System.Windows.Forms.ListView();
            columnHeader1 = new System.Windows.Forms.ColumnHeader();
            columnHeader2 = new System.Windows.Forms.ColumnHeader();
            columnHeader3 = new System.Windows.Forms.ColumnHeader();
            columnHeader4 = new System.Windows.Forms.ColumnHeader();
            label1 = new System.Windows.Forms.Label();
            _typeCmb = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            _sepTxt = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            _spec1Txt = new System.Windows.Forms.TextBox();
            _spec2Txt = new System.Windows.Forms.TextBox();
            _browseBtn = new System.Windows.Forms.Button();
            _openFileDialog = new System.Windows.Forms.OpenFileDialog();
            _addBtn = new System.Windows.Forms.Button();
            _saveBtn = new System.Windows.Forms.Button();
            _runBtn = new System.Windows.Forms.Button();
            _baseLineChk = new System.Windows.Forms.CheckBox();
            _canelBtn = new System.Windows.Forms.Button();
            _tabCtrl = new System.Windows.Forms.TabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            tabPage2 = new System.Windows.Forms.TabPage();
            _outputTxt = new System.Windows.Forms.RichTextBox();
            label5 = new System.Windows.Forms.Label();
            _outputPathTxt = new System.Windows.Forms.TextBox();
            _browseOutputBtn = new System.Windows.Forms.Button();
            _folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            _toggleChk = new System.Windows.Forms.CheckBox();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            label6 = new System.Windows.Forms.Label();
            _maxRowsTxt = new System.Windows.Forms.TextBox();
            _testBtn = new System.Windows.Forms.Button();
            _tabCtrl.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            _statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _testsLst
            // 
            _testsLst.CheckBoxes = true;
            _testsLst.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            _testsLst.Dock = System.Windows.Forms.DockStyle.Fill;
            _testsLst.Location = new System.Drawing.Point(4, 4);
            _testsLst.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _testsLst.Name = "_testsLst";
            _testsLst.Size = new System.Drawing.Size(1202, 589);
            _testsLst.TabIndex = 0;
            _testsLst.UseCompatibleStateImageBehavior = false;
            _testsLst.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Name = "columnHeader1";
            columnHeader1.Text = "Type";
            columnHeader1.Width = 100;
            // 
            // columnHeader2
            // 
            columnHeader2.Name = "columnHeader2";
            columnHeader2.Text = "Sep";
            // 
            // columnHeader3
            // 
            columnHeader3.Name = "columnHeader3";
            columnHeader3.Text = "Spec1";
            columnHeader3.Width = 400;
            // 
            // columnHeader4
            // 
            columnHeader4.Name = "columnHeader4";
            columnHeader4.Text = "Spec2";
            columnHeader4.Width = 200;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(24, 89);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(49, 25);
            label1.TabIndex = 1;
            label1.Text = "Type";
            // 
            // _typeCmb
            // 
            _typeCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _typeCmb.FormattingEnabled = true;
            _typeCmb.Items.AddRange(new object[] { "CSV", "Excel", "Google" });
            _typeCmb.Location = new System.Drawing.Point(76, 84);
            _typeCmb.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _typeCmb.Name = "_typeCmb";
            _typeCmb.Size = new System.Drawing.Size(188, 33);
            _typeCmb.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(24, 152);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(42, 25);
            label2.TabIndex = 3;
            label2.Text = "Sep";
            // 
            // _sepTxt
            // 
            _sepTxt.Location = new System.Drawing.Point(74, 149);
            _sepTxt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _sepTxt.Name = "_sepTxt";
            _sepTxt.Size = new System.Drawing.Size(190, 31);
            _sepTxt.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(294, 89);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(60, 25);
            label3.TabIndex = 5;
            label3.Text = "Spec1";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(294, 152);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(60, 25);
            label4.TabIndex = 6;
            label4.Text = "Spec2";
            // 
            // _spec1Txt
            // 
            _spec1Txt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _spec1Txt.Location = new System.Drawing.Point(362, 85);
            _spec1Txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _spec1Txt.Name = "_spec1Txt";
            _spec1Txt.Size = new System.Drawing.Size(714, 31);
            _spec1Txt.TabIndex = 7;
            // 
            // _spec2Txt
            // 
            _spec2Txt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _spec2Txt.Location = new System.Drawing.Point(362, 149);
            _spec2Txt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _spec2Txt.Name = "_spec2Txt";
            _spec2Txt.Size = new System.Drawing.Size(714, 31);
            _spec2Txt.TabIndex = 8;
            // 
            // _browseBtn
            // 
            _browseBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _browseBtn.Location = new System.Drawing.Point(1118, 84);
            _browseBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _browseBtn.Name = "_browseBtn";
            _browseBtn.Size = new System.Drawing.Size(118, 36);
            _browseBtn.TabIndex = 9;
            _browseBtn.Text = "Browse...";
            _browseBtn.UseVisualStyleBackColor = true;
            _browseBtn.Click += _browseBtn_Click;
            // 
            // _openFileDialog
            // 
            _openFileDialog.FileName = "openFileDialog1";
            // 
            // _addBtn
            // 
            _addBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _addBtn.Location = new System.Drawing.Point(1118, 148);
            _addBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _addBtn.Name = "_addBtn";
            _addBtn.Size = new System.Drawing.Size(118, 36);
            _addBtn.TabIndex = 10;
            _addBtn.Text = "Add";
            _addBtn.UseVisualStyleBackColor = true;
            _addBtn.Click += _addBtn_Click;
            // 
            // _saveBtn
            // 
            _saveBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _saveBtn.Location = new System.Drawing.Point(21, 836);
            _saveBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _saveBtn.Name = "_saveBtn";
            _saveBtn.Size = new System.Drawing.Size(118, 36);
            _saveBtn.TabIndex = 11;
            _saveBtn.Text = "Save";
            _saveBtn.UseVisualStyleBackColor = true;
            _saveBtn.Click += _saveBtn_Click;
            // 
            // _runBtn
            // 
            _runBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _runBtn.Location = new System.Drawing.Point(1121, 836);
            _runBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _runBtn.Name = "_runBtn";
            _runBtn.Size = new System.Drawing.Size(118, 36);
            _runBtn.TabIndex = 12;
            _runBtn.Text = "Run";
            _runBtn.UseVisualStyleBackColor = true;
            _runBtn.Click += _runBtn_Click;
            // 
            // _baseLineChk
            // 
            _baseLineChk.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            _baseLineChk.AutoSize = true;
            _baseLineChk.Location = new System.Drawing.Point(152, 840);
            _baseLineChk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _baseLineChk.Name = "_baseLineChk";
            _baseLineChk.Size = new System.Drawing.Size(101, 29);
            _baseLineChk.TabIndex = 13;
            _baseLineChk.Text = "Baseline";
            _baseLineChk.UseVisualStyleBackColor = true;
            // 
            // _canelBtn
            // 
            _canelBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _canelBtn.Location = new System.Drawing.Point(991, 836);
            _canelBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _canelBtn.Name = "_canelBtn";
            _canelBtn.Size = new System.Drawing.Size(118, 36);
            _canelBtn.TabIndex = 14;
            _canelBtn.Text = "Cancel...";
            _canelBtn.UseVisualStyleBackColor = true;
            _canelBtn.Click += _canelBtn_Click;
            // 
            // _tabCtrl
            // 
            _tabCtrl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _tabCtrl.Controls.Add(tabPage1);
            _tabCtrl.Controls.Add(tabPage2);
            _tabCtrl.Location = new System.Drawing.Point(21, 194);
            _tabCtrl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _tabCtrl.Name = "_tabCtrl";
            _tabCtrl.SelectedIndex = 0;
            _tabCtrl.Size = new System.Drawing.Size(1218, 635);
            _tabCtrl.TabIndex = 15;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(_testsLst);
            tabPage1.Location = new System.Drawing.Point(4, 34);
            tabPage1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tabPage1.Size = new System.Drawing.Size(1210, 597);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Input";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(_outputTxt);
            tabPage2.Location = new System.Drawing.Point(4, 34);
            tabPage2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tabPage2.Size = new System.Drawing.Size(1210, 597);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Output";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // _outputTxt
            // 
            _outputTxt.Dock = System.Windows.Forms.DockStyle.Fill;
            _outputTxt.Location = new System.Drawing.Point(4, 4);
            _outputTxt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _outputTxt.Name = "_outputTxt";
            _outputTxt.ReadOnly = true;
            _outputTxt.Size = new System.Drawing.Size(1202, 589);
            _outputTxt.TabIndex = 0;
            _outputTxt.Text = "";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(10, 24);
            label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(69, 25);
            label5.TabIndex = 16;
            label5.Text = "Output";
            // 
            // _outputPathTxt
            // 
            _outputPathTxt.Location = new System.Drawing.Point(81, 22);
            _outputPathTxt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _outputPathTxt.Name = "_outputPathTxt";
            _outputPathTxt.Size = new System.Drawing.Size(739, 31);
            _outputPathTxt.TabIndex = 17;
            // 
            // _browseOutputBtn
            // 
            _browseOutputBtn.Location = new System.Drawing.Point(860, 20);
            _browseOutputBtn.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _browseOutputBtn.Name = "_browseOutputBtn";
            _browseOutputBtn.Size = new System.Drawing.Size(118, 36);
            _browseOutputBtn.TabIndex = 18;
            _browseOutputBtn.Text = "Browse...";
            _browseOutputBtn.UseVisualStyleBackColor = true;
            _browseOutputBtn.Click += _browseOutputBtn_Click;
            // 
            // _toggleChk
            // 
            _toggleChk.AutoSize = true;
            _toggleChk.Checked = true;
            _toggleChk.CheckState = System.Windows.Forms.CheckState.Checked;
            _toggleChk.Location = new System.Drawing.Point(188, 192);
            _toggleChk.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _toggleChk.Name = "_toggleChk";
            _toggleChk.Size = new System.Drawing.Size(91, 29);
            _toggleChk.TabIndex = 1;
            _toggleChk.Text = "Toggle";
            _toggleChk.UseVisualStyleBackColor = true;
            _toggleChk.CheckedChanged += _toggleChk_CheckedChanged;
            // 
            // _statusStrip
            // 
            _statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripStatusLabel });
            _statusStrip.Location = new System.Drawing.Point(0, 876);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 18, 0);
            _statusStrip.Size = new System.Drawing.Size(1259, 32);
            _statusStrip.TabIndex = 19;
            _statusStrip.Text = "statusStrip1";
            // 
            // _toolStripStatusLabel
            // 
            _toolStripStatusLabel.Name = "_toolStripStatusLabel";
            _toolStripStatusLabel.Size = new System.Drawing.Size(60, 25);
            _toolStripStatusLabel.Text = "Status";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(289, 195);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(92, 25);
            label6.TabIndex = 1;
            label6.Text = "Max Rows";
            // 
            // _maxRowsTxt
            // 
            _maxRowsTxt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _maxRowsTxt.Location = new System.Drawing.Point(392, 191);
            _maxRowsTxt.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            _maxRowsTxt.Name = "_maxRowsTxt";
            _maxRowsTxt.Size = new System.Drawing.Size(149, 31);
            _maxRowsTxt.TabIndex = 7;
            // 
            // _testBtn
            // 
            _testBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            _testBtn.Location = new System.Drawing.Point(860, 835);
            _testBtn.Margin = new System.Windows.Forms.Padding(4);
            _testBtn.Name = "_testBtn";
            _testBtn.Size = new System.Drawing.Size(118, 36);
            _testBtn.TabIndex = 20;
            _testBtn.Text = "Test";
            _testBtn.UseVisualStyleBackColor = true;
            _testBtn.Click += _testBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1259, 908);
            Controls.Add(_testBtn);
            Controls.Add(_maxRowsTxt);
            Controls.Add(label6);
            Controls.Add(_statusStrip);
            Controls.Add(_toggleChk);
            Controls.Add(_browseOutputBtn);
            Controls.Add(_outputPathTxt);
            Controls.Add(label5);
            Controls.Add(_tabCtrl);
            Controls.Add(_canelBtn);
            Controls.Add(_baseLineChk);
            Controls.Add(_runBtn);
            Controls.Add(_saveBtn);
            Controls.Add(_addBtn);
            Controls.Add(_browseBtn);
            Controls.Add(_spec2Txt);
            Controls.Add(_spec1Txt);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(_sepTxt);
            Controls.Add(label2);
            Controls.Add(_typeCmb);
            Controls.Add(label1);
            Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            Name = "Form1";
            Text = "MatrixEase Tester";
            _tabCtrl.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListView _testsLst;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox _typeCmb;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _sepTxt;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox _spec1Txt;
        private System.Windows.Forms.TextBox _spec2Txt;
        private System.Windows.Forms.Button _browseBtn;
        private System.Windows.Forms.OpenFileDialog _openFileDialog;
        private System.Windows.Forms.Button _addBtn;
        private System.Windows.Forms.Button _saveBtn;
        private System.Windows.Forms.Button _runBtn;
        private System.Windows.Forms.CheckBox _baseLineChk;
        private System.Windows.Forms.Button _canelBtn;
        private System.Windows.Forms.TabControl _tabCtrl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox _outputTxt;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox _outputPathTxt;
        private System.Windows.Forms.Button _browseOutputBtn;
        private System.Windows.Forms.FolderBrowserDialog _folderBrowserDialog;
        private System.Windows.Forms.CheckBox _toggleChk;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox _maxRowsTxt;
        private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
        private System.Windows.Forms.Button _testBtn;
    }
}

