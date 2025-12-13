using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
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
            {
                _currentOrder = new Order(laboratoryGemotest.CreateOrderDetail());
            }

            laboratoryGemotest.CreateOrder(_currentOrder);
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

    }
}