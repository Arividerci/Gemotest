using System.Windows.Forms;

namespace Laboratory.Gemotest
{
    partial class FormAdditionalPatientInfo
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        private void InitializeComponent()
        {
            this.groupBoxAddress = new System.Windows.Forms.GroupBox();
            this.textBoxRepresentativeRegion = new System.Windows.Forms.TextBox();
            this.labelRepresentativeRegion = new System.Windows.Forms.Label();
            this.textBoxRepresentativeActualAddress = new System.Windows.Forms.TextBox();
            this.labelRepresentativeActualAddress = new System.Windows.Forms.Label();
            this.textBoxActualAddress = new System.Windows.Forms.TextBox();
            this.labelActualAddress = new System.Windows.Forms.Label();
            this.textBoxAddress = new System.Windows.Forms.TextBox();
            this.labelAddress = new System.Windows.Forms.Label();
            this.textBoxCity = new System.Windows.Forms.TextBox();
            this.labelCity = new System.Windows.Forms.Label();
            this.groupBoxPassport = new System.Windows.Forms.GroupBox();
            this.textBoxPassportIssuedBy = new System.Windows.Forms.TextBox();
            this.labelPassportIssuedBy = new System.Windows.Forms.Label();
            this.dateTimePassportIssued = new System.Windows.Forms.DateTimePicker();
            this.labelPassportIssued = new System.Windows.Forms.Label();
            this.textBoxPassport = new System.Windows.Forms.TextBox();
            this.labelPassport = new System.Windows.Forms.Label();
            this.groupBoxSnils = new System.Windows.Forms.GroupBox();
            this.textBoxSnils = new System.Windows.Forms.TextBox();
            this.labelSnils = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBoxPhone = new System.Windows.Forms.TextBox();
            this.PhoneNumber_label = new System.Windows.Forms.Label();
            this.textBoxMail = new System.Windows.Forms.TextBox();
            this.Mail_label = new System.Windows.Forms.Label();
            this.groupBoxAddress.SuspendLayout();
            this.groupBoxPassport.SuspendLayout();
            this.groupBoxSnils.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxAddress
            // 
            this.groupBoxAddress.Controls.Add(this.textBoxRepresentativeRegion);
            this.groupBoxAddress.Controls.Add(this.labelRepresentativeRegion);
            this.groupBoxAddress.Controls.Add(this.textBoxRepresentativeActualAddress);
            this.groupBoxAddress.Controls.Add(this.labelRepresentativeActualAddress);
            this.groupBoxAddress.Controls.Add(this.textBoxActualAddress);
            this.groupBoxAddress.Controls.Add(this.labelActualAddress);
            this.groupBoxAddress.Controls.Add(this.textBoxAddress);
            this.groupBoxAddress.Controls.Add(this.labelAddress);
            this.groupBoxAddress.Controls.Add(this.textBoxCity);
            this.groupBoxAddress.Controls.Add(this.labelCity);
            this.groupBoxAddress.Location = new System.Drawing.Point(12, 12);
            this.groupBoxAddress.Name = "groupBoxAddress";
            this.groupBoxAddress.Size = new System.Drawing.Size(580, 210);
            this.groupBoxAddress.TabIndex = 0;
            this.groupBoxAddress.TabStop = false;
            this.groupBoxAddress.Text = "Адреса";
            // 
            // textBoxRepresentativeRegion
            // 
            this.textBoxRepresentativeRegion.Location = new System.Drawing.Point(230, 170);
            this.textBoxRepresentativeRegion.Name = "textBoxRepresentativeRegion";
            this.textBoxRepresentativeRegion.Size = new System.Drawing.Size(334, 22);
            this.textBoxRepresentativeRegion.TabIndex = 4;
            // 
            // labelRepresentativeRegion
            // 
            this.labelRepresentativeRegion.AutoSize = true;
            this.labelRepresentativeRegion.Location = new System.Drawing.Point(10, 173);
            this.labelRepresentativeRegion.Name = "labelRepresentativeRegion";
            this.labelRepresentativeRegion.Size = new System.Drawing.Size(240, 16);
            this.labelRepresentativeRegion.TabIndex = 8;
            this.labelRepresentativeRegion.Text = "Регион проживания представителя";
            // 
            // textBoxRepresentativeActualAddress
            // 
            this.textBoxRepresentativeActualAddress.Location = new System.Drawing.Point(230, 130);
            this.textBoxRepresentativeActualAddress.Name = "textBoxRepresentativeActualAddress";
            this.textBoxRepresentativeActualAddress.Size = new System.Drawing.Size(334, 22);
            this.textBoxRepresentativeActualAddress.TabIndex = 3;
            // 
            // labelRepresentativeActualAddress
            // 
            this.labelRepresentativeActualAddress.AutoSize = true;
            this.labelRepresentativeActualAddress.Location = new System.Drawing.Point(10, 133);
            this.labelRepresentativeActualAddress.Name = "labelRepresentativeActualAddress";
            this.labelRepresentativeActualAddress.Size = new System.Drawing.Size(188, 16);
            this.labelRepresentativeActualAddress.TabIndex = 6;
            this.labelRepresentativeActualAddress.Text = "Факт. адрес представителя";
            // 
            // textBoxActualAddress
            // 
            this.textBoxActualAddress.Location = new System.Drawing.Point(230, 90);
            this.textBoxActualAddress.Name = "textBoxActualAddress";
            this.textBoxActualAddress.Size = new System.Drawing.Size(334, 22);
            this.textBoxActualAddress.TabIndex = 2;
            // 
            // labelActualAddress
            // 
            this.labelActualAddress.AutoSize = true;
            this.labelActualAddress.Location = new System.Drawing.Point(10, 93);
            this.labelActualAddress.Name = "labelActualAddress";
            this.labelActualAddress.Size = new System.Drawing.Size(202, 16);
            this.labelActualAddress.TabIndex = 4;
            this.labelActualAddress.Text = "Фактический адрес пациента";
            // 
            // textBoxAddress
            // 
            this.textBoxAddress.Location = new System.Drawing.Point(230, 60);
            this.textBoxAddress.Name = "textBoxAddress";
            this.textBoxAddress.Size = new System.Drawing.Size(334, 22);
            this.textBoxAddress.TabIndex = 1;
            // 
            // labelAddress
            // 
            this.labelAddress.AutoSize = true;
            this.labelAddress.Location = new System.Drawing.Point(10, 63);
            this.labelAddress.Name = "labelAddress";
            this.labelAddress.Size = new System.Drawing.Size(206, 16);
            this.labelAddress.TabIndex = 2;
            this.labelAddress.Text = "Адрес регистрации / прописки";
            // 
            // textBoxCity
            // 
            this.textBoxCity.Location = new System.Drawing.Point(230, 30);
            this.textBoxCity.Name = "textBoxCity";
            this.textBoxCity.Size = new System.Drawing.Size(334, 22);
            this.textBoxCity.TabIndex = 0;
            // 
            // labelCity
            // 
            this.labelCity.AutoSize = true;
            this.labelCity.Location = new System.Drawing.Point(10, 33);
            this.labelCity.Name = "labelCity";
            this.labelCity.Size = new System.Drawing.Size(129, 16);
            this.labelCity.TabIndex = 0;
            this.labelCity.Text = "Город проживания";
            // 
            // groupBoxPassport
            // 
            this.groupBoxPassport.Controls.Add(this.textBoxPassportIssuedBy);
            this.groupBoxPassport.Controls.Add(this.labelPassportIssuedBy);
            this.groupBoxPassport.Controls.Add(this.dateTimePassportIssued);
            this.groupBoxPassport.Controls.Add(this.labelPassportIssued);
            this.groupBoxPassport.Controls.Add(this.textBoxPassport);
            this.groupBoxPassport.Controls.Add(this.labelPassport);
            this.groupBoxPassport.Location = new System.Drawing.Point(12, 230);
            this.groupBoxPassport.Name = "groupBoxPassport";
            this.groupBoxPassport.Size = new System.Drawing.Size(580, 120);
            this.groupBoxPassport.TabIndex = 1;
            this.groupBoxPassport.TabStop = false;
            this.groupBoxPassport.Text = "Паспорт";
            // 
            // textBoxPassportIssuedBy
            // 
            this.textBoxPassportIssuedBy.Location = new System.Drawing.Point(230, 85);
            this.textBoxPassportIssuedBy.Name = "textBoxPassportIssuedBy";
            this.textBoxPassportIssuedBy.Size = new System.Drawing.Size(334, 22);
            this.textBoxPassportIssuedBy.TabIndex = 2;
            // 
            // labelPassportIssuedBy
            // 
            this.labelPassportIssuedBy.AutoSize = true;
            this.labelPassportIssuedBy.Location = new System.Drawing.Point(10, 88);
            this.labelPassportIssuedBy.Name = "labelPassportIssuedBy";
            this.labelPassportIssuedBy.Size = new System.Drawing.Size(76, 16);
            this.labelPassportIssuedBy.TabIndex = 4;
            this.labelPassportIssuedBy.Text = "Кем выдан";
            // 
            // dateTimePassportIssued
            // 
            this.dateTimePassportIssued.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePassportIssued.Location = new System.Drawing.Point(230, 55);
            this.dateTimePassportIssued.Name = "dateTimePassportIssued";
            this.dateTimePassportIssued.Size = new System.Drawing.Size(120, 22);
            this.dateTimePassportIssued.TabIndex = 1;
            // 
            // labelPassportIssued
            // 
            this.labelPassportIssued.AutoSize = true;
            this.labelPassportIssued.Location = new System.Drawing.Point(10, 58);
            this.labelPassportIssued.Name = "labelPassportIssued";
            this.labelPassportIssued.Size = new System.Drawing.Size(91, 16);
            this.labelPassportIssued.TabIndex = 2;
            this.labelPassportIssued.Text = "Дата выдачи";
            // 
            // textBoxPassport
            // 
            this.textBoxPassport.Location = new System.Drawing.Point(230, 25);
            this.textBoxPassport.Name = "textBoxPassport";
            this.textBoxPassport.Size = new System.Drawing.Size(334, 22);
            this.textBoxPassport.TabIndex = 0;
            // 
            // labelPassport
            // 
            this.labelPassport.AutoSize = true;
            this.labelPassport.Location = new System.Drawing.Point(10, 28);
            this.labelPassport.Name = "labelPassport";
            this.labelPassport.Size = new System.Drawing.Size(129, 16);
            this.labelPassport.TabIndex = 0;
            this.labelPassport.Text = "Паспорт пациента";
            // 
            // groupBoxSnils
            // 
            this.groupBoxSnils.Controls.Add(this.textBoxSnils);
            this.groupBoxSnils.Controls.Add(this.labelSnils);
            this.groupBoxSnils.Location = new System.Drawing.Point(12, 356);
            this.groupBoxSnils.Name = "groupBoxSnils";
            this.groupBoxSnils.Size = new System.Drawing.Size(580, 60);
            this.groupBoxSnils.TabIndex = 2;
            this.groupBoxSnils.TabStop = false;
            this.groupBoxSnils.Text = "СНИЛС";
            // 
            // textBoxSnils
            // 
            this.textBoxSnils.Location = new System.Drawing.Point(230, 25);
            this.textBoxSnils.Name = "textBoxSnils";
            this.textBoxSnils.Size = new System.Drawing.Size(334, 22);
            this.textBoxSnils.TabIndex = 0;
            // 
            // labelSnils
            // 
            this.labelSnils.AutoSize = true;
            this.labelSnils.Location = new System.Drawing.Point(10, 28);
            this.labelSnils.Name = "labelSnils";
            this.labelSnils.Size = new System.Drawing.Size(120, 16);
            this.labelSnils.TabIndex = 0;
            this.labelSnils.Text = "СНИЛС пациента";
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Location = new System.Drawing.Point(436, 537);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 28);
            this.buttonOk.TabIndex = 3;
            this.buttonOk.Text = "ОК";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(517, 537);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 28);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "Отмена";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Mail_label);
            this.groupBox1.Controls.Add(this.textBoxMail);
            this.groupBox1.Controls.Add(this.textBoxPhone);
            this.groupBox1.Controls.Add(this.PhoneNumber_label);
            this.groupBox1.Location = new System.Drawing.Point(12, 430);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(580, 100);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Контактная информация";
            // 
            // textBoxPhone
            // 
            this.textBoxPhone.Location = new System.Drawing.Point(230, 25);
            this.textBoxPhone.Name = "textBoxPhone";
            this.textBoxPhone.Size = new System.Drawing.Size(334, 22);
            this.textBoxPhone.TabIndex = 0;
            // 
            // PhoneNumber_label
            // 
            this.PhoneNumber_label.AutoSize = true;
            this.PhoneNumber_label.Location = new System.Drawing.Point(6, 31);
            this.PhoneNumber_label.Name = "PhoneNumber_label";
            this.PhoneNumber_label.Size = new System.Drawing.Size(119, 16);
            this.PhoneNumber_label.TabIndex = 0;
            this.PhoneNumber_label.Text = "Номер телефона";
            // 
            // textBoxMail
            // 
            this.textBoxMail.Location = new System.Drawing.Point(230, 63);
            this.textBoxMail.Name = "textBoxMail";
            this.textBoxMail.Size = new System.Drawing.Size(334, 22);
            this.textBoxMail.TabIndex = 1;
            // 
            // Mail_label
            // 
            this.Mail_label.AutoSize = true;
            this.Mail_label.Location = new System.Drawing.Point(10, 63);
            this.Mail_label.Name = "Mail_label";
            this.Mail_label.Size = new System.Drawing.Size(48, 16);
            this.Mail_label.TabIndex = 2;
            this.Mail_label.Text = "Почта";
            // 
            // FormAdditionalPatientInfo
            // 
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(604, 577);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.groupBoxSnils);
            this.Controls.Add(this.groupBoxPassport);
            this.Controls.Add(this.groupBoxAddress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormAdditionalPatientInfo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Дополнительные данные пациента";
            this.groupBoxAddress.ResumeLayout(false);
            this.groupBoxAddress.PerformLayout();
            this.groupBoxPassport.ResumeLayout(false);
            this.groupBoxPassport.PerformLayout();
            this.groupBoxSnils.ResumeLayout(false);
            this.groupBoxSnils.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxAddress;
        private System.Windows.Forms.TextBox textBoxRepresentativeRegion;
        private System.Windows.Forms.Label labelRepresentativeRegion;
        private System.Windows.Forms.TextBox textBoxRepresentativeActualAddress;
        private System.Windows.Forms.Label labelRepresentativeActualAddress;
        private System.Windows.Forms.TextBox textBoxActualAddress;
        private System.Windows.Forms.Label labelActualAddress;
        private System.Windows.Forms.TextBox textBoxAddress;
        private System.Windows.Forms.Label labelAddress;
        private System.Windows.Forms.TextBox textBoxCity;
        private System.Windows.Forms.Label labelCity;
        private System.Windows.Forms.GroupBox groupBoxPassport;
        private System.Windows.Forms.TextBox textBoxPassportIssuedBy;
        private System.Windows.Forms.Label labelPassportIssuedBy;
        private System.Windows.Forms.DateTimePicker dateTimePassportIssued;
        private System.Windows.Forms.Label labelPassportIssued;
        private System.Windows.Forms.TextBox textBoxPassport;
        private System.Windows.Forms.Label labelPassport;
        private System.Windows.Forms.GroupBox groupBoxSnils;
        private System.Windows.Forms.TextBox textBoxSnils;
        private System.Windows.Forms.Label labelSnils;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private GroupBox groupBox1;
        private Label Mail_label;
        private TextBox textBoxMail;
        private TextBox textBoxPhone;
        private Label PhoneNumber_label;
    }
}
