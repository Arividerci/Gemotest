using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Xml;
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
using System.Runtime.InteropServices;
namespace Laboratory.Gemotest
{

    public class LaboratoryGemotest : ILaboratory
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        List<DictionaryService> ProductsGemotest;

        public List<ProductGemotest> product;

        public GemotestService Gemotest;
        public ProductsCollection AllProducts {  get; set; }
        public LocalOptions LocalOptions { get; set; }
        public SystemOptions Options { get; set; }
        public Dictionaries Dicts { get; } = new Dictionaries();

        private const string TestResultExtNum = "118569168";
        private Exception last_exception = new Exception("неизвестная ошибка");
        private INumerator numerator;
        private LaboratoryGemotestGUI laboratoryGUI;
        public LaboratoryType GetLaboratoryType()
        {
            return (LaboratoryType)24;

        }
        // Test

        private const string TestResultOrderNum = "118569168";

        private bool TryChooseResultRequestNumbers(Order order, out string requestOrderNum, out string requestExtNum)
        {
            requestOrderNum = string.Empty;
            requestExtNum = string.Empty;

            string currentExtNum = order != null ? (order.Number ?? string.Empty) : string.Empty;

            var choice = MessageBox.Show(
                "Запросить результаты:\r\n\r\n" +
                "Да — по текущему ext_num заказа: " + currentExtNum + "\r\n" +
                "Нет — по тестовому order_num: " + TestResultOrderNum + "\r\n" +
                "Отмена — не выполнять запрос.",
                "Гемотест: выбор номера для запроса результатов",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (choice == DialogResult.Cancel)
                return false;

            if (choice == DialogResult.Yes)
            {
                requestExtNum = currentExtNum;
            }
            else
            {
                requestOrderNum = TestResultOrderNum;
            }

            return true;
        }

        // Test

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
                    Duration = p.Duration,
                    DurationInfo = !string.IsNullOrWhiteSpace(p.DurationInfo)
                        ? p.DurationInfo
                        : (p.Duration > 0 ? $"{p.Duration} дн." : string.Empty)
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

            if (_Order != null && status != OrderState.NotSended)
            {
                ResultsCollection showResults = new ResultsCollection();
                return ShowOrder(_Order, true, ref showResults);
            }
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

            if (_Order.State != status || res == DialogResult.OK)
                return true;

            return false;
        }

        public bool ShowOrder(Order _Order, bool _bReadOnly, ref ResultsCollection _Results)
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

