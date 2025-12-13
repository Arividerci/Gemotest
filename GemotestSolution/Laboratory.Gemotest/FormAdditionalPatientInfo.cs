using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SiMed.Laboratory;

namespace Laboratory.Gemotest
{
    public partial class FormAdditionalPatientInfo : Form
    {
        private readonly Order _order;
        private readonly bool _needPassport;
        private readonly bool _needAddress;
        private readonly bool _needSnils;

        public FormAdditionalPatientInfo(Order order, bool needPassport, bool needAddress, bool needSnils)
        {
            InitializeComponent();

            _order = order ?? throw new ArgumentNullException(nameof(order));
            _needPassport = needPassport;
            _needAddress = needAddress;
            _needSnils = needSnils;

            groupBoxAddress.Visible = needAddress;
            groupBoxAddress.Enabled = needAddress;

            groupBoxPassport.Visible = needPassport;
            groupBoxPassport.Enabled = needPassport;

            groupBoxSnils.Visible = needSnils;
            groupBoxSnils.Enabled = needSnils;

            BindFromOrder();
        }

        private void BindFromOrder()
        {
            var patient = _order.Patient;
            if (patient == null)
                return;

            if (_needSnils)
                textBoxSnils.Text = patient.SNILS ?? string.Empty;

            textBoxCity.Text = string.Empty;
            textBoxAddress.Text = string.Empty;
            textBoxActualAddress.Text = string.Empty;
            textBoxRepresentativeActualAddress.Text = string.Empty;
            textBoxRepresentativeRegion.Text = string.Empty;

            textBoxPassport.Text = string.Empty;
            textBoxPassportIssuedBy.Text = string.Empty;
            dateTimePassportIssued.Value = DateTime.Today;
        }

        private void ApplyToOrder()
        {
            var order = _order;
            if (order == null)
                return;

            var patient = order.Patient;
            if (patient == null)
                return;

            var additional = new List<string>();
            string[] informing = {"", ""};

            if (_needAddress)
            {
                var city = textBoxCity.Text?.Trim();
                var address = textBoxAddress.Text?.Trim();
                var actualAddr = textBoxActualAddress.Text?.Trim();
                var repActual = textBoxRepresentativeActualAddress.Text?.Trim();
                var repRegion = textBoxRepresentativeRegion.Text?.Trim();
                var phone = textBoxPhone.Text?.Trim();
                var mail = textBoxMail.Text?.Trim();

                if (!string.IsNullOrEmpty(city))
                    additional.Add($"city={city}");
                if (!string.IsNullOrEmpty(address))
                    additional.Add($"address={address}");
                if (!string.IsNullOrEmpty(actualAddr))
                    additional.Add($"actual_address={actualAddr}");
                if (!string.IsNullOrEmpty(repActual))
                    additional.Add($"representative_actual_address={repActual}");
                if (!string.IsNullOrEmpty(repRegion))
                    additional.Add($"representative_region={repRegion}");
                if (!string.IsNullOrEmpty(phone))
                    informing[0] = phone;
                if (!string.IsNullOrEmpty(mail))
                    informing[1] = mail;
            }

            if (_needPassport)
            {
                var passport = textBoxPassport.Text?.Trim();
                var issuedBy = textBoxPassportIssuedBy.Text?.Trim();
                var issuedDate = dateTimePassportIssued.Value.Date;

                if (!string.IsNullOrEmpty(passport))
                    additional.Add($"passport={passport}");

                additional.Add($"passport_issued={issuedDate:yyyy-MM-dd}");

                if (!string.IsNullOrEmpty(issuedBy))
                    additional.Add($"passport_issued_by={issuedBy}");
            }

            if (_needSnils)
            {
                var snils = textBoxSnils.Text?.Trim();
                patient.SNILS = snils;

                if (!string.IsNullOrEmpty(snils))
                    additional.Add($"snils={snils}");
            }

            if (additional.Count > 0)
            {
                var line = string.Join("; ", additional);

                if (string.IsNullOrEmpty(order.AuthorInformation))
                    order.AuthorInformation = line;
                else
                    order.AuthorInformation += Environment.NewLine + line;
            }

            if (string.IsNullOrEmpty(order.Patient.Phone))
                order.Patient.Phone = informing[0];
            if (string.IsNullOrEmpty(order.Patient.EMail))
                order.Patient.EMail = informing[1];
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            ApplyToOrder();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
