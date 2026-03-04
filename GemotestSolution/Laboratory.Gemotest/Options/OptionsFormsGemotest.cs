using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Laboratory.Gemotest.Options
{
    public partial class OptionsFormsGemotest : Form
    {
        public OptionsGemotest Options { get; private set; }

        private readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Симплекс", "СиМед - Клиника", "GemotestDictionaries", "Options", "options.xml");

        public OptionsFormsGemotest(string options)
        {
            InitializeComponent();

            Options = OptionsGemotest.LoadFromFile(filePath);
            if (!string.IsNullOrEmpty(options))
                Options = (OptionsGemotest)Options.Unpack(options);

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

            check_status_value.Text = "—";
            check_status_value.ForeColor = System.Drawing.Color.DimGray;
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

        private static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");
        }

        private void SetCheckStatus(string text, System.Drawing.Color color, bool enableButton)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => SetCheckStatus(text, color, enableButton)));
                return;
            }

            check_status_value.ForeColor = color;
            check_status_value.Text = text;
            check_button.Enabled = enableButton;
        }

        private void check_button_Click(object sender, EventArgs e)
        {
            check_button.Enabled = false;
            check_status_value.ForeColor = System.Drawing.Color.Black;
            check_status_value.Text = "проверяю…";

            var url = (address_textbox.Text ?? "").Trim();
            var contractor = (contractor_textBox.Text ?? "").Trim();
            var salt = (key_textBox.Text ?? "").Trim();
            var login = (login_textBox.Text ?? "").Trim();
            var password = (password_textBox.Text ?? "");

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(contractor) || string.IsNullOrWhiteSpace(salt))
            {
                SetCheckStatus("не заполнены URL/Контрагент/Соль", System.Drawing.Color.DarkRed, true);
                return;
            }

            // Для простого пинга используем hash = sha1(contractor + salt)
            var hash = Sha1Hex(contractor + salt);

            var soap = string.Format(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<soapenv:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:urn=\"urn:OdoctorControllerwsdl\">\r\n" +
                "  <soapenv:Header/>\r\n" +
                "  <soapenv:Body>\r\n" +
                "    <urn:get_services_supplementals soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">\r\n" +
                "      <params xsi:type=\"urn:request_get_services_supplementals\">\r\n" +
                "        <contractor xsi:type=\"xsd:string\">{0}</contractor>\r\n" +
                "        <hash xsi:type=\"xsd:string\">{1}</hash>\r\n" +
                "      </params>\r\n" +
                "    </urn:get_services_supplementals>\r\n" +
                "  </soapenv:Body>\r\n" +
                "</soapenv:Envelope>",
                EscapeXml(contractor),
                hash);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var r = SoapPing(url, soap, login, password);
                    if (!r.OkFlag)
                    {
                        SetCheckStatus(r.Message, System.Drawing.Color.DarkRed, true);
                        return;
                    }

                    var body = r.Body ?? "";

                    var status = ExtractTag(body, "status");
                    var errCode = ExtractTag(body, "error_code");
                    var errDesc = ExtractTag(body, "error_description");

                    if (string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        SetCheckStatus("OK (accepted)", System.Drawing.Color.DarkGreen, true);
                        return;
                    }

                    var msg = status;
                    if (!string.IsNullOrWhiteSpace(errCode) || !string.IsNullOrWhiteSpace(errDesc))
                        msg = string.Format("{0} (код {1}) {2}", status, errCode, errDesc).Trim();

                    SetCheckStatus(string.IsNullOrWhiteSpace(msg) ? "rejected/unknown" : msg, System.Drawing.Color.DarkRed, true);
                }
                catch (Exception ex)
                {
                    SetCheckStatus(ex.Message, System.Drawing.Color.DarkRed, true);
                }
            });
        }

        private static SoapPingResult SoapPing(string url, string soapBody, string login, string password)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=utf-8";
            req.Accept = "text/xml";
            req.Timeout = 15000;
            req.ReadWriteTimeout = 15000;
            req.Headers.Add("SOAPAction", "urn:OdoctorControllerwsdl#get_services_supplementals");

            // Basic Auth (логин+пароль)
            if (!string.IsNullOrEmpty(login))
            {
                req.PreAuthenticate = true;
                req.Credentials = new NetworkCredential(login, password ?? "");

                // Дублируем заголовком, т.к. PreAuthenticate не всегда срабатывает.
                var token = Convert.ToBase64String(Encoding.ASCII.GetBytes(login + ":" + (password ?? "")));
                req.Headers["Authorization"] = "Basic " + token;
            }

            var bytes = Encoding.UTF8.GetBytes(soapBody ?? "");
            req.ContentLength = bytes.Length;

            using (var rs = req.GetRequestStream())
                rs.Write(bytes, 0, bytes.Length);

            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var s = resp.GetResponseStream())
                using (var sr = new StreamReader(s ?? Stream.Null, Encoding.UTF8))
                {
                    var body = sr.ReadToEnd();

                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                        return SoapPingResult.Ok("OK", body);

                    return SoapPingResult.Fail("HTTP " + (int)resp.StatusCode + ": " + resp.StatusDescription, body);
                }
            }
            catch (WebException wex)
            {
                string body = "";
                var r = wex.Response as HttpWebResponse;

                if (r != null)
                {
                    try
                    {
                        using (var s = r.GetResponseStream())
                        using (var sr = new StreamReader(s ?? Stream.Null, Encoding.UTF8))
                            body = sr.ReadToEnd();
                    }
                    catch { }

                    return SoapPingResult.Fail("HTTP " + (int)r.StatusCode + ": " + r.StatusDescription, body);
                }

                return SoapPingResult.Fail(wex.Message, body);
            }
        }

        private static string ExtractTag(string xml, string localName)
        {
            if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(localName))
                return "";

            // Ищем <localName ...>value</localName> (без namespace)
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

        private sealed class SoapPingResult
        {
            public bool OkFlag;
            public string Message;
            public string Body;

            public static SoapPingResult Ok(string message, string body)
            {
                return new SoapPingResult { OkFlag = true, Message = message, Body = body };
            }

            public static SoapPingResult Fail(string message, string body)
            {
                return new SoapPingResult { OkFlag = false, Message = message, Body = body };
            }
        }
    }
}
