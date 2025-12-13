namespace Laboratory.Gemotest
{
    partial class FormGemotestResult
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.TextBox txtPdf;
        private System.Windows.Forms.Button btnSavePdf;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabCl;
        private System.Windows.Forms.TabPage tabMb;
        private System.Windows.Forms.DataGridView gridCl;
        private System.Windows.Forms.DataGridView gridMb;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.txtPdf = new System.Windows.Forms.TextBox();
            this.btnSavePdf = new System.Windows.Forms.Button();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabCl = new System.Windows.Forms.TabPage();
            this.gridCl = new System.Windows.Forms.DataGridView();
            this.tabMb = new System.Windows.Forms.TabPage();
            this.gridMb = new System.Windows.Forms.DataGridView();
            this.tabMain.SuspendLayout();
            this.tabCl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCl)).BeginInit();
            this.tabMb.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMb)).BeginInit();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Location = new System.Drawing.Point(12, 12);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(63, 13);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "Заказ: -";
            // 
            // lblError
            // 
            this.lblError.AutoSize = true;
            this.lblError.Location = new System.Drawing.Point(12, 34);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(67, 13);
            this.lblError.TabIndex = 1;
            this.lblError.Text = "Ошибок нет";
            // 
            // txtPdf
            // 
            this.txtPdf.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPdf.Location = new System.Drawing.Point(15, 58);
            this.txtPdf.Name = "txtPdf";
            this.txtPdf.ReadOnly = true;
            this.txtPdf.Size = new System.Drawing.Size(673, 20);
            this.txtPdf.TabIndex = 2;
            // 
            // btnSavePdf
            // 
            this.btnSavePdf.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSavePdf.Location = new System.Drawing.Point(694, 56);
            this.btnSavePdf.Name = "btnSavePdf";
            this.btnSavePdf.Size = new System.Drawing.Size(94, 23);
            this.btnSavePdf.TabIndex = 3;
            this.btnSavePdf.Text = "Скачать PDF";
            this.btnSavePdf.UseVisualStyleBackColor = true;
            this.btnSavePdf.Click += new System.EventHandler(this.btnSavePdf_Click);
            // 
            // tabMain
            // 
            this.tabMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabMain.Controls.Add(this.tabCl);
            this.tabMain.Controls.Add(this.tabMb);
            this.tabMain.Location = new System.Drawing.Point(15, 90);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(773, 348);
            this.tabMain.TabIndex = 4;
            // 
            // tabCl
            // 
            this.tabCl.Controls.Add(this.gridCl);
            this.tabCl.Location = new System.Drawing.Point(4, 22);
            this.tabCl.Name = "tabCl";
            this.tabCl.Padding = new System.Windows.Forms.Padding(3);
            this.tabCl.Size = new System.Drawing.Size(765, 322);
            this.tabCl.TabIndex = 0;
            this.tabCl.Text = "Результаты";
            this.tabCl.UseVisualStyleBackColor = true;
            // 
            // gridCl
            // 
            this.gridCl.AllowUserToAddRows = false;
            this.gridCl.AllowUserToDeleteRows = false;
            this.gridCl.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridCl.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridCl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridCl.Location = new System.Drawing.Point(3, 3);
            this.gridCl.MultiSelect = false;
            this.gridCl.Name = "gridCl";
            this.gridCl.ReadOnly = true;
            this.gridCl.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridCl.Size = new System.Drawing.Size(759, 316);
            this.gridCl.TabIndex = 0;
            // 
            // tabMb
            // 
            this.tabMb.Controls.Add(this.gridMb);
            this.tabMb.Location = new System.Drawing.Point(4, 22);
            this.tabMb.Name = "tabMb";
            this.tabMb.Padding = new System.Windows.Forms.Padding(3);
            this.tabMb.Size = new System.Drawing.Size(765, 322);
            this.tabMb.TabIndex = 1;
            this.tabMb.Text = "Микробиология";
            this.tabMb.UseVisualStyleBackColor = true;
            // 
            // gridMb
            // 
            this.gridMb.AllowUserToAddRows = false;
            this.gridMb.AllowUserToDeleteRows = false;
            this.gridMb.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridMb.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridMb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMb.Location = new System.Drawing.Point(3, 3);
            this.gridMb.MultiSelect = false;
            this.gridMb.Name = "gridMb";
            this.gridMb.ReadOnly = true;
            this.gridMb.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridMb.Size = new System.Drawing.Size(759, 316);
            this.gridMb.TabIndex = 0;
            // 
            // FormGemotestResult
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.btnSavePdf);
            this.Controls.Add(this.txtPdf);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.lblHeader);
            this.Name = "FormGemotestResult";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Результаты Гемотест";
            this.tabMain.ResumeLayout(false);
            this.tabCl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridCl)).EndInit();
            this.tabMb.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMb)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
