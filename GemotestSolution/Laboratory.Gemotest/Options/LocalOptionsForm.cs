using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Laboratory.Gemotest.Options
{
    public partial class LocalOptionsForm : Form
    {
        public LocalOptionsGemotest Options { get; private set; }

        public LocalOptionsForm(string localOptions)
        {
            InitializeComponent();
            Options = new LocalOptionsGemotest();
            if (!string.IsNullOrEmpty(localOptions))
            {
                Options = (LocalOptionsGemotest)Options.Unpack(localOptions);
                if (Options == null)
                    Options = new LocalOptionsGemotest();
                else
                {
                    if (Options.PdfPrinterSettings != null)
                    {
                        printer1Label.Text = Options.PdfPrinterSettings.PrinterName;
                        Options.PdfPrinterSettings.DefaultPageSettings.PaperSource = Options.PaperSource;
                        receiverLabel.Text = Options.PdfPrinterSettings.DefaultPageSettings.PaperSource.SourceName;
                    }
                    if (Options.StickerPrinterSettings != null)
                        printer2Label.Text = Options.StickerPrinterSettings.PrinterName;
                    chbPrintatOnce.Checked = Options.PrintAtOnce;
                    chbPrintStinkersAtOnce.Checked = Options.PrintStikersAtOnce;
                    chbPrintBlankAtOnce.Checked = Options.PrintBlankAtOnce;
                }
                switch (Options.LabelEncoding)
                {
                    case LabelEncoding.UTF8:
                        rdbtnLabelEncoding1.Checked = true;
                        rdbtnLabelEncoding2.Checked = false;
                        break;
                    case LabelEncoding.Code866:
                        rdbtnLabelEncoding1.Checked = false;
                        rdbtnLabelEncoding2.Checked = true;
                        break;
                }
                switch (Options.LabelType)
                {
                    case LabelType.ZPL:
                        txtCustomLabel.Text = LocalOptionsGemotest.GetDefaultLabelTemplate(LabelType.ZPL);
                        break;
                    case LabelType.EPL:
                        txtCustomLabel.Text = LocalOptionsGemotest.GetDefaultLabelTemplate(LabelType.EPL);
                        break;
                    case LabelType.Custom:
                        txtCustomLabel.Text = Options.CustomLabelTemplate.Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n", "\r\n");
                        break;
                }
            }
        }

        private void btPdfPrinter_Click(object sender, EventArgs e)
        {
            PrintDialog pD = new PrintDialog();
            if (pD.ShowDialog() == DialogResult.OK)
            {
                Options.PdfPrinterSettings = pD.PrinterSettings;
                Options.PaperSource = pD.PrinterSettings.DefaultPageSettings.PaperSource;
                printer1Label.Text = pD.PrinterSettings.PrinterName;
                receiverLabel.Text = pD.PrinterSettings.DefaultPageSettings.PaperSource.SourceName;
            }
        }

        private void btStickerPrinter_Click(object sender, EventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            pd.ShowDialog();
            Options.StickerPrinterSettings = pd.PrinterSettings;
            printer2Label.Text = pd.PrinterSettings.PrinterName;
        }

        private void chbPrintatOnce_CheckedChanged(object sender, EventArgs e)
        {
            Options.PrintAtOnce = chbPrintatOnce.Checked;
        }

        private void btClearPdfPrinter_Click(object sender, EventArgs e)
        {
            Options.PdfPrinterSettings = null;
            receiverLabel.Text = "-";
            printer1Label.Text = "Принтер по умолчанию";
        }

        private void btClearStickerPrinter_Click(object sender, EventArgs e)
        {
            Options.StickerPrinterSettings = null;
            printer2Label.Text = "Принтер по умолчанию";
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            if (rdbtnLabelEncoding1.Checked)
                Options.LabelEncoding = LabelEncoding.UTF8;
            else if (rdbtnLabelEncoding2.Checked)
                Options.LabelEncoding = LabelEncoding.Code866;
            Options.PrintStikersAtOnce = chbPrintStinkersAtOnce.Checked;
            Options.PrintBlankAtOnce = chbPrintBlankAtOnce.Checked;
            Options.LabelType = LabelType.Custom;
            Options.CustomLabelTemplate = txtCustomLabel.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void bSetDefault_ZPL_Click(object sender, EventArgs e)
        {
            txtCustomLabel.Text = LocalOptionsGemotest.GetDefaultLabelTemplate(LabelType.ZPL);
        }

        private void bSetDefault_EPL_Click(object sender, EventArgs e)
        {
            txtCustomLabel.Text = LocalOptionsGemotest.GetDefaultLabelTemplate(LabelType.EPL);
        }

        private void bRefrechLabelTemplate_Click(object sender, EventArgs e)
        {
            switch (Options.LabelType)
            {
                case LabelType.ZPL:
                    txtCustomLabel.Text = LocalOptionsGemotest.GetDefaultLabelTemplate(LabelType.ZPL);
                    break;
                case LabelType.EPL:
                    txtCustomLabel.Text = LocalOptionsGemotest.GetDefaultLabelTemplate(LabelType.EPL);
                    break;
                case LabelType.Custom:
                    txtCustomLabel.Text = Options.CustomLabelTemplate.Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n", "\r\n");
                    break;
            }
        }

        private void bTestPrint_Click(object sender, EventArgs e)
        {
            try
            {
                Encoding encoding = Encoding.UTF8;
                if (rdbtnLabelEncoding1.Checked)
                    encoding = Encoding.UTF8;
                else if (rdbtnLabelEncoding2.Checked)
                    encoding = Encoding.GetEncoding(866);
                PrinterSettings settings;
                if (Options != null && Options.StickerPrinterSettings != null)
                    settings = Options.StickerPrinterSettings;
                else
                    settings = PrintCommon.Print.GetDefaultPrinterSettingsAccorgingToFormat();
                string LabelTemplate = txtCustomLabel.Text;
                string str_to_print = string.Format(LabelTemplate, "ЛПУ-1111", "Биоматериал", "111111111111", "Иванов Иван");
                PrintCommon.Print.SendStringToPrinter(settings, str_to_print, encoding);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}