using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Laboratory.Gemotest.Options
{
    public partial class GemotestSystemOptionsForm : Form
    {
        public SystemOptions Options { get; private set; }

        private readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Симплекс", "СиМед - Клиника", "GemotestDictionaries", "Options", "options.xml");

        public GemotestSystemOptionsForm(string options)
        {
            InitializeComponent();

            Options = new SystemOptions();
            Options = (SystemOptions)Options.Unpack(options);
            if (Options == null)
                Options = new SystemOptions();

            PriceList_dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            PriceList_dataGridView.MultiSelect = false;
            PriceList_dataGridView.AllowUserToAddRows = true;
            PriceList_dataGridView.AllowUserToDeleteRows = true;

            LoadOptionsToForm();
        }

        private void LoadOptionsToForm()
        {
            address_textbox.Text = Options.UrlAdress ?? "";
            login_textBox.Text = Options.Login ?? "";
            password_textBox.Text = Options.Password ?? "";
            key_textBox.Text = Options.Salt ?? "";

            LoadPriceListsToGrid();
        }

        private void LoadPriceListsToGrid()
        {
            PriceList_dataGridView.Rows.Clear();

            if (Options.PriceLists != null)
            {
                foreach (var pl in Options.PriceLists)
                {
                    if (pl == null) continue;
                    AddGridRow(pl.Name, pl.ContractorCode, pl.Num ,"");
                }
            }

            if (Options.PriceLists == null || Options.PriceLists.Count == 0)
                return;

            if (Options.PriceLists.Count == 1)
            {
                Options.Contractor = Options.PriceLists[0].Name ?? "";
                Options.Contractor_Code = Options.PriceLists[0].ContractorCode ?? "";
                Options.Numerator = Options.PriceLists[0].Num ?? "1";

                if (PriceList_dataGridView.Rows.Count > 0)
                    PriceList_dataGridView.Rows[0].Selected = true;

                return;
            }

            string selectedCode = (Options.Contractor_Code ?? "").Trim();
            if (!string.IsNullOrEmpty(selectedCode))
            {
                foreach (DataGridViewRow row in PriceList_dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    string code = (row.Cells[1].Value ?? "").ToString().Trim();
                    if (string.Equals(code, selectedCode, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        PriceList_dataGridView.CurrentCell = row.Cells[0];
                        return;
                    }
                }
            }

            PriceList_dataGridView.ClearSelection();
        }

        private void AddGridRow(string contractorName, string contractorCode, string contractorNum, string status)
        {
            int idx = PriceList_dataGridView.Rows.Add();
            var row = PriceList_dataGridView.Rows[idx];
            row.Cells[0].Value = contractorName ?? "";
            row.Cells[1].Value = contractorCode ?? "";
            row.Cells[2].Value = contractorNum ?? "1";
            row.Cells[3].Value = status ?? "";
        }

        private void Exit_button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Save_button_Click(object sender, EventArgs e)
        {
            var list = ReadPriceListsFromGrid();
            if (list.Count == 0)
            {
                MessageBox.Show("Добавь хотя бы один прайс-лист (контрагент + код).", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selected = GetSelectedPriceListRow();
            if (selected == null)
            {
                MessageBox.Show("Выбери одного контрагента в таблице прайс-листов.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selName = (selected.Cells[0].Value ?? "").ToString().Trim();
            var selCode = (selected.Cells[1].Value ?? "").ToString().Trim();
            var selNum = (selected.Cells[2].Value ?? "1").ToString().Trim();

            if (string.IsNullOrEmpty(selCode))
            {
                MessageBox.Show("У выбранного прайс-листа не заполнен код контрагента.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Options.UrlAdress = (address_textbox.Text ?? "").Trim();
            Options.Login = (login_textBox.Text ?? "").Trim();
            Options.Password = (password_textBox.Text ?? "");
            Options.Salt = (key_textBox.Text ?? "").Trim();

            Options.PriceLists = list;
            Options.Contractor = selName;
            Options.Contractor_Code = selCode;
            Options.Numerator = selNum;

            Options.SaveToFile(filePath);

            DialogResult = DialogResult.OK;
            Close();
        }

        private DataGridViewRow GetSelectedPriceListRow()
        {
            if (PriceList_dataGridView.SelectedRows.Count > 0)
            {
                var r = PriceList_dataGridView.SelectedRows[0];
                if (!r.IsNewRow) return r;
            }

            if (PriceList_dataGridView.CurrentRow != null && !PriceList_dataGridView.CurrentRow.IsNewRow)
                return PriceList_dataGridView.CurrentRow;

            return null;
        }

        private List<GemotestPriceList> ReadPriceListsFromGrid()
        {
            var list = new List<GemotestPriceList>();

            foreach (DataGridViewRow row in PriceList_dataGridView.Rows)
            {
                if (row.IsNewRow) continue;

                var name = (row.Cells[0].Value ?? "").ToString().Trim();
                var code = (row.Cells[1].Value ?? "").ToString().Trim();
                var num = (row.Cells[2].Value ?? "1").ToString().Trim();

                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(code))
                    continue;

                if (!string.IsNullOrEmpty(code) && list.Any(x => string.Equals((x.ContractorCode ?? "").Trim(), code, StringComparison.OrdinalIgnoreCase)))
                    continue;

                list.Add(new GemotestPriceList { Name = name, ContractorCode = code , Num = num});
            }

            return list;
        }
        private void CheckConnection_button_Click(object sender, EventArgs e)
        {
            var url = (address_textbox.Text ?? "").Trim();
            var login = (login_textBox.Text ?? "").Trim();
            var pass = (password_textBox.Text ?? "");
            var salt = (key_textBox.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(salt))
            {
                MessageBox.Show("Заполни Url-адрес и Соль.", "Проверка соединения", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CheckConnection_button.Enabled = false;

            foreach (DataGridViewRow row in PriceList_dataGridView.Rows)
            {
                if (row.IsNewRow) continue;
                row.Cells[3].Value = "проверяю…";
                row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    foreach (DataGridViewRow row in PriceList_dataGridView.Rows)
                    {
                        if (row.IsNewRow) continue;

                        var code = (row.Cells[1].Value ?? "").ToString().Trim();
                        if (string.IsNullOrEmpty(code))
                        {
                            SetRowStatus(row, "нет кода", false);
                            continue;
                        }

                        var r = Ping(url, login, pass, code, salt);
                        SetRowStatus(row, r.Message, r.Ok);
                    }
                }
                finally
                {
                    BeginInvoke((Action)delegate { CheckConnection_button.Enabled = true; });
                }
            });
        }

        private void SetRowStatus(DataGridViewRow row, string text, bool ok)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)delegate { SetRowStatus(row, text, ok); });
                return;
            }

            row.Cells[3].Value = text;
            row.DefaultCellStyle.BackColor = ok ? System.Drawing.Color.Honeydew : System.Drawing.Color.MistyRose;
        }

        private sealed class PingResult
        {
            public bool Ok;
            public string Message;
        }

        private static PingResult Ping(string url, string login, string password, string contractorCode, string salt)
        {
            var hash = Sha1Hex(contractorCode + salt);

            var soap = string.Format(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<soapenv:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:OdoctorControllerwsdl\">\r\n" +
                "  <soapenv:Header/>\r\n" +
                "  <soapenv:Body>\r\n" +
                "    <urn:get_services_supplementals>\r\n" +
                "      <params xsi:type=\"urn:request_get_services_supplementals\">\r\n" +
                "        <contractor xsi:type=\"xsd:string\">{0}</contractor>\r\n" +
                "        <hash xsi:type=\"xsd:string\">{1}</hash>\r\n" +
                "      </params>\r\n" +
                "    </urn:get_services_supplementals>\r\n" +
                "  </soapenv:Body>\r\n" +
                "</soapenv:Envelope>",
                EscapeXml(contractorCode),
                EscapeXml(hash)
            );

            try
            {
                string body;
                int http;
                string httpDesc;

                bool okHttp = SoapPost(url, login, password, soap, out body, out http, out httpDesc);
                if (!okHttp)
                {
                    var msg = "HTTP " + http + ": " + httpDesc;
                    return new PingResult { Ok = false, Message = msg };
                }

                var status = ExtractTag(body, "status");
                var errCode = ExtractTag(body, "error_code");
                var errDesc = ExtractTag(body, "error_description");

                if (string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                    return new PingResult { Ok = true, Message = "OK (accepted)" };

                var m = string.IsNullOrEmpty(status) ? "rejected" : status;
                if (!string.IsNullOrEmpty(errCode) || !string.IsNullOrEmpty(errDesc))
                    m = m + " (" + errCode + ") " + errDesc;

                return new PingResult { Ok = false, Message = m.Trim() };
            }
            catch (Exception ex)
            {
                return new PingResult { Ok = false, Message = ex.Message };
            }
        }

        private static bool SoapPost(string url, string login, string password, string soapBody, out string responseBody, out int httpCode, out string httpDesc)
        {
            responseBody = "";
            httpCode = 0;
            httpDesc = "";

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=utf-8";
            req.Accept = "text/xml";
            req.Timeout = 15000;
            req.ReadWriteTimeout = 15000;

            if (!string.IsNullOrEmpty(login))
            {
                req.PreAuthenticate = true;
                req.Credentials = new NetworkCredential(login, password ?? "");
                string token = Convert.ToBase64String(Encoding.ASCII.GetBytes(login + ":" + (password ?? "")));
                req.Headers["Authorization"] = "Basic " + token;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(soapBody ?? "");
            req.ContentLength = bytes.Length;

            using (var rs = req.GetRequestStream())
                rs.Write(bytes, 0, bytes.Length);

            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    httpCode = (int)resp.StatusCode;
                    httpDesc = resp.StatusDescription;
                    using (var s = resp.GetResponseStream())
                    using (var sr = new StreamReader(s ?? Stream.Null, Encoding.UTF8))
                        responseBody = sr.ReadToEnd();
                }

                return httpCode >= 200 && httpCode < 300;
            }
            catch (WebException wex)
            {
                var r = wex.Response as HttpWebResponse;
                if (r != null)
                {
                    httpCode = (int)r.StatusCode;
                    httpDesc = r.StatusDescription;
                    try
                    {
                        using (var s = r.GetResponseStream())
                        using (var sr = new StreamReader(s ?? Stream.Null, Encoding.UTF8))
                            responseBody = sr.ReadToEnd();
                    }
                    catch { }
                }
                else
                {
                    httpCode = 0;
                    httpDesc = wex.Message;
                }
                return false;
            }
        }

        private static string Sha1Hex(string s)
        {
            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(s ?? "");
                var hash = sha1.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static string ExtractTag(string xml, string localName)
        {
            if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(localName))
                return "";

            var open = "<" + localName;
            var close = "</" + localName + ">";

            int i = xml.IndexOf(open, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return "";

            i = xml.IndexOf('>', i);
            if (i < 0) return "";
            i++;

            int j = xml.IndexOf(close, i, StringComparison.OrdinalIgnoreCase);
            if (j < 0) return "";

            return xml.Substring(i, j - i).Trim();
        }

        private static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");
        }

    }
}
