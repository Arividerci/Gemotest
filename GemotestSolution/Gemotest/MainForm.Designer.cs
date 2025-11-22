namespace Gemotest
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
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.настройкиToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GemotestOptions_toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SystemOptions_toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LoadDictionaries_ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.AddProduct_button = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
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
            this.menuStrip1.Size = new System.Drawing.Size(451, 30);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // настройкиToolStripMenuItem
            // 
            this.настройкиToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GemotestOptions_toolStripMenuItem,
            this.SystemOptions_toolStripMenuItem,
            this.LoadDictionaries_ToolStripMenuItem});
            this.настройкиToolStripMenuItem.Name = "настройкиToolStripMenuItem";
            this.настройкиToolStripMenuItem.Size = new System.Drawing.Size(98, 26);
            this.настройкиToolStripMenuItem.Text = "Настройки";
            // 
            // GemotestOptions_toolStripMenuItem
            // 
            this.GemotestOptions_toolStripMenuItem.Name = "GemotestOptions_toolStripMenuItem";
            this.GemotestOptions_toolStripMenuItem.Size = new System.Drawing.Size(291, 26);
            this.GemotestOptions_toolStripMenuItem.Text = "Параметры аутентификации";
            this.GemotestOptions_toolStripMenuItem.Click += new System.EventHandler(this.GemotestOptions_toolStripMenuItem_Click);
            // 
            // SystemOptions_toolStripMenuItem
            // 
            this.SystemOptions_toolStripMenuItem.Name = "SystemOptions_toolStripMenuItem";
            this.SystemOptions_toolStripMenuItem.Size = new System.Drawing.Size(291, 26);
            this.SystemOptions_toolStripMenuItem.Text = "Параметы принтера";
            this.SystemOptions_toolStripMenuItem.Click += new System.EventHandler(this.SystemOptions_toolStripMenuItem_Click);
            // 
            // LoadDictionaries_ToolStripMenuItem
            // 
            this.LoadDictionaries_ToolStripMenuItem.Name = "LoadDictionaries_ToolStripMenuItem";
            this.LoadDictionaries_ToolStripMenuItem.Size = new System.Drawing.Size(291, 26);
            this.LoadDictionaries_ToolStripMenuItem.Text = "Загрузить справочники";
            this.LoadDictionaries_ToolStripMenuItem.Click += new System.EventHandler(this.LoadDictionaries_ToolStripMenuItem_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(306, 60);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(95, 32);
            this.button1.TabIndex = 5;
            this.button1.Text = "GO";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // AddProduct_button
            // 
            this.AddProduct_button.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.AddProduct_button.FlatAppearance.BorderSize = 0;
            this.AddProduct_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AddProduct_button.Location = new System.Drawing.Point(50, 60);
            this.AddProduct_button.Name = "AddProduct_button";
            this.AddProduct_button.Size = new System.Drawing.Size(149, 32);
            this.AddProduct_button.TabIndex = 6;
            this.AddProduct_button.Text = "Выбрать услугу";
            this.AddProduct_button.UseVisualStyleBackColor = false;
            this.AddProduct_button.Click += new System.EventHandler(this.AddProduct_button_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(451, 145);
            this.Controls.Add(this.AddProduct_button);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Интеграция с ЛИС \"Гемотест\"";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem настройкиToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GemotestOptions_toolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SystemOptions_toolStripMenuItem;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ToolStripMenuItem LoadDictionaries_ToolStripMenuItem;
        private System.Windows.Forms.Button AddProduct_button;
    }
}