            GemotestOrderDetail details = _Order.OrderDetail as GemotestOrderDetail;
            if (details == null)
            {
                last_exception = new Exception("OrderDetail не задан (ожидался GemotestOrderDetail).");
                MessageBox.Show(last_exception.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            details.Dicts = Dicts;
            ApplyPriceListToDetails(details);
            details.AddBiomaterialsFromProducts();
            details.DeleteObsoleteDetails();

            bool readOnly = _bReadOnly || _Order.State != OrderState.NotSended;

            ResultsCollection currentResults = _Results ?? new ResultsCollection();
            OrderModelForGUI model = new OrderModelForGUI();

            if (!laboratoryGUI.CreateOrderModelForGUI(readOnly, _Order, ref currentResults, ref model))
            {
                MessageBox.Show(GetLastException().Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            FormLaboratoryOrder form = new FormLaboratoryOrder(
                _Laboratory: this,
                _LaboratoryGUI: laboratoryGUI,
                _Order: _Order,
                _FormCaption: "Гемотест: просмотр заказа",
                _ResultsCollection: ref currentResults,
                _OrderModel: ref model,
                _ReadOnly: readOnly);

            var res = form.ShowDialog();

            if (res == DialogResult.OK && !readOnly)
            {
                if (!laboratoryGUI.SaveOrderModelForGUIToDetails(_Order, model))
                {
                    MessageBox.Show($"Ошибка сохранения деталей заказа: {GetLastException().Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            _Results = currentResults;
            return _Order.State != status || res == DialogResult.OK;
        }

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
            GemotestSystemOptionsForm optionsSystem = new GemotestSystemOptionsForm(_SystemOptions);
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
                Options = (SystemOptions)new SystemOptions().Unpack(_SystemOptions);
            }
            else
            {
                if (Options == null) Options = new SystemOptions();

            }

            if (!string.IsNullOrWhiteSpace(_LocalOptions))
            {
                LocalOptions = (LocalOptions)new LocalOptions().Unpack(_LocalOptions);
            }
            else
            {

                if (LocalOptions == null) LocalOptions = new LocalOptions();
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
            }
        }
        public bool Init()
        {
            //AllocConsole();

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

                laboratoryGUI.SetOptions(this, GetProducts(), LocalOptions, Options, numerator);

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

        public void SetNumerator(INumerator _Numerator)
        {
            numerator = _Numerator;
        }

        private static bool IsGemotestOptionsValid(SystemOptions o)
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

        public bool CheckResult(Order _Order, ref ResultsCollection _Results)
        {
            last_exception = null;

            try
            {
                if (_Results == null)
                    _Results = new ResultsCollection();

                if (_Order == null)
                    throw new InvalidOperationException("Заказ не задан.");

                _Order.SampleError = false;

                if (laboratoryGUI == null)
                {
                    if (!Init())
                    {
                        MessageBox.Show("Ошибка инициализации модуля Гемотест", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                if (!IsGemotestOptionsValid(Options))
                    throw new InvalidOperationException("Опции Gemotest не заполнены (Url/Login/Password/Contractor_Code/Salt).");

                var details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                string contractorCode = !string.IsNullOrWhiteSpace(details.PriceListCode)
                    ? details.PriceListCode
                    : (Options.Contractor_Code ?? string.Empty);

                if (string.IsNullOrWhiteSpace(contractorCode))
                    throw new InvalidOperationException("Не определен contractorCode для запроса результатов.");

                // закоменчено на время тестирования
                /* string extNum = _Order.Number ?? string.Empty;
                 if (string.IsNullOrWhiteSpace(extNum))
                     throw new InvalidOperationException("У заказа отсутствует внешний номер (_Order.Number). Запросить результаты невозможно.");

                 OrderState statusBefore = _Order.State;

                 string hash = BuildContractorHash(contractorCode, Options.Salt);
                 string requestXml = BuildGetAnalysisResultEnvelope(contractorCode, hash, "", extNum);*/

                string requestOrderNum;
                string requestExtNum;

                if (!TryChooseResultRequestNumbers(_Order, out requestOrderNum, out requestExtNum))
                    return false;

                OrderState statusBefore = _Order.State;

                string hash = BuildContractorHash(contractorCode, Options.Salt);
                string requestXml = BuildGetAnalysisResultEnvelope(contractorCode, hash, requestOrderNum, requestExtNum);

                // закоменчено на время тестирования

                Console.WriteLine("\n\n" + requestXml + "\n\n");

                string responseXml = SendSoapRequest(Options.UrlAdress, Options.Login, Options.Password, "get_analysis_result", requestXml);
                details.ResultsRawXml = responseXml ?? string.Empty;
                details.ResultsOrderNum = ExtractAnalysisResultOrderNum(responseXml);
                details.ResultsExtNum = ExtractAnalysisResultExtNum(responseXml);
                Console.WriteLine(responseXml);

                var response = ParseGetAnalysisResultResponse(responseXml);
                SaveResultsToOrderDetail(details, response);

                if (response.ErrorCode != 0)
                {
                    last_exception = new Exception(
                        $"Гемотест вернул ошибку при запросе результатов. Код={response.ErrorCode}. " +
                        $"{response.ErrorDescription}");
                    return false;
                }

                if (response.Status == 2)
                {
                    _Order.SampleError = true;
                    last_exception = new Exception("По заказу получен отказ от выполнения исследования.");
                    return false;
                }

                bool hasResults = response.ResultsCount > 0;
                bool hasAttachments = response.AttachmentsCount > 0;

                if (response.Status == 1)
                {
                    _Order.State = OrderState.FullResultReceived;
                    return hasResults || hasAttachments || _Order.State != statusBefore;
                }

                // status == 0: заказ еще выполняется
                // если что-то уже пришло частично — отметим частичные результаты
                if (hasResults || hasAttachments)
                {
                    _Order.State = OrderState.PartialResultReceived;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                last_exception = ex;
                SiMed.Clinic.Logger.LogEvent.SaveErrorToLog(ex.Message, "Gemotest");
                return false;
            }
        }

        private static string ExtractAnalysisResultOrderNum(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return string.Empty;

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode returnNode = doc.SelectSingleNode("//*[local-name()='get_analysis_resultResponse']/*[local-name()='return']");
            return GetNodeText(returnNode, "order_num");
        }

        private static string ExtractAnalysisResultExtNum(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return string.Empty;

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode returnNode = doc.SelectSingleNode("//*[local-name()='get_analysis_resultResponse']/*[local-name()='return']");
            return GetNodeText(returnNode, "ext_num");
        }

        private void SaveResultsToOrderDetail(GemotestOrderDetail details, GemotestAnalysisResultResponse response)
        {
            if (details == null || response == null)
                return;

            details.Results = new List<GemotestResultDetail>();
            details.Attachments = new List<GemotestAttachmentDetail>();

            if (response.Results != null)
            {
                foreach (var item in response.Results)
                {
                    string orderProductGuid = ResolveOrderProductGuidByServiceId(details, item.ServiceId);

                    details.Results.Add(new GemotestResultDetail
                    {
                        Id = item.Id ?? string.Empty,
                        Name = item.Name ?? string.Empty,
                        TestRusName = item.TestRusName ?? string.Empty,
                        SectionName = item.SectionName ?? string.Empty,
                        Value = item.Value ?? string.Empty,
                        MeasurementUnit = item.MeasurementUnit ?? string.Empty,
                        RefMin = item.RefMin ?? string.Empty,
                        RefMax = item.RefMax ?? string.Empty,
                        RefRange = item.RefRange ?? string.Empty,
                        RefText = item.RefText ?? string.Empty,
                        ResultDate = item.ResultDate ?? string.Empty,
                        ServiceId = item.ServiceId ?? string.Empty,
                        Status = item.Status ?? string.Empty,
                        OrderProductGuid = orderProductGuid ?? string.Empty
                    });
                }
            }

            if (response.Attachments != null)
            {
                int idx = 1;
                foreach (var item in response.Attachments)
                {
                    details.Attachments.Add(new GemotestAttachmentDetail
                    {
                        SectionName = item.SectionName ?? string.Empty,
                        FileUrl = item.FileUrl ?? string.Empty,
                        DisplayName = BuildAttachmentDisplayName(item, idx++),
                        OrderProductGuid = string.Empty,
                        OrderSampleGuid = string.Empty
                    });
                }
            }
        }

        private string ResolveOrderProductGuidByServiceId(GemotestOrderDetail details, string serviceId)
        {
            if (details == null || details.Products == null || string.IsNullOrWhiteSpace(serviceId))
                return string.Empty;

            var direct = details.Products.FirstOrDefault(x =>
                x != null &&
                !string.IsNullOrWhiteSpace(x.ProductId) &&
                string.Equals(x.ProductId, serviceId, StringComparison.OrdinalIgnoreCase));

            if (direct != null)
                return direct.OrderProductGuid ?? string.Empty;

            var byCode = details.Products.FirstOrDefault(x =>
                x != null &&
                !string.IsNullOrWhiteSpace(x.ProductCode) &&
                string.Equals(x.ProductCode, serviceId, StringComparison.OrdinalIgnoreCase));

            if (byCode != null)
                return byCode.OrderProductGuid ?? string.Empty;

            return string.Empty;
        }

        private string BuildAttachmentDisplayName(GemotestAttachmentResponseItem item, int index)
        {
            string section = item != null ? (item.SectionName ?? "") : "";
            if (!string.IsNullOrWhiteSpace(section))
                return $"Файл результатов {section}.pdf";

            return $"Файл результатов #{index}.pdf";
        }
        public bool ExtractContainers(Order _Order, ref ContainersCollection _Containers)
        {
            try
            {
                _Containers = new ContainersCollection();

                if (_Order == null)
                    throw new ArgumentNullException(nameof(_Order));

                if (laboratoryGUI == null)
                {
                    if (!Init())
                    {
                        MessageBox.Show("Ошибка инициализации модуля Гемотест", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                GemotestOrderDetail details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                details.Dicts = Dicts;

                ResultsCollection results = new ResultsCollection();
                OrderModelForGUI model = new OrderModelForGUI();

                if (!laboratoryGUI.CreateOrderModelForGUI(true, _Order, ref results, ref model))
                {
                    Exception guiEx = laboratoryGUI.GetLastException();
                    if (guiEx != null)
                        throw guiEx;

                    throw new Exception("Не удалось построить GUI-модель заказа для извлечения контейнеров.");
                }

                if (model.Samples == null || model.Samples.Count == 0)
                    return true;

                foreach (var sample in model.Samples)
                {
                    if (sample == null || sample.Biomaterial == null)
                        continue;

                    Container labContainer = new Container();
                    labContainer.BarCode = sample.Barcode ?? string.Empty;
                    labContainer.Code = sample.Biomaterial.ContainerCode ?? string.Empty;
                    labContainer.Name = sample.Biomaterial.ContainerName ?? string.Empty;
                    labContainer.BioMaterialName = sample.Biomaterial.BiomaterialName ?? string.Empty;
                    labContainer.Comment = string.Empty;

                    _Containers.Add(labContainer);
                }

                return true;
            }
            catch (Exception e)
            {
                last_exception = e;
                MessageBox.Show("Ошибка при извлечении сведений о контейнерах (лаборатория Гемотест): " + e.Message);
                return false;
            }
        }

        public bool ExtractResult(Order _Order, ref ResultsCollection _Results)
        {
            try
            {
                if (_Results == null)
                    _Results = new ResultsCollection();

                return CheckResult(_Order, ref _Results);
            }
            catch (Exception e)
            {
                last_exception = e;
                return false;
            }
            }

        public void SetContainerMarkerList(List<IContainerMarker> _ContainerMarkerList) { }

        public Exception GetLastException() { return last_exception; }

        public void BeginTransaction(LaboratoryTransactionType _TransactionType) { }

        public void EndTransaction(LaboratoryTransactionType _TransactionType) { }

        public bool GetNumbersPoolIfNeed(out bool _NumbersPoolChanged, out string _SystemOptionsNew) { _NumbersPoolChanged = false; _SystemOptionsNew = ""; return true; }

        private static bool HasConfiguredPriceLists(SystemOptions o)
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

        private static void ResolveContractorForServiceInit(SystemOptions o, out string contractorName, out string contractorCode)
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

        private static string BuildContractorHash(string contractor, string salt)
        {
            string plain = (contractor ?? "") + (salt ?? "");

            using (var sha1 = SHA1.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                byte[] hash = sha1.ComputeHash(data);

                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));

                return sb.ToString();
            }
        }

        private static string BuildGetAnalysisResultEnvelope(string contractor, string hash, string orderNum, string extNum)
        {
            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope ");
            sb.Append("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ");
            sb.Append("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ");
            sb.Append("xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
            sb.Append("xmlns:urn=\"urn:OdoctorControllerwsdl\" ");
            sb.Append("xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<urn:get_analysis_result soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            sb.Append("<params xsi:type=\"urn:request_get_analysis_result\">");

            sb.Append("<contractor xsi:type=\"xsd:string\">").Append(SecurityElementEscape(contractor)).Append("</contractor>");
            sb.Append("<hash xsi:type=\"xsd:string\">").Append(SecurityElementEscape(hash)).Append("</hash>");
            sb.Append("<order_num xsi:type=\"xsd:string\">").Append(SecurityElementEscape(orderNum)).Append("</order_num>");
            sb.Append("<ext_num xsi:type=\"xsd:string\">").Append(SecurityElementEscape(extNum ?? "")).Append("</ext_num>");

            sb.Append("</params>");
            sb.Append("</urn:get_analysis_result>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }

        private static string SendSoapRequest(string url, string login, string password, string method, string xmlBody)
        {
            string soapAction = "\"urn:OdoctorControllerwsdl#" + method + "\"";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers["SOAPAction"] = soapAction;

            string credentials = (login ?? "") + ":" + (password ?? "");
            string authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
            request.Headers["Authorization"] = "Basic " + authHeader;
            request.PreAuthenticate = true;

            byte[] payload = Encoding.UTF8.GetBytes(xmlBody);
            request.ContentLength = payload.Length;

            using (var reqStream = request.GetRequestStream())
                reqStream.Write(payload, 0, payload.Length);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private sealed class GemotestAnalysisResultResponse
        {
            public int ErrorCode;
            public string ErrorDescription;
            public int Status;

            public List<GemotestResultResponseItem> Results = new List<GemotestResultResponseItem>();
            public List<GemotestAttachmentResponseItem> Attachments = new List<GemotestAttachmentResponseItem>();

            public int ResultsCount => Results != null ? Results.Count : 0;
            public int AttachmentsCount => Attachments != null ? Attachments.Count : 0;
        }

        private sealed class GemotestResultResponseItem
        {
            public string Id;
            public string Name;
            public string TestRusName;
            public string SectionName;
            public string Value;
            public string MeasurementUnit;
            public string RefMin;
            public string RefMax;
            public string RefRange;
            public string RefText;
            public string ResultDate;
            public string ServiceId;
            public string Status;
        }

        private sealed class GemotestAttachmentResponseItem
        {
            public string SectionName;
            public string FileUrl;
        }
        private static GemotestAnalysisResultResponse ParseGetAnalysisResultResponse(string xml)
        {
            var result = new GemotestAnalysisResultResponse
            {
                ErrorCode = -1,
                ErrorDescription = "Пустой ответ"
            };

            if (string.IsNullOrWhiteSpace(xml))
                return result;

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode returnNode = doc.SelectSingleNode("//*[local-name()='get_analysis_resultResponse']/*[local-name()='return']");
            if (returnNode == null)
            {
                result.ErrorDescription = "Не найден узел return в ответе get_analysis_result.";
                return result;
            }

            result.ErrorCode = ParseInt(GetNodeText(returnNode, "error_code"), -1);
            result.ErrorDescription = GetNodeText(returnNode, "error_description");
            result.Status = ParseInt(GetNodeText(returnNode, "status"), 0);

            XmlNodeList clItems = returnNode.SelectNodes("./*[local-name()='results_cl']/*[local-name()='item']");
            if (clItems != null)
            {
                foreach (XmlNode node in clItems)
                {
                    result.Results.Add(ParseResultNode(node, false));
                }
            }

            XmlNodeList mbItems = returnNode.SelectNodes("./*[local-name()='results_mb']/*[local-name()='item']");
            if (mbItems != null)
            {
                foreach (XmlNode node in mbItems)
                {
                    result.Results.Add(ParseResultNode(node, true));
                }
            }

            XmlNodeList attItems = returnNode.SelectNodes("./*[local-name()='attachments']/*[local-name()='item']");
            if (attItems != null)
            {
                foreach (XmlNode node in attItems)
                {
                    if (node == null)
                        continue;

                    result.Attachments.Add(new GemotestAttachmentResponseItem
                    {
                        SectionName = GetNodeText(node, "section_name"),
                        FileUrl = GetNodeText(node, "file")
                    });
                }
            }

            return result;
        }

        private static GemotestResultResponseItem ParseResultNode(XmlNode node, bool isMb)
        {
            if (node == null)
                return new GemotestResultResponseItem();

            return new GemotestResultResponseItem
            {
                Id = GetNodeText(node, "id"),
                Name = GetNodeText(node, "name"),
                TestRusName = GetNodeText(node, "test_rusname"),
                SectionName = GetNodeText(node, "section_name"),
                Value = GetNodeText(node, "value"),
                MeasurementUnit = GetNodeText(node, "measurement_unit"),
                RefMin = GetNodeText(node, "ref_min"),
                RefMax = GetNodeText(node, "ref_max"),
                RefRange = GetNodeText(node, "ref_range"),
                RefText = GetNodeText(node, "ref_text"),
                ResultDate = GetNodeText(node, "result_date"),
                ServiceId = GetNodeText(node, "service_id"),
                Status = isMb ? GetNodeText(node, "status_mb") : GetNodeText(node, "status_cl")
            };
        }

        private static string GetNodeText(XmlNode parent, string localName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(localName))
                return string.Empty;

            XmlNode node = parent.SelectSingleNode(".//*[local-name()='" + localName + "']");
            return node != null ? (node.InnerText ?? string.Empty).Trim() : string.Empty;
        }

        private static int ParseInt(string s, int defValue)
        {
            int v;
            return int.TryParse((s ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : defValue;
        }

        private static string SecurityElementEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

    }
}
