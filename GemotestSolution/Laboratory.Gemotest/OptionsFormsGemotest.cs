using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Laboratory.Gemotest.Options
{
    public partial class OptionsFormsGemotest : Form
    {
        public Laboratory.Gemotest.OptionsGemotest Options { get; private set; }

        public OptionsFormsGemotest(string packedOptions)
        {
            InitializeComponent();

            // Как в Bregislab: unpack через новый объект, null -> new
            Options = new Laboratory.Gemotest.OptionsGemotest();
            if (!string.IsNullOrWhiteSpace(packedOptions))
            {
                var u = (Laboratory.Gemotest.OptionsGemotest)new Laboratory.Gemotest.OptionsGemotest().Unpack(packedOptions);
                if (u != null) Options = u;
            }

            // кнопки Add/Delete могут быть без обработчиков в Designer
            var bAdd = FindControlRecursive<Button>("Add_button");
            if (bAdd != null) bAdd.Click += Add_button_Click;

            var bDel = FindControlRecursive<Button>("Delite_button");
            if (bDel != null) bDel.Click += Delite_button_Click;

            LoadOptionsToForm();
        }

        private void LoadOptionsToForm()
        {
            SetText("address_textbox", Options.UrlAdress);
            SetText("login_textBox", Options.Login);
            SetText("password_textBox", Options.Password);
            SetText("key_textBox", Options.Salt);

            var grid = GetPriceListGrid();
            if (grid != null)
            {
                grid.Rows.Clear();

                if (Options.PriceLists == null)
                    Options.PriceLists = new List<Laboratory.Gemotest.GemotestPriceList>();

                foreach (var pl in Options.PriceLists)
                {
                    if (pl == null) continue;
                    int r = grid.Rows.Add();
                    SetGridCell(grid, r, "Name", 0, pl.Name);
                    SetGridCell(grid, r, "Code", 1, pl.ContractorCode);
                    SetGridCell(grid, r, "StatusConection", 2, "");
                }

                // Требование: если прайсов несколько — показываем "неопределенный" и не выделяем автоматически.
                if (Options.PriceLists.Count == 1)
                {
                    Options.Contractor = Options.PriceLists[0].Name;
                    Options.Contractor_Code = Options.PriceLists[0].ContractorCode;
                    TrySelectRowByContractorCode(grid, Options.Contractor_Code);
                }
                else
                {
                    Options.Contractor = "";
                    Options.Contractor_Code = "";
                    grid.ClearSelection();
                    grid.CurrentCell = null;
                }

                UpdatePriceListHint(grid);
            }
            else
            {
                // fallback для старой формы (если остались contractor_textBox/contractorCode_textBox)
                SetText("contractor_textBox", Options.Contractor);
                SetText("contractorCode_textBox", Options.Contractor_Code);
            }

            SetCheckStatus("-", Color.DimGray, true);
        }

        // ====== Save (OK) ======
        private void go_button_Click(object sender, EventArgs e)
        {
            SaveFormToOptions();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Exit_button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // иногда в старых Designer кнопка отмены называется button1
        private void button1_Click(object sender, EventArgs e)
        {
            Exit_button_Click(sender, e);
        }

        private void SaveFormToOptions()
        {
            Options.UrlAdress = GetText("address_textbox");
            Options.Login = GetText("login_textBox");
            Options.Password = GetText("password_textBox", trim: false);
            Options.Salt = GetText("key_textBox");

            var grid = GetPriceListGrid();
            if (grid != null)
            {
                // сохранить весь список
                var list = new List<Laboratory.Gemotest.GemotestPriceList>();

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    string name = GetGridCell(row, "Name", 0).Trim();
                    string code = GetGridCell(row, "Code", 1).Trim();

                    if (name.Length == 0 && code.Length == 0)
                        continue;

                    list.Add(new Laboratory.Gemotest.GemotestPriceList(name, code));
                }

                Options.PriceLists = list;

                // выбранный прайс: если один — автозаполнить; если несколько — брать выделенную строку
                if (Options.PriceLists.Count == 1)
                {
                    Options.Contractor = Options.PriceLists[0].Name;
                    Options.Contractor_Code = Options.PriceLists[0].ContractorCode;
                }
                else
                {
                    var sel = GetSelectedPriceListFromGrid(grid);
                    if (sel != null)
                    {
                        Options.Contractor = sel.Name;
                        Options.Contractor_Code = sel.ContractorCode;
                    }
                    else
                    {
                        // неопределенный
                        Options.Contractor = "";
                        Options.Contractor_Code = "";
                    }
                }

                UpdatePriceListHint(grid);
            }
            else
            {
                Options.Contractor = GetText("contractor_textBox");
                Options.Contractor_Code = GetText("contractorCode_textBox");
            }
        }

        // ====== Add / Delete ======
        private void Add_button_Click(object sender, EventArgs e)
        {
            var grid = GetPriceListGrid();
            if (grid == null) return;

            int r = grid.Rows.Add();
            grid.ClearSelection();
            if (r >= 0 && r < grid.Rows.Count)
            {
                grid.Rows[r].Selected = true;
                if (grid.Columns.Count > 0)
                    grid.CurrentCell = grid.Rows[r].Cells[0];
            }

            UpdatePriceListHint(grid);
        }

        private void Delite_button_Click(object sender, EventArgs e)
        {
            var grid = GetPriceListGrid();
            if (grid == null) return;

            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                if (!row.IsNewRow)
                    grid.Rows.Remove(row);
            }

            grid.ClearSelection();
            grid.CurrentCell = null;

            UpdatePriceListHint(grid);
        }

        // ====== Check connection ======
        private void CheckConnection_button_Click(object sender, EventArgs e)
        {
            CheckConnectionsAll();
        }

        // старое имя из другого Designer
        private void check_button_Click(object sender, EventArgs e)
        {
            CheckConnectionsAll();
        }

        private void CheckConnectionsAll()
        {
            SaveFormToOptions();

            string url = (Options.UrlAdress ?? "").Trim();
            string login = (Options.Login ?? "").Trim();
            string password = Options.Password ?? "";
            string salt = (Options.Salt ?? "").Trim();

            if (url.Length == 0 || salt.Length == 0)
            {
                SetCheckStatus("не заполнены URL/Соль", Color.DarkRed, true);
                return;
            }

            var grid = GetPriceListGrid();
            if (grid == null)
            {
                // fallback на единичного контрагента
                string contractorCode = (Options.Contractor_Code ?? "").Trim();
                if (contractorCode.Length == 0)
                {
                    SetCheckStatus("не заполнен код контрагента", Color.DarkRed, true);
                    return;
                }

                ThreadPool.QueueUserWorkItem(delegate
                {
                    string err;
                    bool ok = GemotestPing(url, login, password, contractorCode, salt, out err);
                    SetCheckStatus(ok ? "Успешно!" : ("Ошибка: " + err), ok ? Color.DarkGreen : Color.DarkRed, true);
                });

                return;
            }

            SetCheckStatus("проверяю…", Color.Black, false);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string code = GetGridCell(row, "Code", 1).Trim();
                        if (code.Length == 0)
                        {
                            SetGridStatusSafe(grid, row.Index, "Ошибка: пустой код", Color.DarkRed);
                            continue;
                        }

                        SetGridStatusSafe(grid, row.Index, "[Проверка соединения]", Color.DimGray);

                        string err;
                        bool ok = GemotestPing(url, login, password, code, salt, out err);
                        SetGridStatusSafe(grid, row.Index, ok ? "Успешно!" : ("Ошибка: " + err), ok ? Color.DarkGreen : Color.DarkRed);
                    }

                    SetCheckStatus("готово", Color.DimGray, true);
                }
                catch (Exception ex)
                {
                    SetCheckStatus(ex.Message, Color.DarkRed, true);
                }
            });
        }

        // ====== SOAP ping: get_services_supplementals ======
        private static bool GemotestPing(string url, string login, string password, string contractorCode, string salt, out string message)
        {
            message = "";

            string hash = Sha1Hex(contractorCode + salt);
            string soap = BuildGetServicesSupplementalsSoap(contractorCode, hash);

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "text/xml; charset=utf-8";
                req.Accept = "text/xml";
                req.Timeout = 15000;
                req.ReadWriteTimeout = 15000;
                req.KeepAlive = false;

                // BASIC AUTH
                if (!string.IsNullOrEmpty(login))
                {
                    req.PreAuthenticate = true;
                    req.Credentials = new NetworkCredential(login, password ?? "");
                    string token = Convert.ToBase64String(Encoding.ASCII.GetBytes(login + ":" + (password ?? "")));
                    req.Headers["Authorization"] = "Basic " + token;
                }

                byte[] bytes = Encoding.UTF8.GetBytes(soap);
                req.ContentLength = bytes.Length;

                using (var rs = req.GetRequestStream())
                    rs.Write(bytes, 0, bytes.Length);

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    int code = (int)resp.StatusCode;
                    if (code >= 200 && code < 300)
                        return true;

                    message = "HTTP " + code + ": " + resp.StatusDescription;
                    return false;
                }
            }
            catch (WebException wex)
            {
                var r = wex.Response as HttpWebResponse;
                if (r != null)
                    message = "HTTP " + (int)r.StatusCode + ": " + r.StatusDescription;
                else
                    message = wex.Message;

                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        private static string BuildGetServicesSupplementalsSoap(string contractor, string hash)
        {
            return
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<soapenv:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" " +
                "xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                "xmlns:urn=\"urn:OdoctorControllerwsdl\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<urn:get_services_supplementals soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                "<params xsi:type=\"urn:request_get_services_supplementals\">" +
                "<contractor xsi:type=\"xsd:string\">" + EscapeXml(contractor) + "</contractor>" +
                "<hash xsi:type=\"xsd:string\">" + EscapeXml(hash) + "</hash>" +
                "</params>" +
                "</urn:get_services_supplementals>" +
                "</soapenv:Body>" +
                "</soapenv:Envelope>";
        }

        private static string Sha1Hex(string s)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(s ?? "");
                byte[] hash = sha1.ComputeHash(bytes);
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

        // ====== UI helpers ======
        private void SetCheckStatus(string text, Color color, bool enableButtons)
        {
            var lbl = FindControlRecursive<Label>("check_status_value");
            var btn = FindControlRecursive<Button>("check_button") ?? FindControlRecursive<Button>("CheckConnection_button");

            if (lbl != null)
            {
                if (lbl.InvokeRequired)
                {
                    lbl.BeginInvoke((Action)(() => SetCheckStatus(text, color, enableButtons)));
                    return;
                }
                lbl.ForeColor = color;
                lbl.Text = text;
            }

            if (btn != null)
                btn.Enabled = enableButtons;
        }

        private void UpdatePriceListHint(DataGridView grid)
        {
            var label = FindControlRecursive<Label>("label6");
            if (label == null) return;

            string baseText = "Прайс листы (учетные данные)";

            if (Options.PriceLists == null || Options.PriceLists.Count == 0)
            {
                label.Text = baseText + " — не добавлены";
                return;
            }

            if (Options.PriceLists.Count > 1 && string.IsNullOrWhiteSpace(Options.Contractor_Code))
            {
                label.Text = baseText + " — прайс-лист: НЕОПРЕДЕЛЕН";
                return;
            }

            if (!string.IsNullOrWhiteSpace(Options.Contractor_Code))
                label.Text = baseText + " — выбран: " + (Options.Contractor ?? Options.Contractor_Code);
            else
                label.Text = baseText;
        }

        private static void SetGridCell(DataGridView grid, int rowIndex, string colName, int colIndexFallback, string value)
        {
            if (grid == null) return;
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;

            DataGridViewCell cell = null;
            if (grid.Columns.Contains(colName))
                cell = grid.Rows[rowIndex].Cells[colName];
            else if (colIndexFallback >= 0 && colIndexFallback < grid.Columns.Count)
                cell = grid.Rows[rowIndex].Cells[colIndexFallback];

            if (cell != null)
                cell.Value = value ?? "";
        }

        private static string GetGridCell(DataGridViewRow row, string colName, int colIndexFallback)
        {
            if (row == null) return "";
            DataGridViewCell cell = null;
            if (row.DataGridView != null && row.DataGridView.Columns.Contains(colName))
                cell = row.Cells[colName];
            else if (colIndexFallback >= 0 && colIndexFallback < row.Cells.Count)
                cell = row.Cells[colIndexFallback];

            return Convert.ToString(cell != null ? cell.Value : "") ?? "";
        }

        private void SetGridStatusSafe(DataGridView grid, int rowIndex, string status, Color color)
        {
            if (grid == null) return;

            if (grid.InvokeRequired)
            {
                grid.BeginInvoke((Action)(() => SetGridStatusSafe(grid, rowIndex, status, color)));
                return;
            }

            SetGridCell(grid, rowIndex, "StatusConection", 2, status);

            try
            {
                DataGridViewCell cell = null;
                if (grid.Columns.Contains("StatusConection"))
                    cell = grid.Rows[rowIndex].Cells["StatusConection"];
                else if (grid.Columns.Count > 2)
                    cell = grid.Rows[rowIndex].Cells[2];

                if (cell != null)
                    cell.Style.ForeColor = color;
            }
            catch { }
        }

        private static Laboratory.Gemotest.GemotestPriceList GetSelectedPriceListFromGrid(DataGridView grid)
        {
            if (grid == null) return null;
            if (grid.SelectedRows == null || grid.SelectedRows.Count == 0) return null;

            var row = grid.SelectedRows[0];
            if (row.IsNewRow) return null;

            string name = GetGridCell(row, "Name", 0).Trim();
            string code = GetGridCell(row, "Code", 1).Trim();

            if (name.Length == 0 || code.Length == 0) return null;
            return new Laboratory.Gemotest.GemotestPriceList(name, code);
        }

        private static void TrySelectRowByContractorCode(DataGridView grid, string contractorCode)
        {
            if (grid == null) return;
            if (string.IsNullOrWhiteSpace(contractorCode)) return;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string code = GetGridCell(row, "Code", 1).Trim();
                if (string.Equals(code, contractorCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    if (grid.Columns.Count > 0)
                        grid.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        private DataGridView GetPriceListGrid()
        {
            return FindControlRecursive<DataGridView>("PriceList_dataGridView");
        }

        private string GetText(string controlName, bool trim = true)
        {
            var tb = FindControlRecursive<TextBox>(controlName);
            if (tb == null) return "";
            return trim ? ((tb.Text ?? "").Trim()) : (tb.Text ?? "");
        }

        private void SetText(string controlName, string value)
        {
            var tb = FindControlRecursive<TextBox>(controlName);
            if (tb != null) tb.Text = value ?? "";
        }

        private T FindControlRecursive<T>(string name) where T : Control
        {
            return FindControlRecursive<T>(this, name);
        }

        private static T FindControlRecursive<T>(Control parent, string name) where T : Control
        {
            if (parent == null) return null;

            foreach (Control c in parent.Controls)
            {
                if (c == null) continue;
                if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) && c is T)
                    return (T)c;

                var child = FindControlRecursive<T>(c, name);
                if (child != null) return child;
            }

            return null;
        }
    }
}
