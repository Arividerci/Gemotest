using Laboratory.Gemotest.Options;
using SiMed.Laboratory;
using Laboratory.Gemotest.SourseClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StatisticsCollectionSystemClient;
using System.Windows.Forms;
using static Laboratory.Gemotest.SourseClass.GemotestOrderDetail;
using Laboratory.Gemotest.GemotestRequests;

namespace Laboratory.Gemotest
{
    internal class LaboratoryGemotestGUI : ILaboratoryGUI
    {
        private LaboratoryGemotest laboratory;
        private LocalOptionsGemotest localOptions;
        private OptionsGemotest globalOptions;
        private ProductsCollection AllProducts;
        private const int std_id_priority = -2;

        public void GetProducts(ProductsCollection products)
        {
            AllProducts = products;
        }

        private Exception lastException { get; set; }
        private Exception LastException
        {
            get
            {
                return lastException;
            }
            set
            {
                if (value != null)
                    SiMed.Clinic.Logger.LogEvent.SaveErrorToLog($"Гемотест. {value.Message}\r\n{value.StackTrace}", "Gemotest");
                lastException = value;
            }
        }

        public Exception GetLastException() {
            return LastException;
        }

        public bool GetGuiOptions(out List<GuiOption> _Options)
        {
            _Options = new List<GuiOption>();
            _Options.Add(new GuiOption() { OptionName = eGuiOptionName.CanAddProduct });
            _Options.Add(new GuiOption() { OptionName = eGuiOptionName.CanRemoveProduct });
            _Options.Add(new GuiOption() { OptionName = eGuiOptionName.CanCheckResultsIfCommited });
            return true;
        }

        public bool GenerateSamples(Order _Order, OrderModelForGUI _Model)
        {
            return true;
        }

        public bool GenerateFields(Order _Order, OrderModelForGUI _Model)
        {
            return true;
        }

        public bool CreateOrderModelForGUI(bool _ReadOnly, Order _Order, ref ResultsCollection _Results, ref OrderModelForGUI _Model)
        {
            LastException = null;
            _Model.Documents.Clear();
            _Model.Errors.Clear();
            _Model.Fields.Clear();
            _Model.PriceLists.Clear();
            _Model.PriceListSelected = null;
            _Model.ProductsInfo.Clear();
            _Model.Samples.Clear();

            try
            {
                GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;

                if (_ReadOnly)
                {

                    if (details.PriceList != null)
                    {
                        _Model.PriceLists.Add(new PriceListForGUI() { Id = $"", Name = "" });
                        _Model.PriceListSelected = _Model.PriceLists[0];
                    }

                    foreach (var product in details.Products)
                    {
                        ProductInfoForGUI productNew = new ProductInfoForGUI();
                        productNew.OrderProductGuid = details.Products.IndexOf(product).ToString();

                        productNew.Id = product.ProductId;
                        productNew.Code = product.ProductCode;
                        productNew.Name = product.ProductName;
                        productNew.ProductGroupGuid = null;

                        BiomaterialGroupForGUI groupForGUI = new BiomaterialGroupForGUI();

                        foreach (var biomaterialInfo in details.BioMaterials)
                        {

                            if (biomaterialInfo.Chosen.Contains(details.Products.IndexOf(product)) ||
                                biomaterialInfo.Mandatory.Contains(details.Products.IndexOf(product)))
                            {
                                BiomaterialInfoForGUI biomInfo = new BiomaterialInfoForGUI();
                                biomInfo.BiomaterialId = biomaterialInfo.Id;
                                biomInfo.BiomaterialCode = biomaterialInfo.Code;
                                biomInfo.BiomaterialName = biomaterialInfo.Name;

                                Console.WriteLine($" - {biomInfo.BiomaterialName}");

                                var param = Dictionaries.ServiceParameters
                                    .FirstOrDefault(p =>
                                        p.service_id == product.ProductId &&
                                        p.biomaterial_id == biomaterialInfo.Id);

                                DictionaryTransport transport = null;

                                if (param != null && !string.IsNullOrEmpty(param.transport_id))
                                {
                                    transport = Dictionaries.Transport
                                        .FirstOrDefault(t => t.id == param.transport_id);
                                }
                                if (transport == null)
                                {
                                    var service = Dictionaries.Directory
                                        .FirstOrDefault(s => s.id == product.ProductId);

                                    if (service != null && !string.IsNullOrEmpty(service.transport_id))
                                    {
                                        transport = Dictionaries.Transport
                                            .FirstOrDefault(t => t.id == service.transport_id);
                                    }
                                }

                                if (transport != null)
                                {
                                    biomInfo.ContainerId = transport.id;
                                    biomInfo.ContainerCode = "";
;                                    biomInfo.ContainerName = transport.name; 
                                }
                                else
                                {
                                    biomInfo.ContainerId = "";
                                    biomInfo.ContainerCode = "";
                                    biomInfo.ContainerName = "";
                                }

                                groupForGUI.Biomaterials.Add(biomInfo);
                                if (details.BioMaterials.Count == 1)
                                    groupForGUI.BiomaterialsSelected.Add(biomInfo);
                            }
                        }

                        productNew.BiomaterialGroups.Add(groupForGUI);

                        _Model.ProductsInfo.Add(productNew);
                    }
                 
                }
            }
            catch (Exception ex) { }

            return true;
        }

