using ContainerMarker.Common;
using Laboratory.Gemotest.GemotestRequests;
using Laboratory.Gemotest.Options;
using SiMed.Clinic;
using SiMed.Laboratory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Laboratory.Gemotest.SourseClass;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static Laboratory.Gemotest.SourseClass.GemotestOrderDetail;

namespace Laboratory.Gemotest
{
    public class LaboratoryGemotest : ILaboratory
    {
        List<DictionaryService> ProductsGemotest;

        public List<ProductGemotest> product;

        public GemotestService Gemotest;
        public ProductsCollection AllProducts {  get; set; }
        public LocalOptionsGemotest LocalOptions { get; set; }
        public OptionsGemotest Options { get; set; }

        private LaboratoryGemotestGUI laboratoryGUI;

        public LaboratoryType GetLaboratoryType()
        {
            return (LaboratoryType)24;
            
        }

        private void EnsureProductsLoaded()
        {
            if (ProductsGemotest != null) return;

            if (Gemotest == null)
            {
                throw new InvalidOperationException("Gemotest не инициализирован. Вызовите SetOptions сначала.");
            }

            if (!Gemotest.all_dictionaries_is_valid())
            {
                Gemotest.get_all_dictionary();
            }

            if (Dictionaries.Directory == null || Dictionaries.Directory.Count == 0)
            {
                bool unpackSuccess = Dictionaries.Unpack(Gemotest.filePath);
                if (!unpackSuccess)
                {
                    Console.WriteLine("Ошибка распаковки справочников.");
                    ProductsGemotest = new List<DictionaryService>();
                    return;
                }

            }

            string dirPath = Path.Combine(Gemotest.filePath, "Directory.xml");
            if (File.Exists(dirPath))
            {
                string dirContent = File.ReadAllText(dirPath);
                ProductsGemotest = DictionaryService.Parse(dirContent);
            }
            else
            {
                ProductsGemotest = new List<DictionaryService>();
            }

            product = ProductsGemotest
             .Where(service =>
                 !service.is_blocked &&
                 service.service_type != 3 &&
                 service.service_type != 4 &&
                 !string.IsNullOrEmpty(service.id) &&
                 !string.IsNullOrEmpty(service.code) &&
                 !string.IsNullOrEmpty(service.name))
             .Select(service => new ProductGemotest(service, ""))
             .ToList();
        }

        public ProductsCollection GetProducts()
        {
            EnsureProductsLoaded();

            ProductsCollection pC = new ProductsCollection();

            foreach (var p in product) 
            {
                if (p.IsBlocked)
                    continue;

                if (p.ServiceType == 3 || p.ServiceType == 4)
                    continue;

                if (string.IsNullOrEmpty(p.ID) || string.IsNullOrEmpty(p.Code) || string.IsNullOrEmpty(p.Name))
                    continue;

                pC.Add(new Product
                {
                    ID = p.ID,
                    Code = p.Code,
                    Name = p.Name
                });
            }

            return pC;
        }


        public Product ChooseProduct(Product _SourceProduct = null) {

            return null; 
        }

