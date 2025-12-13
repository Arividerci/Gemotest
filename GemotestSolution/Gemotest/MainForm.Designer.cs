﻿namespace Gemotest
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.настройкиToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GemotestOptions_toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SystemOptions_toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LoadDictionaries_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.отладкаКонсольToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.включитьКонсольToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.выключитьКонсольToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CreateOrder_button = new System.Windows.Forms.Button();
            this.groupBoxOrder = new System.Windows.Forms.GroupBox();
            this.labelComment = new System.Windows.Forms.Label();
            this.textBoxComment = new System.Windows.Forms.TextBox();
            this.labelDoctor = new System.Windows.Forms.Label();
            this.textBoxDoctor = new System.Windows.Forms.TextBox();
            this.labelOrderNum = new System.Windows.Forms.Label();
            this.textBoxOrderNum = new System.Windows.Forms.TextBox();
            this.groupBoxPatient = new System.Windows.Forms.GroupBox();
            this.checkBoxAnonymous = new System.Windows.Forms.CheckBox();
            this.comboBoxSex = new System.Windows.Forms.ComboBox();
            this.labelSex = new System.Windows.Forms.Label();
            this.dateTimePickerBirthdate = new System.Windows.Forms.DateTimePicker();
            this.labelBirthdate = new System.Windows.Forms.Label();
            this.textBoxPatronymic = new System.Windows.Forms.TextBox();
            this.labelPatronymic = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.labelName = new System.Windows.Forms.Label();
            this.textBoxSurname = new System.Windows.Forms.TextBox();
            this.labelSurname = new System.Windows.Forms.Label();
            this.CheckResult_button = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.groupBoxOrder.SuspendLayout();
            this.groupBoxPatient.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.Gainsboro;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.настройкиToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(705, 28);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // настройкиToolStripMenuItem
            // 
            this.настройкиToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GemotestOptions_toolStripMenuItem,
            this.SystemOptions_toolStripMenuItem,
            this.LoadDictionaries_ToolStripMenuItem,
            this.отладкаКонсольToolStripMenuItem});
            this.настройкиToolStripMenuItem.Name = "настройкиToolStripMenuItem";
            this.настройкиToolStripMenuItem.Size = new System.Drawing.Size(98, 24);
            this.настройкиToolStripMenuItem.Text = "Настройки";
            // 
            // GemotestOptions_toolStripMenuItem
            // 
            this.GemotestOptions_toolStripMenuItem.Name = "GemotestOptions_toolStripMenuItem";
            this.GemotestOptions_toolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.GemotestOptions_toolStripMenuItem.Text = "Параметры ЛИС Гемотест";
            this.GemotestOptions_toolStripMenuItem.Click += new System.EventHandler(this.GemotestOptions_toolStripMenuItem_Click);
            // 
            // SystemOptions_toolStripMenuItem
            // 
            this.SystemOptions_toolStripMenuItem.Name = "SystemOptions_toolStripMenuItem";
            this.SystemOptions_toolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.SystemOptions_toolStripMenuItem.Text = "Параметы принтера";
            this.SystemOptions_toolStripMenuItem.Click += new System.EventHandler(this.SystemOptions_toolStripMenuItem_Click);
            // 
            // LoadDictionaries_ToolStripMenuItem
            // 
            this.LoadDictionaries_ToolStripMenuItem.Name = "LoadDictionaries_ToolStripMenuItem";
            this.LoadDictionaries_ToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.LoadDictionaries_ToolStripMenuItem.Text = "Загрузить справочники";
            this.LoadDictionaries_ToolStripMenuItem.Click += new System.EventHandler(this.LoadDictionaries_ToolStripMenuItem_Click);
            // 
            // отладкаКонсольToolStripMenuItem
            // 
            this.отладкаКонсольToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.включитьКонсольToolStripMenuItem,
            this.выключитьКонсольToolStripMenuItem});
            this.отладкаКонсольToolStripMenuItem.Name = "отладкаКонсольToolStripMenuItem";
            this.отладкаКонсольToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.отладкаКонсольToolStripMenuItem.Text = "Отладка / Консоль";
            // 
            // включитьКонсольToolStripMenuItem
            // 
            this.включитьКонсольToolStripMenuItem.ForeColor = System.Drawing.Color.DarkGreen;
            this.включитьКонсольToolStripMenuItem.Name = "включитьКонсольToolStripMenuItem";
            this.включитьКонсольToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            this.включитьКонсольToolStripMenuItem.Text = "Включить консоль";
            this.включитьКонсольToolStripMenuItem.Click += new System.EventHandler(this.включитьКонсольToolStripMenuItem_Click);
            // 
            // выключитьКонсольToolStripMenuItem
            // 
            this.выключитьКонсольToolStripMenuItem.ForeColor = System.Drawing.Color.Red;
            this.выключитьКонсольToolStripMenuItem.Name = "выключитьКонсольToolStripMenuItem";
            this.выключитьКонсольToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            this.выключитьКонсольToolStripMenuItem.Text = "Выключить консоль";
            this.выключитьКонсольToolStripMenuItem.Click += new System.EventHandler(this.выключитьКонсольToolStripMenuItem_Click);
            // 
            // CreateOrder_button
            // 
            this.CreateOrder_button.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.CreateOrder_button.FlatAppearance.BorderSize = 0;
            this.CreateOrder_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CreateOrder_button.Location = new System.Drawing.Point(501, 60);
            this.CreateOrder_button.Name = "CreateOrder_button";
            this.CreateOrder_button.Size = new System.Drawing.Size(169, 32);
            this.CreateOrder_button.TabIndex = 20;
            this.CreateOrder_button.Text = "Создать заказ";
            this.CreateOrder_button.UseVisualStyleBackColor = false;
            this.CreateOrder_button.Click += new System.EventHandler(this.CreateOrder_button_Click);
            // 
            // groupBoxOrder
            // 
            this.groupBoxOrder.Controls.Add(this.labelComment);
            this.groupBoxOrder.Controls.Add(this.textBoxComment);
            this.groupBoxOrder.Controls.Add(this.labelDoctor);
            this.groupBoxOrder.Controls.Add(this.textBoxDoctor);
            this.groupBoxOrder.Controls.Add(this.labelOrderNum);
            this.groupBoxOrder.Controls.Add(this.textBoxOrderNum);
            this.groupBoxOrder.Location = new System.Drawing.Point(12, 40);
            this.groupBoxOrder.Name = "groupBoxOrder";
            this.groupBoxOrder.Size = new System.Drawing.Size(420, 150);
            this.groupBoxOrder.TabIndex = 7;
            this.groupBoxOrder.TabStop = false;
            this.groupBoxOrder.Text = "Заказ";
            // 
            // labelComment
            // 
            this.labelComment.AutoSize = true;
            this.labelComment.Location = new System.Drawing.Point(10, 88);
            this.labelComment.Name = "labelComment";
            this.labelComment.Size = new System.Drawing.Size(99, 16);
            this.labelComment.TabIndex = 7;
            this.labelComment.Text = "Комментарий:";
            // 
            // textBoxComment
            // 
            this.textBoxComment.Location = new System.Drawing.Point(130, 85);
            this.textBoxComment.Multiline = true;
            this.textBoxComment.Name = "textBoxComment";
            this.textBoxComment.Size = new System.Drawing.Size(274, 48);
            this.textBoxComment.TabIndex = 3;
            // 
            // labelDoctor
            // 
            this.labelDoctor.AutoSize = true;
            this.labelDoctor.Location = new System.Drawing.Point(10, 58);
            this.labelDoctor.Name = "labelDoctor";
            this.labelDoctor.Size = new System.Drawing.Size(43, 16);
            this.labelDoctor.TabIndex = 5;
            this.labelDoctor.Text = "Врач:";
            // 
            // textBoxDoctor
            // 
            this.textBoxDoctor.Location = new System.Drawing.Point(130, 55);
            this.textBoxDoctor.Name = "textBoxDoctor";
            this.textBoxDoctor.Size = new System.Drawing.Size(274, 22);
            this.textBoxDoctor.TabIndex = 2;
            // 
            // labelOrderNum
            // 
            this.labelOrderNum.AutoSize = true;
            this.labelOrderNum.Location = new System.Drawing.Point(10, 33);
            this.labelOrderNum.Name = "labelOrderNum";
            this.labelOrderNum.Size = new System.Drawing.Size(103, 16);
            this.labelOrderNum.TabIndex = 3;
            this.labelOrderNum.Text = "Номер заказа:";
            // 
            // textBoxOrderNum
            // 
            this.textBoxOrderNum.Location = new System.Drawing.Point(130, 30);
            this.textBoxOrderNum.Name = "textBoxOrderNum";
            this.textBoxOrderNum.Size = new System.Drawing.Size(274, 22);
            this.textBoxOrderNum.TabIndex = 1;
            // 
            // groupBoxPatient
            // 
            this.groupBoxPatient.Controls.Add(this.checkBoxAnonymous);
            this.groupBoxPatient.Controls.Add(this.comboBoxSex);
            this.groupBoxPatient.Controls.Add(this.labelSex);
            this.groupBoxPatient.Controls.Add(this.dateTimePickerBirthdate);
            this.groupBoxPatient.Controls.Add(this.labelBirthdate);
            this.groupBoxPatient.Controls.Add(this.textBoxPatronymic);
            this.groupBoxPatient.Controls.Add(this.labelPatronymic);
            this.groupBoxPatient.Controls.Add(this.textBoxName);
            this.groupBoxPatient.Controls.Add(this.labelName);
            this.groupBoxPatient.Controls.Add(this.textBoxSurname);
            this.groupBoxPatient.Controls.Add(this.labelSurname);
            this.groupBoxPatient.Location = new System.Drawing.Point(12, 200);
            this.groupBoxPatient.Name = "groupBoxPatient";
            this.groupBoxPatient.Size = new System.Drawing.Size(510, 217);
            this.groupBoxPatient.TabIndex = 8;
            this.groupBoxPatient.TabStop = false;
            this.groupBoxPatient.Text = "Пациент";
            // 
            // checkBoxAnonymous
            // 
            this.checkBoxAnonymous.AutoSize = true;
            this.checkBoxAnonymous.Location = new System.Drawing.Point(392, 175);
            this.checkBoxAnonymous.Name = "checkBoxAnonymous";
            this.checkBoxAnonymous.Size = new System.Drawing.Size(104, 20);
            this.checkBoxAnonymous.TabIndex = 10;
            this.checkBoxAnonymous.Text = "Анонимный";
            this.checkBoxAnonymous.UseVisualStyleBackColor = true;
            // 
            // comboBoxSex
            // 
            this.comboBoxSex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSex.FormattingEnabled = true;
            this.comboBoxSex.Items.AddRange(new object[] {
            "Не указан",
            "Мужской",
            "Женский"});
            this.comboBoxSex.Location = new System.Drawing.Point(357, 145);
            this.comboBoxSex.Name = "comboBoxSex";
            this.comboBoxSex.Size = new System.Drawing.Size(139, 24);
            this.comboBoxSex.TabIndex = 9;
            // 
            // labelSex
            // 
            this.labelSex.AutoSize = true;
            this.labelSex.Location = new System.Drawing.Point(314, 148);
            this.labelSex.Name = "labelSex";
            this.labelSex.Size = new System.Drawing.Size(36, 16);
            this.labelSex.TabIndex = 8;
            this.labelSex.Text = "Пол:";
            // 
            // dateTimePickerBirthdate
            // 
            this.dateTimePickerBirthdate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePickerBirthdate.Location = new System.Drawing.Point(117, 145);
            this.dateTimePickerBirthdate.Name = "dateTimePickerBirthdate";
            this.dateTimePickerBirthdate.Size = new System.Drawing.Size(180, 22);
            this.dateTimePickerBirthdate.TabIndex = 7;
            // 
            // labelBirthdate
            // 
            this.labelBirthdate.AutoSize = true;
            this.labelBirthdate.Location = new System.Drawing.Point(7, 148);
            this.labelBirthdate.Name = "labelBirthdate";
            this.labelBirthdate.Size = new System.Drawing.Size(109, 16);
            this.labelBirthdate.TabIndex = 6;
            this.labelBirthdate.Text = "Дата рождения:";
            // 
            // textBoxPatronymic
            // 
            this.textBoxPatronymic.Location = new System.Drawing.Point(117, 88);
            this.textBoxPatronymic.Name = "textBoxPatronymic";
            this.textBoxPatronymic.Size = new System.Drawing.Size(287, 22);
            this.textBoxPatronymic.TabIndex = 5;
            // 
            // labelPatronymic
            // 
            this.labelPatronymic.AutoSize = true;
            this.labelPatronymic.Location = new System.Drawing.Point(10, 94);
            this.labelPatronymic.Name = "labelPatronymic";
            this.labelPatronymic.Size = new System.Drawing.Size(73, 16);
            this.labelPatronymic.TabIndex = 4;
            this.labelPatronymic.Text = "Отчество:";
            // 
            // textBoxName
            // 
            this.textBoxName.Location = new System.Drawing.Point(117, 60);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(287, 22);
            this.textBoxName.TabIndex = 3;
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(10, 63);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(36, 16);
            this.labelName.TabIndex = 2;
            this.labelName.Text = "Имя:";
            // 
            // textBoxSurname
            // 
            this.textBoxSurname.Location = new System.Drawing.Point(117, 27);
            this.textBoxSurname.Name = "textBoxSurname";
            this.textBoxSurname.Size = new System.Drawing.Size(287, 22);
            this.textBoxSurname.TabIndex = 1;
            // 
            // labelSurname
            // 
            this.labelSurname.AutoSize = true;
            this.labelSurname.Location = new System.Drawing.Point(10, 33);
            this.labelSurname.Name = "labelSurname";
            this.labelSurname.Size = new System.Drawing.Size(69, 16);
            this.labelSurname.TabIndex = 0;
            this.labelSurname.Text = "Фамилия:";
            // 
            // CheckResult_button
            // 
            this.CheckResult_button.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.CheckResult_button.FlatAppearance.BorderSize = 0;
            this.CheckResult_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CheckResult_button.Location = new System.Drawing.Point(501, 128);
            this.CheckResult_button.Name = "CheckResult_button";
            this.CheckResult_button.Size = new System.Drawing.Size(169, 32);
            this.CheckResult_button.TabIndex = 21;
            this.CheckResult_button.Text = "Проверить результат";
            this.CheckResult_button.UseVisualStyleBackColor = false;
            this.CheckResult_button.Click += new System.EventHandler(this.CheckResult_button_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(705, 429);
            this.Controls.Add(this.CheckResult_button);
            this.Controls.Add(this.groupBoxPatient);
            this.Controls.Add(this.groupBoxOrder);
            this.Controls.Add(this.CreateOrder_button);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Интеграция с ЛИС \"Гемотест\"";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBoxOrder.ResumeLayout(false);
            this.groupBoxOrder.PerformLayout();
            this.groupBoxPatient.ResumeLayout(false);
            this.groupBoxPatient.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem настройкиToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GemotestOptions_toolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SystemOptions_toolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LoadDictionaries_ToolStripMenuItem;
        private System.Windows.Forms.Button CreateOrder_button;
        private System.Windows.Forms.GroupBox groupBoxOrder;
        private System.Windows.Forms.Label labelComment;
        private System.Windows.Forms.TextBox textBoxComment;
        private System.Windows.Forms.Label labelDoctor;
        private System.Windows.Forms.TextBox textBoxDoctor;
        private System.Windows.Forms.Label labelOrderNum;
        private System.Windows.Forms.TextBox textBoxOrderNum;
        private System.Windows.Forms.GroupBox groupBoxPatient;
        private System.Windows.Forms.CheckBox checkBoxAnonymous;
        private System.Windows.Forms.ComboBox comboBoxSex;
        private System.Windows.Forms.Label labelSex;
        private System.Windows.Forms.DateTimePicker dateTimePickerBirthdate;
        private System.Windows.Forms.Label labelBirthdate;
        private System.Windows.Forms.TextBox textBoxPatronymic;
        private System.Windows.Forms.Label labelPatronymic;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.TextBox textBoxSurname;
        private System.Windows.Forms.Label labelSurname;
        private System.Windows.Forms.Button CheckResult_button;
        private System.Windows.Forms.ToolStripMenuItem отладкаКонсольToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem включитьКонсольToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem выключитьКонсольToolStripMenuItem;
    }
}
