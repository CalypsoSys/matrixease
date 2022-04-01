
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
            this._testsLst = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.label1 = new System.Windows.Forms.Label();
            this._typeCmb = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this._sepTxt = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this._spec1Txt = new System.Windows.Forms.TextBox();
            this._spec2Txt = new System.Windows.Forms.TextBox();
            this._browseBtn = new System.Windows.Forms.Button();
            this._openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._addBtn = new System.Windows.Forms.Button();
            this._saveBtn = new System.Windows.Forms.Button();
            this._runBtn = new System.Windows.Forms.Button();
            this._baseLineChk = new System.Windows.Forms.CheckBox();
            this._canelBtn = new System.Windows.Forms.Button();
            this._tabCtrl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this._outputTxt = new System.Windows.Forms.RichTextBox();
            this._tabCtrl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // _testsLst
            // 
            this._testsLst.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this._testsLst.Dock = System.Windows.Forms.DockStyle.Fill;
            this._testsLst.HideSelection = false;
            this._testsLst.Location = new System.Drawing.Point(3, 3);
            this._testsLst.Name = "_testsLst";
            this._testsLst.Size = new System.Drawing.Size(753, 270);
            this._testsLst.TabIndex = 0;
            this._testsLst.UseCompatibleStateImageBehavior = false;
            this._testsLst.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Name = "columnHeader1";
            this.columnHeader1.Text = "Type";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Name = "columnHeader2";
            this.columnHeader2.Text = "Sep";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Name = "columnHeader3";
            this.columnHeader3.Text = "Spec1";
            this.columnHeader3.Width = 400;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Name = "columnHeader4";
            this.columnHeader4.Text = "Spec2";
            this.columnHeader4.Width = 200;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Type";
            // 
            // _typeCmb
            // 
            this._typeCmb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._typeCmb.FormattingEnabled = true;
            this._typeCmb.Items.AddRange(new object[] {
            "CSV",
            "Excel",
            "Google"});
            this._typeCmb.Location = new System.Drawing.Point(64, 10);
            this._typeCmb.Name = "_typeCmb";
            this._typeCmb.Size = new System.Drawing.Size(151, 28);
            this._typeCmb.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Sep";
            // 
            // _sepTxt
            // 
            this._sepTxt.Location = new System.Drawing.Point(62, 62);
            this._sepTxt.Name = "_sepTxt";
            this._sepTxt.Size = new System.Drawing.Size(153, 27);
            this._sepTxt.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(238, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Spec1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(238, 65);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 20);
            this.label4.TabIndex = 6;
            this.label4.Text = "Spec2";
            // 
            // _spec1Txt
            // 
            this._spec1Txt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._spec1Txt.Location = new System.Drawing.Point(293, 10);
            this._spec1Txt.Name = "_spec1Txt";
            this._spec1Txt.Size = new System.Drawing.Size(365, 27);
            this._spec1Txt.TabIndex = 7;
            // 
            // _spec2Txt
            // 
            this._spec2Txt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._spec2Txt.Location = new System.Drawing.Point(293, 62);
            this._spec2Txt.Name = "_spec2Txt";
            this._spec2Txt.Size = new System.Drawing.Size(365, 27);
            this._spec2Txt.TabIndex = 8;
            // 
            // _browseBtn
            // 
            this._browseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._browseBtn.Location = new System.Drawing.Point(690, 10);
            this._browseBtn.Name = "_browseBtn";
            this._browseBtn.Size = new System.Drawing.Size(94, 29);
            this._browseBtn.TabIndex = 9;
            this._browseBtn.Text = "Browse...";
            this._browseBtn.UseVisualStyleBackColor = true;
            this._browseBtn.Click += new System.EventHandler(this._browseBtn_Click);
            // 
            // _openFileDialog
            // 
            this._openFileDialog.FileName = "openFileDialog1";
            // 
            // _addBtn
            // 
            this._addBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._addBtn.Location = new System.Drawing.Point(690, 61);
            this._addBtn.Name = "_addBtn";
            this._addBtn.Size = new System.Drawing.Size(94, 29);
            this._addBtn.TabIndex = 10;
            this._addBtn.Text = "Add";
            this._addBtn.UseVisualStyleBackColor = true;
            this._addBtn.Click += new System.EventHandler(this._addBtn_Click);
            // 
            // _saveBtn
            // 
            this._saveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._saveBtn.Location = new System.Drawing.Point(17, 410);
            this._saveBtn.Name = "_saveBtn";
            this._saveBtn.Size = new System.Drawing.Size(94, 29);
            this._saveBtn.TabIndex = 11;
            this._saveBtn.Text = "Save";
            this._saveBtn.UseVisualStyleBackColor = true;
            this._saveBtn.Click += new System.EventHandler(this._saveBtn_Click);
            // 
            // _runBtn
            // 
            this._runBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._runBtn.Location = new System.Drawing.Point(690, 410);
            this._runBtn.Name = "_runBtn";
            this._runBtn.Size = new System.Drawing.Size(94, 29);
            this._runBtn.TabIndex = 12;
            this._runBtn.Text = "Run";
            this._runBtn.UseVisualStyleBackColor = true;
            this._runBtn.Click += new System.EventHandler(this._runBtn_Click);
            // 
            // _baseLineChk
            // 
            this._baseLineChk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._baseLineChk.AutoSize = true;
            this._baseLineChk.Location = new System.Drawing.Point(122, 412);
            this._baseLineChk.Name = "_baseLineChk";
            this._baseLineChk.Size = new System.Drawing.Size(86, 24);
            this._baseLineChk.TabIndex = 13;
            this._baseLineChk.Text = "Baseline";
            this._baseLineChk.UseVisualStyleBackColor = true;
            // 
            // _canelBtn
            // 
            this._canelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._canelBtn.Location = new System.Drawing.Point(586, 410);
            this._canelBtn.Name = "_canelBtn";
            this._canelBtn.Size = new System.Drawing.Size(94, 29);
            this._canelBtn.TabIndex = 14;
            this._canelBtn.Text = "Cancel...";
            this._canelBtn.UseVisualStyleBackColor = true;
            this._canelBtn.Click += new System.EventHandler(this._canelBtn_Click);
            // 
            // _tabCtrl
            // 
            this._tabCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._tabCtrl.Controls.Add(this.tabPage1);
            this._tabCtrl.Controls.Add(this.tabPage2);
            this._tabCtrl.Location = new System.Drawing.Point(17, 95);
            this._tabCtrl.Name = "_tabCtrl";
            this._tabCtrl.SelectedIndex = 0;
            this._tabCtrl.Size = new System.Drawing.Size(767, 309);
            this._tabCtrl.TabIndex = 15;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this._testsLst);
            this.tabPage1.Location = new System.Drawing.Point(4, 29);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(759, 276);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Input";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this._outputTxt);
            this.tabPage2.Location = new System.Drawing.Point(4, 29);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(759, 276);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Output";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // _outputTxt
            // 
            this._outputTxt.Dock = System.Windows.Forms.DockStyle.Fill;
            this._outputTxt.Location = new System.Drawing.Point(3, 3);
            this._outputTxt.Name = "_outputTxt";
            this._outputTxt.ReadOnly = true;
            this._outputTxt.Size = new System.Drawing.Size(753, 270);
            this._outputTxt.TabIndex = 0;
            this._outputTxt.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this._tabCtrl);
            this.Controls.Add(this._canelBtn);
            this.Controls.Add(this._baseLineChk);
            this.Controls.Add(this._runBtn);
            this.Controls.Add(this._saveBtn);
            this.Controls.Add(this._addBtn);
            this.Controls.Add(this._browseBtn);
            this.Controls.Add(this._spec2Txt);
            this.Controls.Add(this._spec1Txt);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._sepTxt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._typeCmb);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "MatrixEase Tester";
            this._tabCtrl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}

