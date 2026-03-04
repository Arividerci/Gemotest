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
                // Печать ДО фильтров, чтобы понять, кого ты отсекаешь
                Console.WriteLine(
                    $"RAW: id={p.ID} code={p.Code} name={p.Name} blocked={p.IsBlocked} serviceType={p.ServiceType} duration={p.Duration}"
                );

                if (p.IsBlocked)
                    continue;

                if (p.ServiceType == 3 || p.ServiceType == 4)
                    continue;

                if (string.IsNullOrEmpty(p.ID) || string.IsNullOrEmpty(p.Code) || string.IsNullOrEmpty(p.Name))
                    continue;

                Console.WriteLine($"ADD: {p.Code} | {p.Name} | Duration={p.Duration}");

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
            details.AddBiomaterialsFromProducts();
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

                var sender = new GemotestOrderSender(
                    Options.UrlAdress,
                    Options.Contractor_Code,
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
            Directory.CreateDirectory(BaseDir);


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
            Directory.CreateDirectory(BaseDir);

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
            Directory.CreateDirectory(BaseDir);

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



                if (!RefreshDictionariesAtInit())
                    return false;

                // На всякий случай сбрасываем кэш продуктов (чтобы перечитать Directory.xml после обновления)
                ProductsGemotest = null;
                product = null;
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
            // Требование:
            // 1) при каждой инициализации пытаемся скачать свежие справочники
            // 2) если не смогли — используем старые локальные
            if (Gemotest == null)
                return false;

            string root = Gemotest.filePath;

            // 0) сначала пытаемся прочитать старые (если есть)
            bool oldLoaded = false;
            try { oldLoaded = Dicts.Unpack(root); } catch { oldLoaded = false; }

            // 1) бэкап существующих XML, чтобы можно было откатить частично скачанные/битые файлы
            string backupDir = Path.Combine(root, "_backup");

            try
            {
                BackupDictionaryFiles(root, backupDir);

                // 2) форсим обновление: если внутри GemotestService стоит проверка "24 часа",
                //    то искусственно "старим" файлы, чтобы метод не скипал загрузку.
                ForceDictionaryFilesOutdated(root, 2);

                // 3) пробуем скачать
                bool downloaded = Gemotest.get_all_dictionary();

                // 4) если скачали — пробуем распаковать новые; если распаковка упала — откат
                if (downloaded)
                {
                    bool unpackOk = Dicts.Unpack(root);
                    if (unpackOk)
                    {
                        DeleteDirectorySafe(backupDir);
                        return true;
                    }
                }

                // Скачивание не удалось или новые файлы битые → откатываемся
                RestoreDictionaryFiles(root, backupDir);

                bool restoredOk = Dicts.Unpack(root);
                if (restoredOk)
                    return true;

                // Если даже восстановленные не читаются — тогда возвращаем то, что было в памяти
                return oldLoaded;
            }
            catch (Exception ex)
            {
                // Любая ошибка обновления не должна "ронять" работу, если старые справочники уже были
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

        private static readonly string BaseDir =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Симплекс", "СиМед - Клиника", "GemotestDictionaries", "Options");

        private static readonly string OptionsFilePath = Path.Combine(BaseDir, "options.xml");
        private static readonly string LocalOptionsFilePath = Path.Combine(BaseDir, "local_options.xml");

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
