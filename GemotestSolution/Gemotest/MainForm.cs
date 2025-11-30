using System;
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

        private LaboratoryGemotest laboratoryGemotest;
        private string SystemOptions = ""; 
        private string LocalOptions = "";
        private Order _currentOrder;
        public OptionsGemotest Options; 

        public MainForm()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)3072;
            InitializeComponent();
            AllocConsole();
            
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

            laboratoryGemotest.Init();
            FreeConsole();
        }

        private void CheckResult_button_Click(object sender, EventArgs e)
        {
            if (_currentOrder == null)
            {
                _currentOrder = new Order(laboratoryGemotest.CreateOrderDetail());
            }

            _currentOrder.Patient.Surname = textBoxSurname.Text;
            _currentOrder.Patient.Name = textBoxName.Text;
            _currentOrder.Patient.Patronimic = textBoxPatronymic.Text;
            _currentOrder.Patient.Birthday = dateTimePickerBirthdate.Value.Date;
            _currentOrder.Patient.Sex = (comboBoxSex.SelectedIndex == 1)
                ? Sex.Female
                : Sex.Male;

            // дальше твой тестовый код добавления услуг — можешь оставить как есть
            _currentOrder.Items.Clear();
            _currentOrder.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[130], 1, 1));
            _currentOrder.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[1328], 1, 1));
            _currentOrder.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[1128], 1, 1));
            _currentOrder.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[1828], 1, 1));
            _currentOrder.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[1938], 1, 1));

            laboratoryGemotest.CreateOrder(_currentOrder);
        }

        private void включитьКонсольToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Пытаемся выделить/подключить консоль к текущему процессу
            AllocConsole();
        }

        private void выключитьКонсольToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Отсоединяемся от консоли
            FreeConsole();
        }

    }
}