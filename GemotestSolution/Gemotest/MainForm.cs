using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Laboratory.Gemotest;
using Laboratory.Gemotest.GemotestRequests;
using Laboratory.Gemotest.Options;
using SiMed.Clinic;
using SiMed.Laboratory;
using StatisticsCollectionSystemClient;

namespace Gemotest
{
    public partial class MainForm : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole(); 

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;


        private LaboratoryGemotest laboratoryGemotest;
        private string SystemOptions = ""; 
        private string LocalOptions = "";
        private Order _currentOrder;
        public OptionsGemotest Options; 

        public MainForm()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)3072;
            InitializeComponent();

            laboratoryGemotest = new LaboratoryGemotest();

        }

        private void GemotestOptions_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            string localOptions = LocalOptions;
            if (laboratoryGemotest.ShowLocalOptions(ref localOptions))
            {
                laboratoryGemotest.SetOptions(SystemOptions, localOptions);
                LocalOptions = localOptions;
            }
        }

        private void SystemOptions_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            string systemOptions = SystemOptions;
            if (laboratoryGemotest.ShowSystemOptions(ref systemOptions))
            {
                laboratoryGemotest.SetOptions(systemOptions, LocalOptions); 
                SystemOptions = systemOptions;
                MessageBox.Show("Системные опции сохранены. Теперь можно загрузить продукты.", "Успех");
            }
        }

        private void LoadDictionaries_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (laboratoryGemotest.Gemotest == null)
            {
                MessageBox.Show("Сначала настройте системные опции.");
                return;
            }

            try
            {
                bool success = laboratoryGemotest.Gemotest.get_all_dictionary();
                if (success)
                {
                    Dictionaries.Unpack(laboratoryGemotest.Gemotest.filePath);
                    MessageBox.Show($"Справочники загружены и распарсены. Продуктов: {Dictionaries.Directory.Count}", "Успех");
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки справочников.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            AllocConsole();                
            var ok = laboratoryGemotest.Init();

            if (!ok)
                MessageBox.Show("Ошибка инициализации Гемотест. Подробности были в консоли.");

        }

        private void CreateOrder_button_Click(object sender, EventArgs e)
        {
            if (_currentOrder == null)
                _currentOrder = new Order(laboratoryGemotest.CreateOrderDetail());

            if (!TryBuildOrderForSend(_currentOrder, out var orderForSend, out var error))
            {
                MessageBox.Show(error, "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            laboratoryGemotest.CreateOrder(orderForSend);
        }


        private void включитьКонсольToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AllocConsole();
            var h = GetConsoleWindow();
            if (h != IntPtr.Zero)
                ShowWindow(h, SW_SHOW);
        }

        private void выключитьКонсольToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var h = GetConsoleWindow();
            if (h != IntPtr.Zero)
                ShowWindow(h, SW_HIDE);

            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }

        private void CheckResult_button_Click(object sender, EventArgs e)
        {
            try
            {
                string orderNum = textBoxOrderNum.Text.Trim();

                var opts = laboratoryGemotest.Options; 

                var client = new GemotestAnalysisResultClient(
                    opts.UrlAdress,
                    opts.Contractor_Code,
                    opts.Salt,
                    opts.Login,
                    opts.Password
                );

                string xml = client.GetAnalysisResultRaw(orderNum);

                var parsed = GemotestAnalysisResultParser.Parse(xml);

                var form = new FormGemotestResult(parsed);
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Проверить результат", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static readonly Regex FioRegex = new Regex(@"^[A-Za-zА-Яа-яЁё\-]+$", RegexOptions.Compiled);

        private bool TryBuildOrderForSend(Order baseOrder, out Order orderForSend, out string error)
        {
            error = "";
            orderForSend = null;

            string formOrderNum = (textBoxOrderNum.Text ?? "").Trim();
            string formDoctor = (textBoxDoctor.Text ?? "").Trim();
            string formComment = (textBoxComment.Text ?? "").Trim();

            string formSurname = (textBoxSurname.Text ?? "").Trim();
            string formName = (textBoxName.Text ?? "").Trim();
            string formPatronymic = (textBoxPatronymic.Text ?? "").Trim();

            DateTime formBirthdate = dateTimePickerBirthdate.Value.Date;
            Sex? formSex = MapSexFromCombo(comboBoxSex.SelectedIndex);

            bool anonymous = checkBoxAnonymous.Checked;

            if (!ValidateForm(anonymous, formSurname, formName, formPatronymic, formBirthdate, out error))
                return false;

            orderForSend = new Order(baseOrder.OrderDetail);

            TryCopyStringProp(baseOrder, orderForSend, "Number");
            TryCopyStringProp(baseOrder, orderForSend, "Doctor");
            TryCopyStringProp(baseOrder, orderForSend, "Comment");

            TryCopyProp(baseOrder, orderForSend, "Items");

            TrySetStringIfDifferent(orderForSend, "Number", formOrderNum);
            TrySetStringIfDifferent(orderForSend, "Doctor", formDoctor);
            TrySetStringIfDifferent(orderForSend, "Comment", formComment);

            var p = orderForSend.Patient;
            if (p == null)
            {
                error = "Order.Patient == null. В текущей модели Order пациент не создаётся автоматически. " +
                        "Нужно, чтобы Order создавал Patient в конструкторе или имел метод инициализации пациента.";
                return false;
            }

            if (anonymous)
            {
                if (string.IsNullOrWhiteSpace(formSurname)) formSurname = "Аноним";
                if (string.IsNullOrWhiteSpace(formName)) formName = "Пациент";
            }

            ApplyPatientOverrides(p, formSurname, formName, formPatronymic, formBirthdate, formSex);

            if (string.IsNullOrWhiteSpace(p.Surname) || string.IsNullOrWhiteSpace(p.Name))
            {
                error = "Для отправки должны быть заполнены Фамилия и Имя пациента (или включи «Анонимный»).";
                return false;
            }

            return true;
        }

        private static bool ValidateForm(bool anonymous, string surname, string name, string patronymic, DateTime birthdate, out string error)
        {
            error = "";

            if (birthdate > DateTime.Today)
            {
                error = "Дата рождения не может быть в будущем.";
                return false;
            }

            if (!anonymous)
            {
                if (string.IsNullOrWhiteSpace(surname))
                {
                    error = "Заполни «Фамилия» (или включи «Анонимный»).";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(name))
                {
                    error = "Заполни «Имя» (или включи «Анонимный»).";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(surname) && !IsValidFioToken(surname))
            {
                error = "Фамилия: допускаются только буквы и дефис. Пробелы/цифры/символы запрещены.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(name) && !IsValidFioToken(name))
            {
                error = "Имя: допускаются только буквы и дефис. Пробелы/цифры/символы запрещены.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(patronymic) && !IsValidFioToken(patronymic))
            {
                error = "Отчество: допускаются только буквы и дефис. Пробелы/цифры/символы запрещены.";
                return false;
            }

            return true;
        }

        private static bool IsValidFioToken(string s)
        {
            s = (s ?? "").Trim().Replace(" ", "");
            return s.Length > 0 && FioRegex.IsMatch(s);
        }

        private static void ApplyPatientOverrides(Patient p, string surname, string name, string patronymic, DateTime birthdate, Sex? sex)
        {
            if (!string.IsNullOrWhiteSpace(surname))
            {
                var v = surname.Trim().Replace(" ", "");
                if (!string.Equals(p.Surname ?? "", v, StringComparison.Ordinal))
                    p.Surname = v;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var v = name.Trim().Replace(" ", "");
                if (!string.Equals(p.Name ?? "", v, StringComparison.Ordinal))
                    p.Name = v;
            }

            if (!string.IsNullOrWhiteSpace(patronymic))
            {
                var v = patronymic.Trim().Replace(" ", "");
                if (!string.Equals(p.Patronimic ?? "", v, StringComparison.Ordinal))
                    p.Patronimic = v;
            }

            if (p.Birthday.Date != birthdate.Date)
                p.Birthday = birthdate.Date;

            if (sex.HasValue && !Equals(p.Sex, sex.Value))
                p.Sex = sex.Value;
        }

        private static Sex? MapSexFromCombo(int selectedIndex)
        {
            if (selectedIndex <= 0) return null;
            try
            {
                if (Enum.IsDefined(typeof(Sex), selectedIndex))
                    return (Sex)selectedIndex;
            }
            catch { }
            return null;
        }

        private static void TrySetStringIfDifferent(object obj, string propName, string value)
        {
            if (obj == null) return;
            if (string.IsNullOrWhiteSpace(value)) return;

            var p = obj.GetType().GetProperty(propName);
            if (p == null || !p.CanWrite || p.PropertyType != typeof(string)) return;

            string v = value.Trim();
            var cur = p.CanRead ? (p.GetValue(obj, null) as string) : null;

            if (!string.Equals(cur ?? "", v, StringComparison.Ordinal))
                p.SetValue(obj, v, null);
        }

        private static void TryCopyStringProp(object src, object dst, string propName)
        {
            try
            {
                var p = src.GetType().GetProperty(propName);
                if (p == null || !p.CanRead) return;

                var val = p.GetValue(src, null) as string;
                TrySetStringIfDifferent(dst, propName, val);
            }
            catch { }
        }

        private static void TryCopyProp(object src, object dst, string propName)
        {
            try
            {
                var p = src.GetType().GetProperty(propName);
                if (p == null || !p.CanRead || !p.CanWrite) return;

                var val = p.GetValue(src, null);
                p.SetValue(dst, val, null);
            }
            catch { }
        }

    }
}