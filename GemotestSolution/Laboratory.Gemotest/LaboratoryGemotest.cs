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
            return LaboratoryType.None;
            
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
                // Распаковка всех справочников, если Directory пустой
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
                .Where(service => !service.is_blocked && !string.IsNullOrEmpty(service.id) && !string.IsNullOrEmpty(service.code) && !string.IsNullOrEmpty(service.name))
                .Select(service => new ProductGemotest(service, ""))
                .ToList();

            ProductGemotest.PrintAllProductsRelatedData(product);

        }

        public ProductsCollection GetProducts()
        {
            EnsureProductsLoaded();

            ProductsCollection pC = new ProductsCollection();

            foreach (var product in ProductsGemotest)
            {
                if (String.IsNullOrEmpty(product.id) || String.IsNullOrEmpty(product.code) || String.IsNullOrEmpty(product.name) || product.is_blocked)
                    continue;
                pC.Add(new Product { ID = product.id, Code = product.code, Name = product.name });
        
            }
            Gemotest.PrintS();
            return pC;
        }

        public Product ChooseProduct(Product _SourceProduct = null) {

            return null; 
        }

        public BaseOrderDetail CreateOrderDetail() { return new GemotestOrderDetail(); }

        public void FillDefaultOrderDetail(BaseOrderDetail _OrderDetail, OrderItemsCollection _Items) { }

        public bool CreateOrder(Order _Order) {

            if (_Order.Items.Count == 0)
            {
                MessageBox.Show("Не выбрано ни одной услуги для формирования заказа!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
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
            LocalOptionsForm optionsSystem = new LocalOptionsForm(_SystemOptions);
            if (optionsSystem.ShowDialog() == DialogResult.OK)
            {
                _SystemOptions = optionsSystem.Options.Pack();
                return true;
            }
            return false;
        }

        public bool ShowLocalOptions(ref string _LocalOptions)
        {
            OptionsFormsGemotest optionsGemotest = new OptionsFormsGemotest(_LocalOptions);
            if (optionsGemotest.ShowDialog() == DialogResult.OK)
            {
                _LocalOptions = optionsGemotest.Options.Pack();
                return true;
            }
            return false;
        }

        public void SetOptions(string _SystemOptions, string _LocalOptions)
        {
            
            Options = (OptionsGemotest)new OptionsGemotest().Unpack(_LocalOptions);
            LocalOptions = (LocalOptionsGemotest)new LocalOptionsGemotest().Unpack(_SystemOptions);

            Console.WriteLine(Options.UrlAdress);
            if (!string.IsNullOrEmpty(Options.UrlAdress) && !string.IsNullOrEmpty(Options.Login) &&
                !string.IsNullOrEmpty(Options.Password) && !string.IsNullOrEmpty(Options.Contractor) &&
                !string.IsNullOrEmpty(Options.Contractor_Code) && !string.IsNullOrEmpty(Options.Salt))
            {
                Gemotest = new GemotestService(Options.UrlAdress, Options.Login, Options.Password,
                                               Options.Contractor, Options.Contractor_Code, Options.Salt);
            }
            else
            {
                Gemotest = null;
                Console.WriteLine("Предупреждение: Опции Gemotest неполные. Сервис не инициализирован.");
            }
        }

        //--------------------------------------------------------------------//--------------------------------------------------------------------

        public void SetNumerator(INumerator _Numerator) { }

        private const string OptionsFilePath = "options.xml";
        private const string LocalOptionsFilePath = "local_options.xml"; 

        public bool Init()
        {
            try
            {
                if (Options == null)
                {
                    Options = OptionsGemotest.LoadFromFile(OptionsFilePath);
                }

                if (LocalOptions == null)
                {
                    
                    LocalOptions = new LocalOptionsGemotest(); 
                }

                if (Gemotest == null && Options != null)
                {
                    if (!string.IsNullOrEmpty(Options.UrlAdress) && !string.IsNullOrEmpty(Options.Login) &&
                        !string.IsNullOrEmpty(Options.Password) && !string.IsNullOrEmpty(Options.Contractor) &&
                        !string.IsNullOrEmpty(Options.Contractor_Code) && !string.IsNullOrEmpty(Options.Salt))
                    {
                        Gemotest = new GemotestService(Options.UrlAdress, Options.Login, Options.Password,
                                                       Options.Contractor, Options.Contractor_Code, Options.Salt);
                    }
                    else
                    {
                        Console.WriteLine("Предупреждение: Опции Gemotest неполные. Сервис не инициализирован.");
                        return false;
                    }
                }

                if (Gemotest == null)
                {
                    return false;
                }

                EnsureProductsLoaded();
                AllProducts = GetProducts();
                laboratoryGUI = new LaboratoryGemotestGUI();
                laboratoryGUI.GetProducts(AllProducts);
                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Ошибка инициализации Gemotest: {exc.Message}");
                return false;
            }
        }

        public bool CheckResult(Order _Order, ref ResultsCollection _Results) { _Results = null; return false; }

        public bool ExtractResult(Order _Order, ref ResultsCollection _Results) { _Results = null; return false; }

        public bool ExtractContainers(Order _Order, ref ContainersCollection _Containers) { _Containers = null; return false; }

        public void SetContainerMarkerList(List<IContainerMarker> _ContainerMarkerList) { }

        public Exception GetLastException() { return null; }

        public void BeginTransaction(LaboratoryTransactionType _TransactionType) { }

        public void EndTransaction(LaboratoryTransactionType _TransactionType) { }

        public bool GetNumbersPoolIfNeed(out bool _NumbersPoolChanged, out string _SystemOptionsNew) { _NumbersPoolChanged = false; _SystemOptionsNew = ""; return true; }


    }
}