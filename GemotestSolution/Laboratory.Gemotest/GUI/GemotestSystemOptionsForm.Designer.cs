namespace Laboratory.Gemotest.Options
{
    partial class GemotestSystemOptionsForm
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
            this.address_textbox = new System.Windows.Forms.TextBox();
            this.address_label = new System.Windows.Forms.Label();
            this.login_label = new System.Windows.Forms.Label();
            this.login_textBox = new System.Windows.Forms.TextBox();
            this.password_label = new System.Windows.Forms.Label();
            this.password_textBox = new System.Windows.Forms.TextBox();
            this.go_button = new System.Windows.Forms.Button();
            this.key_label = new System.Windows.Forms.Label();
            this.key_textBox = new System.Windows.Forms.TextBox();
            this.connectionGroupBox = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.PriceList_dataGridView = new System.Windows.Forms.DataGridView();
            this.CheckConnection_button = new System.Windows.Forms.Button();
            this.Exit_button = new System.Windows.Forms.Button();
            this.Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Code = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Num = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StatusConection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.connectionGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PriceList_dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // address_textbox
            // 
            this.address_textbox.Location = new System.Drawing.Point(215, 35);
            this.address_textbox.Name = "address_textbox";
            this.address_textbox.Size = new System.Drawing.Size(410, 27);
            this.address_textbox.TabIndex = 1;
            // 
            // address_label
            // 
            this.address_label.AutoSize = true;
            this.address_label.Location = new System.Drawing.Point(22, 38);
            this.address_label.Name = "address_label";
            this.address_label.Size = new System.Drawing.Size(140, 20);
            this.address_label.TabIndex = 0;
            this.address_label.Text = "Url-адрес Гемотест";
            // 
            // login_label
            // 
            this.login_label.AutoSize = true;
            this.login_label.Location = new System.Drawing.Point(22, 78);
            this.login_label.Name = "login_label";
            this.login_label.Size = new System.Drawing.Size(52, 20);
            this.login_label.TabIndex = 2;
            this.login_label.Text = "Логин";
            // 
            // login_textBox
            // 
            this.login_textBox.Location = new System.Drawing.Point(215, 75);
            this.login_textBox.Name = "login_textBox";
            this.login_textBox.Size = new System.Drawing.Size(410, 27);
            this.login_textBox.TabIndex = 3;
            // 
            // password_label
            // 
            this.password_label.AutoSize = true;
            this.password_label.Location = new System.Drawing.Point(22, 118);
            this.password_label.Name = "password_label";
            this.password_label.Size = new System.Drawing.Size(62, 20);
            this.password_label.TabIndex = 4;
            this.password_label.Text = "Пароль";
            // 
            // password_textBox
            // 
            this.password_textBox.Location = new System.Drawing.Point(215, 115);
            this.password_textBox.Name = "password_textBox";
            this.password_textBox.Size = new System.Drawing.Size(410, 27);
            this.password_textBox.TabIndex = 5;
            // 
            // go_button
            // 
            this.go_button.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.go_button.Location = new System.Drawing.Point(503, 454);
            this.go_button.Name = "go_button";
            this.go_button.Size = new System.Drawing.Size(110, 35);
            this.go_button.TabIndex = 2;
            this.go_button.Text = "ОК";
            this.go_button.UseVisualStyleBackColor = true;
            this.go_button.Click += new System.EventHandler(this.Save_button_Click);
            // 
            // key_label
            // 
            this.key_label.AutoSize = true;
            this.key_label.Location = new System.Drawing.Point(22, 163);
            this.key_label.Name = "key_label";
            this.key_label.Size = new System.Drawing.Size(43, 20);
            this.key_label.TabIndex = 10;
            this.key_label.Text = "Соль";
            // 
            // key_textBox
            // 
            this.key_textBox.Location = new System.Drawing.Point(215, 163);
            this.key_textBox.Name = "key_textBox";
            this.key_textBox.Size = new System.Drawing.Size(410, 27);
            this.key_textBox.TabIndex = 11;
            // 
            // connectionGroupBox
            // 
            this.connectionGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionGroupBox.Controls.Add(this.label6);
            this.connectionGroupBox.Controls.Add(this.address_label);
            this.connectionGroupBox.Controls.Add(this.PriceList_dataGridView);
            this.connectionGroupBox.Controls.Add(this.address_textbox);
            this.connectionGroupBox.Controls.Add(this.login_label);
            this.connectionGroupBox.Controls.Add(this.login_textBox);
            this.connectionGroupBox.Controls.Add(this.password_label);
            this.connectionGroupBox.Controls.Add(this.password_textBox);
            this.connectionGroupBox.Controls.Add(this.key_label);
            this.connectionGroupBox.Controls.Add(this.key_textBox);
            this.connectionGroupBox.Location = new System.Drawing.Point(24, 12);
            this.connectionGroupBox.Name = "connectionGroupBox";
            this.connectionGroupBox.Size = new System.Drawing.Size(799, 426);
            this.connectionGroupBox.TabIndex = 1;
            this.connectionGroupBox.TabStop = false;
            this.connectionGroupBox.Text = "Параметры подключения";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 209);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(226, 20);
            this.label6.TabIndex = 45;
            this.label6.Text = "Прайс листы (учетные данные)";
            // 
            // PriceList_dataGridView
            // 
            this.PriceList_dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.PriceList_dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Name,
            this.Code,
            this.Num,
            this.StatusConection});
            this.PriceList_dataGridView.Location = new System.Drawing.Point(10, 232);
            this.PriceList_dataGridView.Name = "PriceList_dataGridView";
            this.PriceList_dataGridView.RowHeadersWidth = 51;
            this.PriceList_dataGridView.RowTemplate.Height = 24;
            this.PriceList_dataGridView.Size = new System.Drawing.Size(778, 123);
            this.PriceList_dataGridView.TabIndex = 3;
            // 
            // CheckConnection_button
            // 
            this.CheckConnection_button.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.CheckConnection_button.Location = new System.Drawing.Point(99, 454);
            this.CheckConnection_button.Name = "CheckConnection_button";
            this.CheckConnection_button.Size = new System.Drawing.Size(194, 35);
            this.CheckConnection_button.TabIndex = 4;
            this.CheckConnection_button.Text = " Проверка соединения";
            this.CheckConnection_button.UseVisualStyleBackColor = true;
            this.CheckConnection_button.Click += new System.EventHandler(this.CheckConnection_button_Click);
            // 
            // Exit_button
            // 
            this.Exit_button.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.Exit_button.Location = new System.Drawing.Point(619, 454);
            this.Exit_button.Name = "Exit_button";
            this.Exit_button.Size = new System.Drawing.Size(110, 35);
            this.Exit_button.TabIndex = 5;
            this.Exit_button.Text = "Отмена";
            this.Exit_button.UseVisualStyleBackColor = true;
            this.Exit_button.Click += new System.EventHandler(this.Exit_button_Click);
            // 
            // Name
            // 
            this.Name.HeaderText = "Наименование контрагент";
            this.Name.MinimumWidth = 6;
            this.Name.Name = "Name";
            this.Name.Width = 250;
            // 
            // Code
            // 
            this.Code.HeaderText = "Код контрагента";
            this.Code.MinimumWidth = 6;
            this.Code.Name = "Code";
            this.Code.Width = 150;
            // 
            // Num
            // 
            this.Num.HeaderText = "Начальная нумерация заказа";
            this.Num.MinimumWidth = 6;
            this.Num.Name = "Num";
            this.Num.Width = 125;
            // 
            // StatusConection
            // 
            this.StatusConection.HeaderText = "Статус соединения";
            this.StatusConection.MinimumWidth = 6;
            this.StatusConection.Name = "StatusConection";
            this.StatusConection.Width = 200;
            // 
            // GemotestSystemOptionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(847, 549);
            this.Controls.Add(this.Exit_button);
            this.Controls.Add(this.CheckConnection_button);
            this.Controls.Add(this.go_button);
            this.Controls.Add(this.connectionGroupBox);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Опции ЛИС «Гемотест»";
            this.connectionGroupBox.ResumeLayout(false);
            this.connectionGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PriceList_dataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox address_textbox;
        private System.Windows.Forms.Label address_label;
        private System.Windows.Forms.Label login_label;
        private System.Windows.Forms.TextBox login_textBox;
        private System.Windows.Forms.Label password_label;
        private System.Windows.Forms.TextBox password_textBox;
        private System.Windows.Forms.Button go_button;
        private System.Windows.Forms.Label key_label;
        private System.Windows.Forms.TextBox key_textBox;
        private System.Windows.Forms.GroupBox connectionGroupBox;
        private System.Windows.Forms.DataGridView PriceList_dataGridView;
        private System.Windows.Forms.Button CheckConnection_button;
        private System.Windows.Forms.Button Exit_button;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DataGridViewTextBoxColumn Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn Code;
        private System.Windows.Forms.DataGridViewTextBoxColumn Num;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusConection;
    }
}
