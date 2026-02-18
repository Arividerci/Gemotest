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


        public Dictionaries Dicts { get; } = new Dictionaries();
        private Exception last_exception = new Exception("неизвестная ошибка");

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

            if (Dicts.Directory == null || Dicts.Directory.Count == 0)
            {
                bool unpackSuccess = Dicts.Unpack(Gemotest.filePath);
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
             .Select(service => new ProductGemotest(service, "", Dicts))
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

            details.Dicts = Dicts;
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
            details.Dicts = Dicts;
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


        public bool ShowSystemOptions(ref string _SystemOptions)
        {
            Directory.CreateDirectory(BaseDir);

            if (string.IsNullOrWhiteSpace(_SystemOptions))
                _SystemOptions = ReadTextSafe(OptionsFilePath);

            OptionsFormsGemotest optionsSystem = new OptionsFormsGemotest(_SystemOptions);
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

            if (string.IsNullOrWhiteSpace(_LocalOptions))
                _LocalOptions = ReadTextSafe(LocalOptionsFilePath);

            LocalOptionsForm Local_options = new LocalOptionsForm(_LocalOptions);
            if (Local_options.ShowDialog() == DialogResult.OK)
            {
                _LocalOptions = Local_options.Options.Pack();
                WriteTextSafe(OptionsFilePath, _LocalOptions);
                return true;
            }
            return false;
        }

        public void SetOptions(string _SystemOptions, string _LocalOptions)
        {
            Directory.CreateDirectory(BaseDir);

            if (string.IsNullOrWhiteSpace(_LocalOptions))
                _LocalOptions = ReadTextSafe(OptionsFilePath);

            if (string.IsNullOrWhiteSpace(_SystemOptions))
                _SystemOptions = ReadTextSafe(LocalOptionsFilePath);

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
            last_exception = null;
            try
            {
                if (Options == null)
                    return false;

                if (!IsGemotestOptionsValid(Options))
                    return false;

                if (Gemotest == null)
                {
                    Gemotest = new GemotestService(
                        Options.UrlAdress, Options.Login, Options.Password,
                        Options.Contractor, Options.Contractor_Code, Options.Salt);
                }


                if (!Gemotest.all_dictionaries_is_valid())
                {
                    bool ok = Gemotest.get_all_dictionary();
                    if (!ok) return false;
                }


                bool unpackOk = Dicts.Unpack(Gemotest.filePath);
                if (!unpackOk)
                {
                    Console.WriteLine("Ошибка распаковки");
                    return false;

                }
                laboratoryGUI = new LaboratoryGemotestGUI();
                EnsureProductsLoaded();
                AllProducts = GetProducts();

                laboratoryGUI.SetAssignedModules(this, AllProducts, LocalOptions, Options);
                Console.WriteLine("+");


                SiMed.Clinic.Logger.LogEvent.RemoveOldFilesFromLog("Gemotest", 30);
                return true;
            }
            catch (Exception exc)
            {
                last_exception = exc;
                return false;
            }
        }


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

        public Exception GetLastException() { return last_exception; }

        public void BeginTransaction(LaboratoryTransactionType _TransactionType) { }

        public void EndTransaction(LaboratoryTransactionType _TransactionType) { }

        public bool GetNumbersPoolIfNeed(out bool _NumbersPoolChanged, out string _SystemOptionsNew) { _NumbersPoolChanged = false; _SystemOptionsNew = ""; return true; }


    }
}
