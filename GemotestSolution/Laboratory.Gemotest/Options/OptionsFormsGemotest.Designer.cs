namespace Laboratory.Gemotest.Options
{
    partial class OptionsFormsGemotest
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
            this.contractor_label = new System.Windows.Forms.Label();
            this.contractor_textBox = new System.Windows.Forms.TextBox();
            this.password_label = new System.Windows.Forms.Label();
            this.password_textBox = new System.Windows.Forms.TextBox();
            this.contractorCode_label = new System.Windows.Forms.Label();
            this.contractorCode_textBox = new System.Windows.Forms.TextBox();
            this.go_button = new System.Windows.Forms.Button();
            this.key_label = new System.Windows.Forms.Label();
            this.key_textBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // address_textbox
            // 
            this.address_textbox.Location = new System.Drawing.Point(242, 39);
            this.address_textbox.Name = "address_textbox";
            this.address_textbox.Size = new System.Drawing.Size(209, 22);
            this.address_textbox.TabIndex = 0;
            // 
            // address_label
            // 
            this.address_label.AutoSize = true;
            this.address_label.Location = new System.Drawing.Point(69, 42);
            this.address_label.Name = "address_label";
            this.address_label.Size = new System.Drawing.Size(146, 16);
            this.address_label.TabIndex = 1;
            this.address_label.Text = "Url-аддресс Гемотест";
            // 
            // login_label
            // 
            this.login_label.AutoSize = true;
            this.login_label.Location = new System.Drawing.Point(69, 85);
            this.login_label.Name = "login_label";
            this.login_label.Size = new System.Drawing.Size(46, 16);
            this.login_label.TabIndex = 3;
            this.login_label.Text = "Логин";
            // 
            // login_textBox
            // 
            this.login_textBox.Location = new System.Drawing.Point(242, 82);
            this.login_textBox.Name = "login_textBox";
            this.login_textBox.Size = new System.Drawing.Size(209, 22);
            this.login_textBox.TabIndex = 2;
            // 
            // contractor_label
            // 
            this.contractor_label.AutoSize = true;
            this.contractor_label.Location = new System.Drawing.Point(69, 171);
            this.contractor_label.Name = "contractor_label";
            this.contractor_label.Size = new System.Drawing.Size(83, 16);
            this.contractor_label.TabIndex = 7;
            this.contractor_label.Text = "Контрагент";
            // 
            // contractor_textBox
            // 
            this.contractor_textBox.Location = new System.Drawing.Point(242, 168);
            this.contractor_textBox.Name = "contractor_textBox";
            this.contractor_textBox.Size = new System.Drawing.Size(209, 22);
            this.contractor_textBox.TabIndex = 6;
            // 
            // password_label
            // 
            this.password_label.AutoSize = true;
            this.password_label.Location = new System.Drawing.Point(69, 128);
            this.password_label.Name = "password_label";
            this.password_label.Size = new System.Drawing.Size(56, 16);
            this.password_label.TabIndex = 5;
            this.password_label.Text = "Пароль";
            // 
            // password_textBox
            // 
            this.password_textBox.Location = new System.Drawing.Point(242, 125);
            this.password_textBox.Name = "password_textBox";
            this.password_textBox.Size = new System.Drawing.Size(209, 22);
            this.password_textBox.TabIndex = 4;
            // 
            // contractorCode_label
            // 
            this.contractorCode_label.AutoSize = true;
            this.contractorCode_label.Location = new System.Drawing.Point(69, 220);
            this.contractorCode_label.Name = "contractorCode_label";
            this.contractorCode_label.Size = new System.Drawing.Size(118, 16);
            this.contractorCode_label.TabIndex = 9;
            this.contractorCode_label.Text = "Код Контрагента";
            // 
            // contractorCode_textBox
            // 
            this.contractorCode_textBox.Location = new System.Drawing.Point(242, 217);
            this.contractorCode_textBox.Name = "contractorCode_textBox";
            this.contractorCode_textBox.Size = new System.Drawing.Size(108, 22);
            this.contractorCode_textBox.TabIndex = 8;
            // 
            // go_button
            // 
            this.go_button.Location = new System.Drawing.Point(276, 310);
            this.go_button.Name = "go_button";
            this.go_button.Size = new System.Drawing.Size(138, 35);
            this.go_button.TabIndex = 12;
            this.go_button.Text = "Продолжить";
            this.go_button.UseVisualStyleBackColor = true;
            this.go_button.Click += new System.EventHandler(this.go_button_Click);
            // 
            // key_label
            // 
            this.key_label.AutoSize = true;
            this.key_label.Location = new System.Drawing.Point(69, 267);
            this.key_label.Name = "key_label";
            this.key_label.Size = new System.Drawing.Size(39, 16);
            this.key_label.TabIndex = 13;
            this.key_label.Text = "Соль";
            // 
            // key_textBox
            // 
            this.key_textBox.Location = new System.Drawing.Point(242, 261);
            this.key_textBox.Name = "key_textBox";
            this.key_textBox.Size = new System.Drawing.Size(420, 22);
            this.key_textBox.TabIndex = 14;
            // 
            // OptionsFormsGemotest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(703, 498);
            this.Controls.Add(this.key_textBox);
            this.Controls.Add(this.key_label);
            this.Controls.Add(this.go_button);
            this.Controls.Add(this.contractorCode_label);
            this.Controls.Add(this.contractorCode_textBox);
            this.Controls.Add(this.contractor_label);
            this.Controls.Add(this.contractor_textBox);
            this.Controls.Add(this.password_label);
            this.Controls.Add(this.password_textBox);
            this.Controls.Add(this.login_label);
            this.Controls.Add(this.login_textBox);
            this.Controls.Add(this.address_label);
            this.Controls.Add(this.address_textbox);
            this.Name = "OptionsFormsGemotest";
            this.Text = "LocalOptions";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox address_textbox;
        private System.Windows.Forms.Label address_label;
        private System.Windows.Forms.Label login_label;
        private System.Windows.Forms.TextBox login_textBox;
        private System.Windows.Forms.Label contractor_label;
        private System.Windows.Forms.TextBox contractor_textBox;
        private System.Windows.Forms.Label password_label;
        private System.Windows.Forms.TextBox password_textBox;
        private System.Windows.Forms.Label contractorCode_label;
        private System.Windows.Forms.TextBox contractorCode_textBox;
        private System.Windows.Forms.Button go_button;
        private System.Windows.Forms.Label key_label;
        private System.Windows.Forms.TextBox key_textBox;
    }
}