        private void PrintInitHeader()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }

            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║        Гемотест: инициализация модуля ЛИС         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");
        }

        private void PrintInitStep(int step, int total, string text)
        {
            string prefix = $"[{step}/{total}]".PadRight(8);
            Console.WriteLine();
            Console.WriteLine($"{prefix}{text}...");
        }

        private void PrintInitOk()
        {
            Console.WriteLine("        → [OK]");
        }

        private void PrintInitWarn(string message)
        {
            Console.WriteLine($"        → [WARN] {message}");
        }

        private void PrintInitFail(string message)
        {
            Console.WriteLine($"        → [FAIL] {message}");
        }

        public BaseOrderDetail CreateOrderDetail() { return new GemotestOrderDetail(); }

        public void FillDefaultOrderDetail(BaseOrderDetail _OrderDetail, OrderItemsCollection _Items)
        {
            var details = (GemotestOrderDetail)_OrderDetail;

            details.Products.Clear();

            int index = 0;
            foreach (var item in _Items)
            {
                var prod = item.Product; 

                details.Products.Add(new GemotestProductDetail
                {
                    OrderProductGuid = index.ToString(),
                    ProductId = prod.ID,      
                    ProductCode = prod.Code,
                    ProductName = prod.Name
                });

                index++;
            }

            details.AddBiomaterialsFromProducts();
        }


        public bool CreateOrder(Order _Order) {

            GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;
            if (details.Products.Count == 0)
            {
                foreach (var item in _Order.Items)
                {
                    details.Products.Add(new GemotestProductDetail()
                    {
                        OrderProductGuid = _Order.Items.IndexOf(item).ToString(),
                        ProductId = item.Product.ID.ToString(),
                        ProductCode = item.Product.Code,
                        ProductName = item.Product.Name
                    });
                }
            }
            details.AddBiomaterialsFromProducts();  
            details.DeleteObsoleteDetails();
            bool readOnly = true;
            ResultsCollection currentResults = new ResultsCollection();
            OrderModelForGUI model = new OrderModelForGUI();

            if (!laboratoryGUI.CreateOrderModelForGUI(readOnly, _Order, ref currentResults, ref model))
            {
                MessageBox.Show(GetLastException().Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            FormLaboratoryOrder form = new FormLaboratoryOrder(_Laboratory: this,
                                                              _LaboratoryGUI: laboratoryGUI,
                                                              _Order: _Order,
                                                              _FormCaption: "Гемотест: оформление заказа",
                                                              _ResultsCollection: ref currentResults,
                                                              _OrderModel: ref model,
                                                              _ReadOnly: readOnly);

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (!readOnly)
                {
                    if (!laboratoryGUI.SaveOrderModelForGUIToDetails(_Order, model))
                        MessageBox.Show($"Ошибка сохранения деталей заказа: {GetLastException().Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return true;
        }

        public bool ShowOrder(Order _Order, bool _bReadOnly, ref ResultsCollection _Results) { return false; }

        public bool SendOrder(Order _Order) { return false; }

        public void PrintOrderForms(Order _Order) {
            ResultsCollection resultsCollection = new ResultsCollection();
            OrderModelForGUI orderModelForGUI = new OrderModelForGUI();
            laboratoryGUI.CreateOrderModelForGUI(true, _Order, ref resultsCollection, ref orderModelForGUI);
        }

        //--------------------------------------------------------------------//--------------------------------------------------------------------

        public bool ShowSystemOptions(ref string _SystemOptions)
        {
            Directory.CreateDirectory(BaseDir);

            // Если строка пустая — подгружаем из файла
            if (string.IsNullOrWhiteSpace(_SystemOptions))
                _SystemOptions = ReadTextSafe(LocalOptionsFilePath);

            LocalOptionsForm optionsSystem = new LocalOptionsForm(_SystemOptions);
            if (optionsSystem.ShowDialog() == DialogResult.OK)
            {
                _SystemOptions = optionsSystem.Options.Pack();
                WriteTextSafe(LocalOptionsFilePath, _SystemOptions);
                return true;
            }
            return false;
        }

        public bool ShowLocalOptions(ref string _LocalOptions)
        {
            Directory.CreateDirectory(BaseDir);

            // Если строка пустая — подгружаем из файла
            if (string.IsNullOrWhiteSpace(_LocalOptions))
                _LocalOptions = ReadTextSafe(OptionsFilePath);

            OptionsFormsGemotest optionsGemotest = new OptionsFormsGemotest(_LocalOptions);
            if (optionsGemotest.ShowDialog() == DialogResult.OK)
            {
                _LocalOptions = optionsGemotest.Options.Pack();
                WriteTextSafe(OptionsFilePath, _LocalOptions);
                return true;
            }
            return false;
        }

        public void SetOptions(string _SystemOptions, string _LocalOptions)
        {
            Directory.CreateDirectory(BaseDir);

            // Если хост прислал пусто — грузим из файлов
            if (string.IsNullOrWhiteSpace(_LocalOptions))
                _LocalOptions = ReadTextSafe(OptionsFilePath);

            if (string.IsNullOrWhiteSpace(_SystemOptions))
                _SystemOptions = ReadTextSafe(LocalOptionsFilePath);

            // Применяем + сохраняем
            if (!string.IsNullOrWhiteSpace(_LocalOptions))
            {
                Options = (OptionsGemotest)new OptionsGemotest().Unpack(_LocalOptions);
                WriteTextSafe(OptionsFilePath, _LocalOptions);
            }
            else
            {
                if (Options == null) Options = new OptionsGemotest();

            }

            if (!string.IsNullOrWhiteSpace(_SystemOptions))
            {
                LocalOptions = (LocalOptionsGemotest)new LocalOptionsGemotest().Unpack(_SystemOptions);
                WriteTextSafe(LocalOptionsFilePath, _SystemOptions);
            }
            else
            {

                if (LocalOptions == null) LocalOptions = new LocalOptionsGemotest();
            }

            if (IsGemotestOptionsValid(Options))
            {
                Gemotest = new GemotestService(
                    Options.UrlAdress, Options.Login, Options.Password,
                    Options.Contractor, Options.Contractor_Code, Options.Salt);
            }
            else
            {
                Gemotest = null;
                Console.WriteLine("Предупреждение: Опции Gemotest неполные. Сервис не инициализирован.");
            }
        }

        public bool Init()
        {
            const int totalSteps = 5;
            int step = 0;

            try
            {
                PrintInitHeader();
                Directory.CreateDirectory(BaseDir);

                step++;
                PrintInitStep(step, totalSteps, "Загрузка/проверка опций Gemotest");

                bool optionsFileExists = File.Exists(OptionsFilePath);
                Options = OptionsGemotest.LoadFromFile(OptionsFilePath);

                // Первый запуск или битые/пустые опции -> открываем форму
                if (!optionsFileExists || !IsGemotestOptionsValid(Options))
                {
                    PrintInitWarn("Опции Gemotest отсутствуют/некорректны — требуется первичная настройка.");

                    string xml = ReadTextSafe(OptionsFilePath);
                    if (!ShowLocalOptions(ref xml))
                    {
                        PrintInitFail("Первичная настройка отменена пользователем.");
                        return false;
                    }

                    Options = (OptionsGemotest)new OptionsGemotest().Unpack(xml);
                    Options.SaveToFile(OptionsFilePath);
                }
                else
                {
                    // Нормализуем файл (после старого Pack мог быть мусор)
                    Options.SaveToFile(OptionsFilePath);
                }

                if (!IsGemotestOptionsValid(Options))
                {
                    PrintInitFail("Опции Gemotest неполные. Сервис не инициализирован.");
                    return false;
                }
                PrintInitOk();

                step++;
                PrintInitStep(step, totalSteps, "Загрузка локальных опций");

                bool localFileExists = File.Exists(LocalOptionsFilePath);
                LocalOptions = LocalOptionsGemotest.LoadFromFile(LocalOptionsFilePath);

                if (!localFileExists)
                {
                    PrintInitWarn("Локальные опции не найдены — создано по умолчанию.");
                    LocalOptions.SaveToFile(LocalOptionsFilePath);
                }
                else
                {
                    // нормализуем
                    LocalOptions.SaveToFile(LocalOptionsFilePath);
                    PrintInitOk();
                }

                step++;
                PrintInitStep(step, totalSteps, "Инициализация сервиса Gemotest");
                Gemotest = new GemotestService(
                    Options.UrlAdress, Options.Login, Options.Password,
                    Options.Contractor, Options.Contractor_Code, Options.Salt);
                PrintInitOk();

                step++;
                PrintInitStep(step, totalSteps, "Загрузка справочников и списка продуктов");
                EnsureProductsLoaded();
                AllProducts = GetProducts();
                if (AllProducts == null || AllProducts.Count == 0)
                {
                    PrintInitFail("Список продуктов пуст. Возможно, справочники не загружены.");
                    return false;
                }
                PrintInitOk();

                step++;
                PrintInitStep(step, totalSteps, "Инициализация GUI-обвязки лаборатории");
                laboratoryGUI = new LaboratoryGemotestGUI();
                laboratoryGUI.GetProducts(AllProducts);
                PrintInitOk();

                Console.WriteLine();
                Console.WriteLine("╔════════════════════════════════════════════════════╗");
                Console.WriteLine("║      Инициализация Гемотест успешно завершена      ║");
                Console.WriteLine("╚════════════════════════════════════════════════════╝");
                Console.WriteLine();

                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine();
                PrintInitFail($"Ошибка инициализации Gemotest: {exc.Message}");
                return false;
            }
        }


        //--------------------------------------------------------------------//--------------------------------------------------------------------

        public void SetNumerator(INumerator _Numerator) { }

        private static readonly string BaseDir =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Симплекс", "СиМед - Клиника", "GemotestDictionaries", "Options");

        private static readonly string OptionsFilePath = Path.Combine(BaseDir, "options.xml");
        private static readonly string LocalOptionsFilePath = Path.Combine(BaseDir, "local_options.xml");

        private static void WriteTextSafe(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, text ?? string.Empty, Encoding.UTF8);
        }

        private static string ReadTextSafe(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsGemotestOptionsValid(OptionsGemotest o)
        {
            return o != null &&
                   !string.IsNullOrWhiteSpace(o.UrlAdress) &&
                   !string.IsNullOrWhiteSpace(o.Login) &&
                   !string.IsNullOrWhiteSpace(o.Password) &&
                   !string.IsNullOrWhiteSpace(o.Contractor) &&
                   !string.IsNullOrWhiteSpace(o.Contractor_Code) &&
                   !string.IsNullOrWhiteSpace(o.Salt);
        }



       
        public bool CheckResult(Order _Order, ref ResultsCollection _Results) { 
            _Results = null;
            return false;
        }

        public bool ExtractResult(Order _Order, ref ResultsCollection _Results) { _Results = null; return false; }

        public bool ExtractContainers(Order _Order, ref ContainersCollection _Containers) { _Containers = null; return false; }

        public void SetContainerMarkerList(List<IContainerMarker> _ContainerMarkerList) { }

        public Exception GetLastException() { return null; }

        public void BeginTransaction(LaboratoryTransactionType _TransactionType) { }

        public void EndTransaction(LaboratoryTransactionType _TransactionType) { }

        public bool GetNumbersPoolIfNeed(out bool _NumbersPoolChanged, out string _SystemOptionsNew) { _NumbersPoolChanged = false; _SystemOptionsNew = ""; return true; }


    }
}