using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Laboratory.Gemotest;
using SiMed.Laboratory;
using StatisticsCollectionSystemClient;

namespace Laboratory.Gemotest.Options
{
    public partial class OptionsFormsGemotest : Form
    {

        public OptionsGemotest Options { get; private set; }
        private readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Симплекс", "СиМед - Клиника", "GemotestDictionaries", "Options", "options.xml");


        public OptionsFormsGemotest(string options)
        {
            InitializeComponent();

            Options = OptionsGemotest.LoadFromFile(filePath);
            if (!string.IsNullOrEmpty(options))
            {
                Options = (OptionsGemotest)Options.Unpack(options);
            }

            LoadOptionsToForm();
        }

        private void LoadOptionsToForm()
        {
            address_textbox.Text = Options.UrlAdress ?? string.Empty;
            login_textBox.Text = Options.Login ?? string.Empty;
            password_textBox.Text = Options.Password ?? string.Empty;
            contractor_textBox.Text = Options.Contractor ?? string.Empty;
            contractorCode_textBox.Text = Options.Contractor_Code ?? string.Empty;
            key_textBox.Text = Options.Salt ?? string.Empty;
        }

        
        private void go_button_Click(object sender, EventArgs e)
        {
            Options.UrlAdress = address_textbox.Text;
            Options.Login = login_textBox.Text;
            Options.Password = password_textBox.Text;
            Options.Contractor = contractor_textBox.Text;
            Options.Contractor_Code = contractorCode_textBox.Text;
            Options.Salt = key_textBox.Text;

            Options.SaveToFile(filePath);


            DialogResult = DialogResult.OK;
            Close();
        }
    }
}