namespace Laboratory.Gemotest.Options
{
    partial class LocalOptionsForm
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
            this.pdfPrinterLabel = new System.Windows.Forms.Label();
            this.printer1Label = new System.Windows.Forms.Label();
            this.btPdfPrinter = new System.Windows.Forms.Button();
            this.receptacleLabel = new System.Windows.Forms.Label();
            this.stickerLabel = new System.Windows.Forms.Label();
            this.btStickerPrinter = new System.Windows.Forms.Button();
            this.printer2Label = new System.Windows.Forms.Label();
            this.btSave = new System.Windows.Forms.Button();
            this.btCancel = new System.Windows.Forms.Button();
            this.chbPrintatOnce = new System.Windows.Forms.CheckBox();
            this.receiverLabel = new System.Windows.Forms.Label();
            this.btClearPdfPrinter = new System.Windows.Forms.Button();
            this.btClearStickerPrinter = new System.Windows.Forms.Button();
            this.rdbtnLabelEncoding2 = new System.Windows.Forms.RadioButton();
            this.rdbtnLabelEncoding1 = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.gbTemplate = new System.Windows.Forms.GroupBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.bTestPrint = new System.Windows.Forms.Button();
            this.bRefrechLabelTemplate = new System.Windows.Forms.Button();
            this.bSetDefault_EPL = new System.Windows.Forms.Button();
            this.bSetDefault_ZPL = new System.Windows.Forms.Button();
            this.txtCustomLabel = new System.Windows.Forms.TextBox();
            this.pEncoding = new System.Windows.Forms.Panel();
            this.chbPrintStinkersAtOnce = new System.Windows.Forms.CheckBox();
            this.chbPrintBlankAtOnce = new System.Windows.Forms.CheckBox();
            this.gbTemplate.SuspendLayout();
            this.panel4.SuspendLayout();
            this.pEncoding.SuspendLayout();
            this.SuspendLayout();
            // 
            // pdfPrinterLabel
            // 
            this.pdfPrinterLabel.AutoSize = true;
            this.pdfPrinterLabel.Location = new System.Drawing.Point(12, 18);
            this.pdfPrinterLabel.Name = "pdfPrinterLabel";
            this.pdfPrinterLabel.Size = new System.Drawing.Size(325, 16);
            this.pdfPrinterLabel.TabIndex = 0;
            this.pdfPrinterLabel.Text = "Принтер для печати сопроводительных бланков";
            this.pdfPrinterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // printer1Label
            // 
            this.printer1Label.AutoSize = true;
            this.printer1Label.Location = new System.Drawing.Point(298, 18);
            this.printer1Label.Name = "printer1Label";
            this.printer1Label.Size = new System.Drawing.Size(164, 16);
            this.printer1Label.TabIndex = 1;
            this.printer1Label.Text = "Принтер по умолчанию ";
            this.printer1Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btPdfPrinter
            // 
            this.btPdfPrinter.AutoSize = true;
            this.btPdfPrinter.Location = new System.Drawing.Point(489, 13);
            this.btPdfPrinter.Name = "btPdfPrinter";
            this.btPdfPrinter.Size = new System.Drawing.Size(24, 23);
            this.btPdfPrinter.TabIndex = 0;
            this.btPdfPrinter.UseVisualStyleBackColor = true;
            this.btPdfPrinter.Click += new System.EventHandler(this.btPdfPrinter_Click);
            // 
            // receptacleLabel
            // 
            this.receptacleLabel.AutoSize = true;
            this.receptacleLabel.Location = new System.Drawing.Point(12, 46);
            this.receptacleLabel.Name = "receptacleLabel";
            this.receptacleLabel.Size = new System.Drawing.Size(46, 16);
            this.receptacleLabel.TabIndex = 3;
            this.receptacleLabel.Text = "Лоток";
            this.receptacleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stickerLabel
            // 
            this.stickerLabel.AutoSize = true;
            this.stickerLabel.Location = new System.Drawing.Point(12, 73);
            this.stickerLabel.Name = "stickerLabel";
            this.stickerLabel.Size = new System.Drawing.Size(197, 16);
            this.stickerLabel.TabIndex = 5;
            this.stickerLabel.Text = "Принтер для печати наклеек";
            this.stickerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btStickerPrinter
            // 
            this.btStickerPrinter.AutoSize = true;
            this.btStickerPrinter.Location = new System.Drawing.Point(489, 68);
            this.btStickerPrinter.Name = "btStickerPrinter";
            this.btStickerPrinter.Size = new System.Drawing.Size(24, 23);
            this.btStickerPrinter.TabIndex = 2;
            this.btStickerPrinter.UseVisualStyleBackColor = true;
            this.btStickerPrinter.Click += new System.EventHandler(this.btStickerPrinter_Click);
            // 
            // printer2Label
            // 
            this.printer2Label.AutoSize = true;
            this.printer2Label.Location = new System.Drawing.Point(298, 73);
            this.printer2Label.Name = "printer2Label";
            this.printer2Label.Size = new System.Drawing.Size(161, 16);
            this.printer2Label.TabIndex = 7;
            this.printer2Label.Text = "Принтер по умолчанию";
            this.printer2Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btSave
            // 
            this.btSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btSave.AutoSize = true;
            this.btSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btSave.Location = new System.Drawing.Point(130, 408);
            this.btSave.Name = "btSave";
            this.btSave.Size = new System.Drawing.Size(86, 26);
            this.btSave.TabIndex = 5;
            this.btSave.Text = "Сохранить";
            this.btSave.UseVisualStyleBackColor = true;
            this.btSave.Click += new System.EventHandler(this.btSave_Click);
            // 
            // btCancel
            // 
            this.btCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancel.Location = new System.Drawing.Point(354, 409);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(70, 25);
            this.btCancel.TabIndex = 6;
            this.btCancel.Text = "Отмена";
            this.btCancel.UseVisualStyleBackColor = true;
            // 
            // chbPrintatOnce
            // 
            this.chbPrintatOnce.AutoSize = true;
            this.chbPrintatOnce.Location = new System.Drawing.Point(12, 100);
            this.chbPrintatOnce.Margin = new System.Windows.Forms.Padding(7, 3, 3, 3);
            this.chbPrintatOnce.Name = "chbPrintatOnce";
            this.chbPrintatOnce.Size = new System.Drawing.Size(627, 20);
            this.chbPrintatOnce.TabIndex = 4;
            this.chbPrintatOnce.Text = "Печатать сопроводительный бланк и наклейки сразу после подготовки заказа к отправ" +
    "ке";
            this.chbPrintatOnce.UseVisualStyleBackColor = true;
            this.chbPrintatOnce.CheckedChanged += new System.EventHandler(this.chbPrintatOnce_CheckedChanged);
            // 
            // receiverLabel
            // 
            this.receiverLabel.AutoSize = true;
            this.receiverLabel.Location = new System.Drawing.Point(298, 46);
            this.receiverLabel.Name = "receiverLabel";
            this.receiverLabel.Size = new System.Drawing.Size(11, 16);
            this.receiverLabel.TabIndex = 14;
            this.receiverLabel.Text = "-";
            this.receiverLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btClearPdfPrinter
            // 
            this.btClearPdfPrinter.AutoSize = true;
            this.btClearPdfPrinter.Location = new System.Drawing.Point(519, 13);
            this.btClearPdfPrinter.Name = "btClearPdfPrinter";
            this.btClearPdfPrinter.Size = new System.Drawing.Size(25, 23);
            this.btClearPdfPrinter.TabIndex = 1;
            this.btClearPdfPrinter.UseVisualStyleBackColor = true;
            this.btClearPdfPrinter.Click += new System.EventHandler(this.btClearPdfPrinter_Click);
            // 
            // btClearStickerPrinter
            // 
            this.btClearStickerPrinter.AutoSize = true;
            this.btClearStickerPrinter.Location = new System.Drawing.Point(519, 68);
            this.btClearStickerPrinter.Name = "btClearStickerPrinter";
            this.btClearStickerPrinter.Size = new System.Drawing.Size(25, 23);
            this.btClearStickerPrinter.TabIndex = 3;
            this.btClearStickerPrinter.UseVisualStyleBackColor = true;
            this.btClearStickerPrinter.Click += new System.EventHandler(this.btClearStickerPrinter_Click);
            // 
            // rdbtnLabelEncoding2
            // 
            this.rdbtnLabelEncoding2.AutoSize = true;
            this.rdbtnLabelEncoding2.Location = new System.Drawing.Point(170, 9);
            this.rdbtnLabelEncoding2.Name = "rdbtnLabelEncoding2";
            this.rdbtnLabelEncoding2.Size = new System.Drawing.Size(85, 20);
            this.rdbtnLabelEncoding2.TabIndex = 1;
            this.rdbtnLabelEncoding2.TabStop = true;
            this.rdbtnLabelEncoding2.Text = "Code 866";
            this.rdbtnLabelEncoding2.UseVisualStyleBackColor = true;
            // 
            // rdbtnLabelEncoding1
            // 
            this.rdbtnLabelEncoding1.AutoSize = true;
            this.rdbtnLabelEncoding1.Location = new System.Drawing.Point(90, 9);
            this.rdbtnLabelEncoding1.Name = "rdbtnLabelEncoding1";
            this.rdbtnLabelEncoding1.Size = new System.Drawing.Size(62, 20);
            this.rdbtnLabelEncoding1.TabIndex = 0;
            this.rdbtnLabelEncoding1.TabStop = true;
            this.rdbtnLabelEncoding1.Text = "UTF8";
            this.rdbtnLabelEncoding1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 16);
            this.label2.TabIndex = 0;
            this.label2.Text = "Кодировка";
            // 
            // gbTemplate
            // 
            this.gbTemplate.Controls.Add(this.panel4);
            this.gbTemplate.Controls.Add(this.pEncoding);
            this.gbTemplate.Location = new System.Drawing.Point(0, 169);
            this.gbTemplate.Name = "gbTemplate";
            this.gbTemplate.Size = new System.Drawing.Size(544, 233);
            this.gbTemplate.TabIndex = 15;
            this.gbTemplate.TabStop = false;
            this.gbTemplate.Text = "Шаблон этикетки";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.bTestPrint);
            this.panel4.Controls.Add(this.bRefrechLabelTemplate);
            this.panel4.Controls.Add(this.bSetDefault_EPL);
            this.panel4.Controls.Add(this.bSetDefault_ZPL);
            this.panel4.Controls.Add(this.txtCustomLabel);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(3, 53);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(538, 177);
            this.panel4.TabIndex = 1;
            // 
            // bTestPrint
            // 
            this.bTestPrint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bTestPrint.Location = new System.Drawing.Point(6, 134);
            this.bTestPrint.Name = "bTestPrint";
            this.bTestPrint.Size = new System.Drawing.Size(112, 36);
            this.bTestPrint.TabIndex = 17;
            this.bTestPrint.Text = "Тестовая печать";
            this.bTestPrint.UseVisualStyleBackColor = true;
            this.bTestPrint.Click += new System.EventHandler(this.bTestPrint_Click);
            // 
            // bRefrechLabelTemplate
            // 
            this.bRefrechLabelTemplate.Location = new System.Drawing.Point(6, 90);
            this.bRefrechLabelTemplate.Name = "bRefrechLabelTemplate";
            this.bRefrechLabelTemplate.Size = new System.Drawing.Size(112, 36);
            this.bRefrechLabelTemplate.TabIndex = 18;
            this.bRefrechLabelTemplate.Text = "Отменить изменения";
            this.bRefrechLabelTemplate.UseVisualStyleBackColor = true;
            this.bRefrechLabelTemplate.Click += new System.EventHandler(this.bRefrechLabelTemplate_Click);
            // 
            // bSetDefault_EPL
            // 
            this.bSetDefault_EPL.Location = new System.Drawing.Point(6, 48);
            this.bSetDefault_EPL.Name = "bSetDefault_EPL";
            this.bSetDefault_EPL.Size = new System.Drawing.Size(112, 36);
            this.bSetDefault_EPL.TabIndex = 19;
            this.bSetDefault_EPL.Text = "EPL по умолчанию";
            this.bSetDefault_EPL.UseVisualStyleBackColor = true;
            this.bSetDefault_EPL.Click += new System.EventHandler(this.bSetDefault_EPL_Click);
            // 
            // bSetDefault_ZPL
            // 
            this.bSetDefault_ZPL.Location = new System.Drawing.Point(6, 6);
            this.bSetDefault_ZPL.Name = "bSetDefault_ZPL";
            this.bSetDefault_ZPL.Size = new System.Drawing.Size(112, 36);
            this.bSetDefault_ZPL.TabIndex = 20;
            this.bSetDefault_ZPL.Text = "ZPL по умолчанию";
            this.bSetDefault_ZPL.UseVisualStyleBackColor = true;
            this.bSetDefault_ZPL.Click += new System.EventHandler(this.bSetDefault_ZPL_Click);
            // 
            // txtCustomLabel
            // 
            this.txtCustomLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCustomLabel.Location = new System.Drawing.Point(124, 6);
            this.txtCustomLabel.Multiline = true;
            this.txtCustomLabel.Name = "txtCustomLabel";
            this.txtCustomLabel.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCustomLabel.Size = new System.Drawing.Size(405, 164);
            this.txtCustomLabel.TabIndex = 16;
            // 
            // pEncoding
            // 
            this.pEncoding.Controls.Add(this.rdbtnLabelEncoding2);
            this.pEncoding.Controls.Add(this.label2);
            this.pEncoding.Controls.Add(this.rdbtnLabelEncoding1);
            this.pEncoding.Dock = System.Windows.Forms.DockStyle.Top;
            this.pEncoding.Location = new System.Drawing.Point(3, 18);
            this.pEncoding.Name = "pEncoding";
            this.pEncoding.Size = new System.Drawing.Size(538, 35);
            this.pEncoding.TabIndex = 0;
            // 
            // chbPrintStinkersAtOnce
            // 
            this.chbPrintStinkersAtOnce.AutoSize = true;
            this.chbPrintStinkersAtOnce.Location = new System.Drawing.Point(12, 123);
            this.chbPrintStinkersAtOnce.Margin = new System.Windows.Forms.Padding(7, 3, 3, 3);
            this.chbPrintStinkersAtOnce.Name = "chbPrintStinkersAtOnce";
            this.chbPrintStinkersAtOnce.Size = new System.Drawing.Size(445, 20);
            this.chbPrintStinkersAtOnce.TabIndex = 4;
            this.chbPrintStinkersAtOnce.Text = "Печатать наклейки сразу после подготовки заказа к отправке";
            this.chbPrintStinkersAtOnce.UseVisualStyleBackColor = true;
            // 
            // chbPrintBlankAtOnce
            // 
            this.chbPrintBlankAtOnce.AutoSize = true;
            this.chbPrintBlankAtOnce.Location = new System.Drawing.Point(12, 146);
            this.chbPrintBlankAtOnce.Margin = new System.Windows.Forms.Padding(7, 3, 3, 3);
            this.chbPrintBlankAtOnce.Name = "chbPrintBlankAtOnce";
            this.chbPrintBlankAtOnce.Size = new System.Drawing.Size(551, 20);
            this.chbPrintBlankAtOnce.TabIndex = 4;
            this.chbPrintBlankAtOnce.Text = "Печатать сопроводительный бланк сразу после подготовки заказа к отправке";
            this.chbPrintBlankAtOnce.UseVisualStyleBackColor = true;
            // 
            // LocalOptionsForm
            // 
            this.AcceptButton = this.btSave;
            this.CancelButton = this.btCancel;
            this.ClientSize = new System.Drawing.Size(553, 446);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btSave);
            this.Controls.Add(this.gbTemplate);
            this.Controls.Add(this.btStickerPrinter);
            this.Controls.Add(this.chbPrintBlankAtOnce);
            this.Controls.Add(this.chbPrintStinkersAtOnce);
            this.Controls.Add(this.chbPrintatOnce);
            this.Controls.Add(this.stickerLabel);
            this.Controls.Add(this.btClearStickerPrinter);
            this.Controls.Add(this.printer2Label);
            this.Controls.Add(this.receptacleLabel);
            this.Controls.Add(this.btPdfPrinter);
            this.Controls.Add(this.printer1Label);
            this.Controls.Add(this.pdfPrinterLabel);
            this.Controls.Add(this.receiverLabel);
            this.Controls.Add(this.btClearPdfPrinter);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LocalOptionsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ЛИС «НМЦ Ситилаб» — пользовательские настройки";
            this.gbTemplate.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.pEncoding.ResumeLayout(false);
            this.pEncoding.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label pdfPrinterLabel;
        private System.Windows.Forms.Label printer1Label;
        private System.Windows.Forms.Button btPdfPrinter;
        private System.Windows.Forms.Label receptacleLabel;
        private System.Windows.Forms.Label stickerLabel;
        private System.Windows.Forms.Button btStickerPrinter;
        private System.Windows.Forms.Label printer2Label;
        private System.Windows.Forms.Button btSave;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.CheckBox chbPrintatOnce;
        private System.Windows.Forms.Label receiverLabel;
        private System.Windows.Forms.Button btClearPdfPrinter;
        private System.Windows.Forms.Button btClearStickerPrinter;
        private System.Windows.Forms.RadioButton rdbtnLabelEncoding2;
        private System.Windows.Forms.RadioButton rdbtnLabelEncoding1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox gbTemplate;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button bTestPrint;
        private System.Windows.Forms.Button bRefrechLabelTemplate;
        private System.Windows.Forms.Button bSetDefault_EPL;
        private System.Windows.Forms.Button bSetDefault_ZPL;
        private System.Windows.Forms.TextBox txtCustomLabel;
        private System.Windows.Forms.Panel pEncoding;
        private System.Windows.Forms.CheckBox chbPrintStinkersAtOnce;
        private System.Windows.Forms.CheckBox chbPrintBlankAtOnce;
    }
}