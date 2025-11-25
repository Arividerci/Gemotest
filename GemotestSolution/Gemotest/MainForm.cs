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

        private void button1_Click(object sender, EventArgs e)
        {
            FreeConsole();
            
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            laboratoryGemotest.Init();
        }

        private void AddProduct_button_Click(object sender, EventArgs e)
        {
            Order order = new Order(laboratoryGemotest.CreateOrderDetail()) { Number = "1" };
          
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[3], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[30], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[432], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[528], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[728], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[1528], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[938], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[724], 1, 1));
            order.Items.Add(new OrderItem((SiMed.Laboratory.Product)laboratoryGemotest.AllProducts[1111], 1, 1));
            order.Patient.Surname = "Тестовая";
            order.Patient.Name = "Тестина";
            order.Patient.Patronimic = "Тестовина";
            order.Patient.Birthday = new DateTime(1999, 12, 9);
            order.Patient.Sex = Sex.Female;

            laboratoryGemotest.CreateOrder(order);
        }
    }
}