        public bool SaveOrderModelForGUIToDetails(Order _Order, OrderModelForGUI _Model)
        {
            LastException = null;
            try
            {
                GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;
                //если при открытии формы заказа была возможность редактирования, то детализацию по биоматериалам нужно сохранить в OrderDetail
                // сохранение выбранного прайс листа
                if (_Model.PriceListSelected != null)
                {
                    string[] args = _Model.PriceListSelected.Id.Split('_');
                    int?[] args_int = new int?[3];
                    if (!String.IsNullOrEmpty(args[0]))
                        args_int[0] = Int32.Parse(args[0]);
                    if (!String.IsNullOrEmpty(args[1]))
                        args_int[1] = Int32.Parse(args[1]);
                    if (!String.IsNullOrEmpty(args[2]))
                        args_int[2] = Int32.Parse(args[2]);
                    /*OptionsCitiLabPriceList priceListFind = globalOptions.priceLists.Find(x => x.PriceListId == args_int[0] && x.HospitalId == args_int[1] && x.CusDepartmentId == args_int[2]);
                    if (priceListFind != null)
                        details.PriceList = priceListFind;*/
                }

                // сохранение информации о продуктах
                details.Products.Clear();
                foreach (var productInfo in _Model.ProductsInfo)
                {
                    details.Products.Add(new GemotestProductDetail()
                    {
                        OrderProductGuid = productInfo.OrderProductGuid,
                        ProductId = productInfo.Id,
                        ProductCode = productInfo.Code,
                        ProductName = productInfo.Name
                    });
                }

                foreach (var biom in details.BioMaterials)
                {
                    // выбранные и обязательные биоматериалы
                    biom.Chosen.Clear();
                    List<ProductInfoForGUI> productsFind = _Model.ProductsInfo.FindAll(p => p.BiomaterialGroups.Find(b => b.BiomaterialsSelected.Find(bs => bs.BiomaterialId == biom.Id.ToString()) != null) != null);
                    foreach (ProductInfoForGUI productInfo in productsFind)
                    {
                        int orderProductIndex = Int32.Parse(productInfo.OrderProductGuid);
                        if (biom.Mandatory.Contains(orderProductIndex))
                            continue;
                        else
                            biom.Chosen.Add(orderProductIndex);
                    }

                    // биоматериалы, доступные к выбору, но не выбранные
                    biom.Another.Clear();
                    productsFind = _Model.ProductsInfo.FindAll(p => p.BiomaterialGroups.Find(b => b.BiomaterialsSelected.Find(bs => bs.BiomaterialId == biom.Id.ToString()) == null && b.Biomaterials.Find(ba => ba.BiomaterialId == biom.Id.ToString()) != null) != null);
                    foreach (ProductInfoForGUI productInfo in productsFind)
                    {
                        int orderProductIndex = Int32.Parse(productInfo.OrderProductGuid);
                        biom.Another.Add(orderProductIndex);
                    }
                }

                // сохранение доп. полей
                foreach (var field in _Model.Fields)
                {
                    GemotestDetail detailFind = details.Details.Find(x => GetNormalizedFieldId(x.ID, x.isStdField) == field.Id);
                    if (detailFind == null)
                        continue;

                    detailFind.Value = field.Value;
                    detailFind.DisplayValue = field.DisplayValue;
                }

                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }

        private string GetNormalizedFieldId(int _FieldId, bool _IsStdField)
        {
            if (_IsStdField)
                return $"stdField_{_FieldId}";
            else
                return $"userField_{_FieldId}";
        }

        public bool CreateFormLaboratoryChooseOfProduct()
        {
            List<ProductInfoForGUI> products = new List<ProductInfoForGUI>();
            List<ProductGroupInfoForGUI> productGroups = new List<ProductGroupInfoForGUI>();

            foreach (var prod in AllProducts)
            {
                products.Add(new ProductInfoForGUI()
                {
                    OrderProductGuid = Guid.NewGuid().ToString(),
                    Id = prod.ID.ToString(),
                    Code = prod.Code,
                    Name = prod.Name,
                    ProductGroupGuid = null
                });
            }

            FormLaboratoryChooseOfProduct form = new FormLaboratoryChooseOfProduct(null, products, productGroups);
            if (form.ShowDialog() != DialogResult.OK)
                return false;

            return true;
        }
        public bool ProcessOrderGUIAction(eOrderAction _Action, Order _Order, ref OrderModelForGUI _OrderModel, ProductInfoForGUI _Product)
        {
            LastException = null;

            try
            {
                // Всегда сразу приводим детали к нужному типу
                var details = (GemotestOrderDetail)_Order.OrderDetail;

                // ---------------- УДАЛЕНИЕ ПРОДУКТА ----------------
                if (_Action == eOrderAction.RemoveProduct)
                {
                    if (_Product == null)
                        return true;

                    int productIndex = _OrderModel.ProductsInfo.IndexOf(_Product);
                    if (productIndex < 0)
                        return true;

                    // 1) Убираем из модели и деталей
                    _OrderModel.ProductsInfo.RemoveAt(productIndex);
                    details.DeleteProduct(productIndex);

                    // 2) Переиндексируем OrderProductGuid в GUI и в деталях
                    for (int i = 0; i < _OrderModel.ProductsInfo.Count; i++)
                        _OrderModel.ProductsInfo[i].OrderProductGuid = i.ToString();

                    for (int i = 0; i < details.Products.Count; i++)
                        details.Products[i].OrderProductGuid = i.ToString();

                    // 3) Сохраняем модель → детали
                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    // 4) Пересчитываем биоматериалы и группы
                    details.AddBiomaterialsFromProducts();
                    details.DeleteObsoleteDetails();
                    RebuildBiomaterialGroups(details, _OrderModel);

                    // 5) Образцы и поля
                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

                    return true;
                }

                // ---------------- ДОБАВЛЕНИЕ ПРОДУКТА ----------------
                if (_Action == eOrderAction.AddProduct)
                {
                    List<ProductInfoForGUI> products = new List<ProductInfoForGUI>();
                    List<ProductGroupInfoForGUI> productGroups = new List<ProductGroupInfoForGUI>();

                    // Список доступных услуг из AllProducts (уже отфильтрован по service_type / IsBlocked)
                    foreach (var prod in AllProducts)
                    {
                        products.Add(new ProductInfoForGUI
                        {
                            OrderProductGuid = Guid.NewGuid().ToString(),
                            Id = prod.ID,      // ID у тебя уже строка
                            Code = prod.Code,
                            Name = prod.Name,
                            ProductGroupGuid = null
                        });
                    }

                    // Форма выбора продукта
                    FormLaboratoryChooseOfProduct form = new FormLaboratoryChooseOfProduct(null, products, productGroups);
                    if (form.ShowDialog() != DialogResult.OK)
                        return false;

                    ProductInfoForGUI productNew = products.Find(x => x.OrderProductGuid == form.selectedProductGuid);
                    if (productNew == null)
                        return false;

                    // *** Проверка: такая услуга уже есть в заказе? ***
                    if (details.Products != null &&
                        details.Products.Any(p =>
                            string.Equals(p.ProductId, productNew.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show(
                            "Эта услуга уже есть в заказе.",
                            "Добавление услуги",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return false;
                    }

                    // 1) Добавляем выбранную услугу
                    int newIndex = details.Products.Count;

                    productNew.OrderProductGuid = newIndex.ToString();
                    _OrderModel.ProductsInfo.Add(productNew);

                    GemotestProductDetail productInfo = new GemotestProductDetail
                    {
                        OrderProductGuid = productNew.OrderProductGuid,
                        ProductId = productNew.Id,
                        ProductCode = productNew.Code,
                        ProductName = productNew.Name
                    };

                    details.Products.Add(productInfo);

                    // 2) Автодобавление по справочнику service_auto_insert
                    ApplyAutoInsertServices(details, _OrderModel);

                    // На всякий случай переиндексируем после auto-insert
                    for (int i = 0; i < details.Products.Count; i++)
                        details.Products[i].OrderProductGuid = i.ToString();

                    for (int i = 0; i < _OrderModel.ProductsInfo.Count; i++)
                        _OrderModel.ProductsInfo[i].OrderProductGuid = i.ToString();

                    // 3) Пересчёт биоматериалов и групп
                    details.AddBiomaterialsFromProducts();
                    details.DeleteObsoleteDetails();
                    RebuildBiomaterialGroups(details, _OrderModel);

                    // 4) Сохранение + образцы/поля
                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

                    return true;
                }

                // ---------------- ПОДГОТОВКА К ОТПРАВКЕ ----------------
                if (_Action == eOrderAction.PrepareOrderForSend)
                {
                    if (!PrepareOrderForSend(_Order))
                        return false;
                }

                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }

        private void RebuildBiomaterialGroups(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details == null || model == null || model.ProductsInfo == null)
                return;

            for (int i = 0; i < model.ProductsInfo.Count; i++)
            {
                var guiProduct = model.ProductsInfo[i];
                guiProduct.BiomaterialGroups.Clear();

                var group = BuildBiomaterialGroupForProduct(details, i);
                guiProduct.BiomaterialGroups.Add(group);
            }
        }

        private BiomaterialGroupForGUI BuildBiomaterialGroupForProduct(GemotestOrderDetail details, int productIndex)
        {
            var group = new BiomaterialGroupForGUI();

            if (details?.Products == null ||
                details.BioMaterials == null ||
                productIndex < 0 ||
                productIndex >= details.Products.Count)
                return group;

            var productDetail = details.Products[productIndex];
            var service = Dictionaries.Directory?.FirstOrDefault(s => s.id == productDetail.ProductId);
            int serviceType = service?.service_type ?? 0;

            var linkedBioms = details.BioMaterials
                .Where(b =>
                    b.Mandatory.Contains(productIndex) ||
                    b.Chosen.Contains(productIndex) ||
                    b.Another.Contains(productIndex))
                .ToList();

            foreach (var biom in linkedBioms)
            {
                var biomInfo = new BiomaterialInfoForGUI
                {
                    BiomaterialId = biom.Id,
                    BiomaterialCode = biom.Code,
                    BiomaterialName = biom.Name
                };

                DictionaryTransport transport = null;

                var param = Dictionaries.ServiceParameters?
                    .FirstOrDefault(p =>
                        p.service_id == productDetail.ProductId &&
                        p.biomaterial_id == biom.Id);

                if (param != null && !string.IsNullOrEmpty(param.transport_id))
                    transport = Dictionaries.Transport?.FirstOrDefault(t => t.id == param.transport_id);

                if (transport == null && service != null && !string.IsNullOrEmpty(service.transport_id))
                    transport = Dictionaries.Transport?.FirstOrDefault(t => t.id == service.transport_id);

                if (transport != null)
                {
                    biomInfo.ContainerId = transport.id;
                    biomInfo.ContainerCode = transport.id;
                    biomInfo.ContainerName = transport.name;
                }
                else
                {
                    biomInfo.ContainerId = "";
                    biomInfo.ContainerCode = "";
                    biomInfo.ContainerName = "";
                }

                group.Biomaterials.Add(biomInfo);
            }

            group.SelectOnlyOne = (serviceType != 2);

            var mandatoryBioms = linkedBioms
                .Where(b => b.Mandatory.Contains(productIndex))
                .ToList();

            foreach (var mand in mandatoryBioms)
            {
                var info = group.Biomaterials.FirstOrDefault(bi => bi.BiomaterialId == mand.Id);
                if (info != null)
                    group.BiomaterialsSelected.Add(info);
            }

            var optionalCandidates = linkedBioms
                .Where(b => !b.Mandatory.Contains(productIndex))
                .ToList();

            if (optionalCandidates.Count == 1)
            {
                var opt = optionalCandidates[0];
                var info = group.Biomaterials.FirstOrDefault(bi => bi.BiomaterialId == opt.Id);
                if (info != null)
                    group.BiomaterialsSelected.Add(info);
            }

            return group;
        }


        private bool PrepareOrderForSend(Order _Order)
        {
            LastException = null;
            try
            {
                GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;

                if (localOptions.PrintBlankAtOnce)
                {
                    ResultsCollection results = null;
                    PrintLaboratoryDocument(_Order, ref results, new DocumentInfoForGUI() { DocType = LaboratoryPrintDocumentType.Blank }, false);
                }

                if (localOptions.PrintStikersAtOnce)
                {
                    List<SampleInfoForGUI> samples = new List<SampleInfoForGUI>();

                    string orderNumberNormalized = _Order.Number.ToString();
                    while (orderNumberNormalized.Length < 9)
                        orderNumberNormalized = '0' + orderNumberNormalized;

                    foreach (GemotestBioMaterial cLB in ((GemotestOrderDetail)_Order.OrderDetail).BioMaterials)
                        if (cLB.Chosen.Count > 0 || cLB.Mandatory.Count > 0)
                        {
                            samples.Add(new SampleInfoForGUI()
                            {
                                
                            });
                        }
                    PrintStikers(_Order, samples);
                }

                _Order.State = OrderState.Prepared;

                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }

        public bool ValidateGUIModel(OrderModelForGUI _Model)
        {
            return true;
        }

        public bool PrintLaboratoryDocument(Order _Order, ref ResultsCollection _ResultsCollection, DocumentInfoForGUI _DocumentInfo, bool _Preview)
        {
            return true;
        }

        public void ShowOrderDetail(Order _Order) { }

        public bool PrintStikers(Order _Order, List<SampleInfoForGUI> _SelectedSamples) { return false; }

        /// <summary>
        /// Автоматически добавляет услуги по справочнику service_auto_insert.
        /// Для каждой услуги в заказе:
        ///   если есть запись в Dictionaries.ServiceAutoInsert (service_id == ProductId),
        ///   и auto_service_id ещё нет в заказе,
        ///   и auto_service_id есть в AllProducts (услуга доступна клинике),
        ///   то добавляем её в details.Products и _Model.ProductsInfo.
        /// </summary>
        private void ApplyAutoInsertServices(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details == null || details.Products == null)
                return;

            if (Dictionaries.ServiceAutoInsert == null || Dictionaries.ServiceAutoInsert.Count == 0)
                return;

            var existingServiceIds = new HashSet<string>(
                details.Products
                       .Where(p => !string.IsNullOrEmpty(p.ProductId))
                       .Select(p => p.ProductId)
            );

            if (AllProducts == null || AllProducts.Count == 0)
                return;

            foreach (var rule in Dictionaries.ServiceAutoInsert.Where(x => x.archive == 0))
            {
                if (!existingServiceIds.Contains(rule.service_id))
                    continue;

                if (existingServiceIds.Contains(rule.auto_service_id))
                    continue;

                var autoProduct = AllProducts.FirstOrDefault(p => p.ID == rule.auto_service_id);
                if (autoProduct == null)
                    continue; 

                int newIndex = details.Products.Count;

                var autoDetail = new GemotestOrderDetail.GemotestProductDetail
                {
                    OrderProductGuid = newIndex.ToString(),
                    ProductId = autoProduct.ID,
                    ProductCode = autoProduct.Code,
                    ProductName = autoProduct.Name
                };
                details.Products.Add(autoDetail);

                var autoGui = new ProductInfoForGUI
                {
                    OrderProductGuid = autoDetail.OrderProductGuid,
                    Id = autoDetail.ProductId,
                    Code = autoDetail.ProductCode,
                    Name = autoDetail.ProductName,
                    ProductGroupGuid = null
                };
                model.ProductsInfo.Add(autoGui);

                existingServiceIds.Add(rule.auto_service_id);
            }
        }

    }
}
