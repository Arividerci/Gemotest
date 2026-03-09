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
                    Name = p.Name,
                    Duration = p.Duration
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
            ApplyPriceListToDetails(details);
            details.AddBiomaterialsFromProducts();
        }

        private void ApplyPriceListToDetails(GemotestOrderDetail details)
        {
            if (details == null)
                return;

            if (!string.IsNullOrWhiteSpace(details.PriceListCode))
            {
                if (Options != null && Options.PriceLists != null && Options.PriceLists.Count > 0)
                {
                    var existing = Options.PriceLists.FirstOrDefault(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(x.ContractorCode) &&
                        string.Equals(x.ContractorCode.Trim(), details.PriceListCode.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        details.PriceList = existing.Name ?? details.PriceList ?? "";
                        details.PriceListName = existing.Name ?? details.PriceListName ?? "";
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(details.PriceListName))
                            details.PriceListName = details.PriceList ?? "";
                    }
                }

                return;
            }

            if (Options != null && Options.PriceLists != null && Options.PriceLists.Count == 1)
            {
                var pl = Options.PriceLists[0];
                details.PriceList = pl.Name ?? "";
                details.PriceListName = pl.Name ?? "";
                details.PriceListCode = pl.ContractorCode ?? "";
                return;
            }

            if (Options != null && Options.PriceLists != null && Options.PriceLists.Count > 1)
            {
                details.PriceList = details.PriceList ?? "";
                details.PriceListName = details.PriceListName ?? "";
                details.PriceListCode = details.PriceListCode ?? "";
                return;
            }

            var code = Options != null ? (Options.Contractor_Code ?? "") : "";
            var name = Options != null ? (Options.Contractor ?? "") : "";

            details.PriceList = name;
            details.PriceListName = name;
            details.PriceListCode = code;
        }

        public bool CreateOrder(Order _Order)
        {
            var status = _Order.State;

            if (laboratoryGUI == null)
            {
                if (!Init())
                {
                    MessageBox.Show("Ошибка инициализации модуля Гемотест", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;
            if (details == null)
            {
                last_exception = new Exception("OrderDetail не задан (ожидался GemotestOrderDetail).");
                MessageBox.Show(last_exception.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // В CreateOrder НЕ отправляем заказ: только переносим выбранные услуги в OrderDetail и открываем GUI.
            // Канонический источник услуг для дальнейшей работы (биоматериалы/образцы/поля) — details.Products.
            if (details.Products == null)
                details.Products = new List<GemotestOrderDetail.GemotestProductDetail>();

            if (details.Products.Count == 0)
            {
                FillDefaultOrderDetail(details, _Order.Items);
            }
            else
            {
                details.Dicts = Dicts;
                ApplyPriceListToDetails(details);
                details.AddBiomaterialsFromProducts();
            }

            details.DeleteObsoleteDetails();

            bool readOnly = _Order.State != OrderState.NotSended;

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

            var res = form.ShowDialog();

            if (res == DialogResult.OK)
            {
                if (!readOnly)
                {
                    if (!laboratoryGUI.SaveOrderModelForGUIToDetails(_Order, model))
                        MessageBox.Show($"Ошибка сохранения деталей заказа: {GetLastException().Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Поведение как в примере Bregislab: true только если ОК или поменялся State.
            if (_Order.State != status || res == DialogResult.OK)
                return true;

            return false;
        }

        public bool ShowOrder(Order _Order, bool _bReadOnly, ref ResultsCollection _Results) { return false; }

        public bool SendOrder(Order _Order)
        {
            last_exception = null;
            try
            {
                if (_Order == null)
                    throw new InvalidOperationException("Заказ не задан.");

                if (_Order.State != OrderState.Prepared)
                {
                    var msg = $"Попытка отправки заказа. Заказ должен быть в состоянии {OrderState.Prepared}, а сейчас { _Order.State }.";
                    last_exception = new Exception(msg);
                    SiMed.Clinic.Logger.LogEvent.SaveErrorToLog(msg, "Gemotest");
                    return false;
                }

                if (!IsGemotestOptionsValid(Options))
                    throw new InvalidOperationException("Опции Gemotest не заполнены (Url/Login/Password/Contractor_Code/Salt).");

                var details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                if (details.Products == null || details.Products.Count == 0)
                    throw new InvalidOperationException("В заказе нет ни одной услуги (details.Products пуст).");

                var contractorCode = !string.IsNullOrEmpty(details.PriceListCode) ? details.PriceListCode : Options.Contractor_Code;

                var sender = new GemotestOrderSender(
                    Options.UrlAdress,
                    contractorCode,
                    Options.Salt,
                    Options.Login,
                    Options.Password
                );

                string errorMessage;
                if (!sender.CreateOrder(_Order, out errorMessage))
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        last_exception = new Exception($"Ошибка отправки заказа в Гемотест: {errorMessage}");
                    else
                        last_exception = new Exception("Ошибка отправки заказа в Гемотест (без текста ошибки)");

                    SiMed.Clinic.Logger.LogEvent.SaveErrorToLog(last_exception.Message, "Gemotest");
                    return false;
                }

                _Order.State = OrderState.Commited;
                return true;
            }
            catch (Exception ex)
            {
                last_exception = ex;
                SiMed.Clinic.Logger.LogEvent.SaveErrorToLog(ex.Message, "Gemotest");
                return false;
            }
        }

        public void PrintOrderForms(Order _Order) {
            ResultsCollection resultsCollection = new ResultsCollection();
            OrderModelForGUI orderModelForGUI = new OrderModelForGUI();
            laboratoryGUI.CreateOrderModelForGUI(true, _Order, ref resultsCollection, ref orderModelForGUI);
        }


        public bool ShowSystemOptions(ref string _SystemOptions)
        {
            OptionsFormsGemotest optionsSystem = new OptionsFormsGemotest(_SystemOptions);
            if (optionsSystem.ShowDialog() == DialogResult.OK)
            {
                _SystemOptions = optionsSystem.Options.Pack();
                return true;
            }
            return false;
        }

        public bool ShowLocalOptions(ref string _LocalOptions)
        {
            LocalOptionsForm Local_options = new LocalOptionsForm(_LocalOptions);
            if (Local_options.ShowDialog() == DialogResult.OK)
            {
                _LocalOptions = Local_options.Options.Pack();
                return true;
            }
            return false;
        }

        public void SetOptions(string _SystemOptions, string _LocalOptions)
        {
            if (!string.IsNullOrWhiteSpace(_SystemOptions))
            {
                Options = (OptionsGemotest)new OptionsGemotest().Unpack(_SystemOptions);
            }
            else
            {
                if (Options == null) Options = new OptionsGemotest();

            }

            if (!string.IsNullOrWhiteSpace(_LocalOptions))
            {
                LocalOptions = (LocalOptionsGemotest)new LocalOptionsGemotest().Unpack(_LocalOptions);
            }
            else
            {

                if (LocalOptions == null) LocalOptions = new LocalOptionsGemotest();
            }

            if (IsGemotestOptionsValid(Options))
            {
                string initContractorName;
                string initContractorCode;
                ResolveContractorForServiceInit(Options, out initContractorName, out initContractorCode);

                Gemotest = new GemotestService(
                    Options.UrlAdress, Options.Login, Options.Password,
                    initContractorName, initContractorCode, Options.Salt);
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

                bool inited = false;
                foreach (var pl in GetInitCandidates())
                {
                    if (TryInitWithPriceList(pl))
                    {
                        inited = true;
                        break;
                    }
                }

                if (!inited)
                    return false;

                ProductsGemotest = null;
                product = null;
                laboratoryGUI = new LaboratoryGemotestGUI();

                EnsureProductsLoaded();
                AllProducts = GetProducts();

                laboratoryGUI.SetAssignedModules(this, AllProducts, LocalOptions, Options);

                SiMed.Clinic.Logger.LogEvent.RemoveOldFilesFromLog("Gemotest", 30);
                return true;
            }
            catch (Exception exc)
            {
                last_exception = exc;
                return false;
            }
        }

        private static readonly string[] DictionaryFiles = new string[]
        {
            "Biomaterials.xml",
            "Transport.xml",
            "Localization.xml",
            "Service_group.xml",
            "Service_parameters.xml",
            "Directory.xml",
            "Tests.xml",
            "Samples_services.xml",
            "Samples.xml",
            "Processing_rules.xml",
            "Marketing_complex_composition.xml",
            "Services_group_analogs.xml",
            "Service_auto_insert.xml",
            "Services_supplementals.xml"
        };

        private bool RefreshDictionariesAtInit()
        {
            if (Gemotest == null)
                return false;

            string root = Gemotest.filePath;

            bool oldLoaded = false;
            try { oldLoaded = Dicts.Unpack(root); } catch { oldLoaded = false; }

            string backupDir = Path.Combine(root, "_backup");

            try
            {
                BackupDictionaryFiles(root, backupDir);
                ForceDictionaryFilesOutdated(root, 2);

                bool downloaded = Gemotest.get_all_dictionary();

                if (downloaded)
                {
                    bool unpackOk = Dicts.Unpack(root);
                    if (unpackOk)
                    {
                        DeleteDirectorySafe(backupDir);
                        return true;
                    }
                }

                RestoreDictionaryFiles(root, backupDir);

                bool restoredOk = Dicts.Unpack(root);
                if (restoredOk)
                    return true;

                return oldLoaded;
            }
            catch (Exception ex)
            {
                last_exception = ex;
                try
                {
                    RestoreDictionaryFiles(root, backupDir);
                    if (Dicts.Unpack(root))
                        return true;
                }
                catch { }

                return oldLoaded;
            }
        }

        private static void BackupDictionaryFiles(string root, string backupDir)
        {
            Directory.CreateDirectory(backupDir);

            foreach (string name in DictionaryFiles)
            {
                string src = Path.Combine(root, name);
                if (!File.Exists(src)) continue;

                string dst = Path.Combine(backupDir, name);
                File.Copy(src, dst, true);
            }
        }

        private static void RestoreDictionaryFiles(string root, string backupDir)
        {
            if (!Directory.Exists(backupDir))
                return;

            foreach (string name in DictionaryFiles)
            {
                string src = Path.Combine(backupDir, name);
                if (!File.Exists(src)) continue;

                string dst = Path.Combine(root, name);
                File.Copy(src, dst, true);
            }
        }

        private static void ForceDictionaryFilesOutdated(string root, int daysBack)
        {
            DateTime ts = DateTime.Now.AddDays(-Math.Abs(daysBack));

            foreach (string name in DictionaryFiles)
            {
                string f = Path.Combine(root, name);
                if (!File.Exists(f)) continue;

                try { File.SetLastWriteTime(f, ts); } catch { }
            }
        }

        private static void DeleteDirectorySafe(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch { }
        }

        public void SetNumerator(INumerator _Numerator) { }

        private static bool IsGemotestOptionsValid(OptionsGemotest o)
        {
            return o != null &&
                   !string.IsNullOrWhiteSpace(o.UrlAdress) &&
                   !string.IsNullOrWhiteSpace(o.Login) &&
                   !string.IsNullOrWhiteSpace(o.Password) &&
                   !string.IsNullOrWhiteSpace(o.Salt) &&
                   (
                       !string.IsNullOrWhiteSpace(o.Contractor_Code) ||
                       (o.PriceLists != null && o.PriceLists.Any(x => x != null && !string.IsNullOrWhiteSpace(x.ContractorCode)))
                   );
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

        private static bool HasConfiguredPriceLists(OptionsGemotest o)
        {
            return o != null &&
                   o.PriceLists != null &&
                   o.PriceLists.Any(x => x != null && !string.IsNullOrWhiteSpace(x.ContractorCode));
        }

        private IEnumerable<GemotestPriceList> GetInitCandidates()
        {
            var result = new List<GemotestPriceList>();

            // 1. Сначала текущий выбранный в настройках
            if (Options != null && !string.IsNullOrWhiteSpace(Options.Contractor_Code))
            {
                result.Add(new GemotestPriceList
                {
                    ContractorCode = Options.Contractor_Code ?? "",
                    Name = Options.Contractor ?? ""
                });
            }

            // 2. Потом все остальные прайсы
            if (Options != null && Options.PriceLists != null)
            {
                foreach (var pl in Options.PriceLists)
                {
                    if (pl == null || string.IsNullOrWhiteSpace(pl.ContractorCode))
                        continue;

                    if (result.Any(x => string.Equals(x.ContractorCode, pl.ContractorCode, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    result.Add(new GemotestPriceList
                    {
                        ContractorCode = pl.ContractorCode ?? "",
                        Name = pl.Name ?? ""
                    });
                }
            }

            return result;
        }

        private bool TryInitWithPriceList(GemotestPriceList pl)
        {
            if (pl == null || string.IsNullOrWhiteSpace(pl.ContractorCode))
                return false;

            try
            {
                Gemotest = new GemotestService(
                    Options.UrlAdress,
                    Options.Login,
                    Options.Password,
                    pl.Name ?? "",
                    pl.ContractorCode ?? "",
                    Options.Salt
                );

                if (!RefreshDictionariesAtInit())
                    return false;

                Options.Contractor = pl.Name ?? "";
                Options.Contractor_Code = pl.ContractorCode ?? "";
                return true;
            }
            catch (Exception ex)
            {
                last_exception = ex;
                return false;
            }
        }

        private static void ResolveContractorForServiceInit(OptionsGemotest o, out string contractorName, out string contractorCode)
        {
            contractorName = o != null ? (o.Contractor ?? "") : "";
            contractorCode = o != null ? (o.Contractor_Code ?? "") : "";

            if (!string.IsNullOrWhiteSpace(contractorCode))
                return;

            if (o == null || o.PriceLists == null)
                return;

            var first = o.PriceLists.FirstOrDefault(x => x != null && !string.IsNullOrWhiteSpace(x.ContractorCode));
            if (first != null)
            {
                contractorName = first.Name ?? "";
                contractorCode = first.ContractorCode ?? "";
            }
        }
    }
}
