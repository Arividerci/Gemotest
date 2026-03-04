using Laboratory.Gemotest.Options;
using SiMed.Laboratory;
using Laboratory.Gemotest.SourseClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StatisticsCollectionSystemClient;
using static Laboratory.Gemotest.SourseClass.GemotestOrderDetail;
using Laboratory.Gemotest.GemotestRequests;

namespace Laboratory.Gemotest
{
    public class LaboratoryGemotestGUI : ILaboratoryGUI
    {
        private LaboratoryGemotest laboratory;
        private LocalOptionsGemotest localOptions;
        private OptionsGemotest globalOptions;
        private ProductsCollection AllProducts;

        public bool SetAssignedModules(LaboratoryGemotest lab, ProductsCollection products, LocalOptionsGemotest local, OptionsGemotest global)
        {
            laboratory = lab;
            localOptions = local;
            globalOptions = global;
            AllProducts = products;
            return true;
        }

        private Exception lastException { get; set; }
        private Exception LastException
        {
            get => lastException;
            set
            {
                if (value != null)
                    SiMed.Clinic.Logger.LogEvent.SaveErrorToLog($"Гемотест. {value.Message}\r\n{value.StackTrace}", "Gemotest");
                lastException = value;
            }
        }

        public Exception GetLastException() => LastException;

        private static string NormalizeServiceName(string name)
        {
            // Убираем суффикс вида " (N дн.)", который мы добавляем для отображения в GUI
            if (string.IsNullOrWhiteSpace(name))
                return name;

            string suffixEnd = " дн.)";
            int end = name.LastIndexOf(suffixEnd, StringComparison.Ordinal);
            if (end < 0 || end + suffixEnd.Length != name.Length)
                return name;

            int open = name.LastIndexOf(" (", StringComparison.Ordinal);
            if (open < 0 || open >= end)
                return name;

            string numberPart = name.Substring(open + 2, end - (open + 2)); // между " (" и " дн.)"
            // допускаем пробелы
            numberPart = numberPart.Trim();
            for (int i = 0; i < numberPart.Length; i++)
            {
                if (!char.IsDigit(numberPart[i]))
                    return name;
            }

            return name.Substring(0, open).TrimEnd();
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
            LastException = null;
            try
            {
                // Создаём образцы для продуктов с учётом выбранных биоматериалов.
                // Ключ объединения: BiomaterialCode + ContainerCode.
                var samples = new List<SampleInfoForGUI>();

                foreach (var productInfo in _Model.ProductsInfo)
                {
                    foreach (var biomGroupInfo in productInfo.BiomaterialGroups)
                    {
                        foreach (var biomInfo in biomGroupInfo.BiomaterialsSelected)
                        {
                            // если в заказе два одинаковых продукта (один и тот же Code),
                            // не кладём их оба в один и тот же контейнер
                            var guidsWithSameProductCode = _Model.ProductsInfo
                                .Where(x => x.Code == productInfo.Code)
                                .Select(x => x.OrderProductGuid)
                                .ToList();

                            SampleInfoForGUI sampleFind = null;

                            foreach (var sample in samples)
                            {
                                if (sample.Biomaterial.BiomaterialCode != biomInfo.BiomaterialCode)
                                    continue;
                                if (sample.Biomaterial.ContainerCode != biomInfo.ContainerCode)
                                    continue;

                                if (sample.OrderProductGuids.Find(x => guidsWithSameProductCode.Contains(x)) != null)
                                    continue;

                                sampleFind = sample;
                                break;
                            }

                            if (sampleFind == null)
                            {
                                var sampleNew = new SampleInfoForGUI();
                                sampleNew.OrderSampleGuid = Guid.NewGuid().ToString();
                                sampleNew.Biomaterial = biomInfo;
                                sampleNew.OrderProductGuids.Add(productInfo.OrderProductGuid);
                                samples.Add(sampleNew);
                            }
                            else
                            {
                                sampleFind.OrderProductGuids.Add(productInfo.OrderProductGuid);
                            }
                        }
                    }
                }

                // Стыкуем с уже существующими образцами модели, чтобы сохранить OrderSampleGuid/Barcode
                foreach (var sampleNew in samples)
                {
                    foreach (var modelSample in _Model.Samples)
                    {
                        if (modelSample.Biomaterial.BiomaterialCode != sampleNew.Biomaterial.BiomaterialCode)
                            continue;
                        if (modelSample.Biomaterial.ContainerCode != sampleNew.Biomaterial.ContainerCode)
                            continue;

                        if (string.Join(",", sampleNew.OrderProductGuids.OrderBy(x => x)) !=
                            string.Join(",", modelSample.OrderProductGuids.OrderBy(x => x)))
                            continue;

                        sampleNew.OrderSampleGuid = modelSample.OrderSampleGuid;
                        sampleNew.Barcode = modelSample.Barcode;
                        break;
                    }
                }

                _Model.Samples = samples;
                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }
        public bool GenerateFields(Order _Order, OrderModelForGUI _Model) => true;

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
                var details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    return true;


                details.Dicts = laboratory.Dicts;
                details.Order = _Order;
                details.Order = _Order;
                if (details.PriceList != null)
                {
                    _Model.PriceLists.Add(new PriceListForGUI() { Id = $"", Name = "" });
                    _Model.PriceListSelected = _Model.PriceLists[0];
                }
                foreach (var product in details.Products)
                {
                    var p = new ProductInfoForGUI
                    {
                        OrderProductGuid = details.Products.IndexOf(product).ToString(),
                        Id = product.ProductId,
                        Code = product.ProductCode,
                        Name = (product.DurationDays > 0 ? $"{product.ProductName} ({product.DurationDays} дн.)" : product.ProductName),
                        ProductGroupGuid = null
                    };
                    PrintServiceMetaToConsole(p.Id);

                    _Model.ProductsInfo.Add(p);
                }
                RebuildBiomaterialGroups(details, _Model);
                if (!GenerateSamples(_Order, _Model))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return true;
            }
        }

        public bool SaveOrderModelForGUIToDetails(Order _Order, OrderModelForGUI _Model)
        {
            LastException = null;
            try
            {
                var details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    return true;


                details.Dicts = laboratory.Dicts;
                details.Order = _Order;
                details.Products.Clear();
                foreach (var productInfo in _Model.ProductsInfo)
                {
                    details.Products.Add(new GemotestProductDetail()
                    {
                        OrderProductGuid = productInfo.OrderProductGuid,
                        ProductId = productInfo.Id,
                        ProductCode = productInfo.Code,
                        ProductName = NormalizeServiceName(productInfo.Name)
                    });
                }

                foreach (var biom in details.BioMaterials)
                {
                    biom.Chosen.Clear();

                    var productsChosen = _Model.ProductsInfo.FindAll(p =>
                        p.BiomaterialGroups.Find(g =>
                            g.BiomaterialsSelected != null &&
                            g.BiomaterialsSelected.Find(bs => bs.BiomaterialId == biom.Id) != null
                        ) != null
                    );

                    foreach (var p in productsChosen)
                    {
                        int idx = int.Parse(p.OrderProductGuid);
                        if (!biom.Mandatory.Contains(idx))
                            biom.Chosen.Add(idx);
                    }

                    biom.Another.Clear();
                    var productsAnother = _Model.ProductsInfo.FindAll(p =>
                        p.BiomaterialGroups.Find(g =>
                            g.BiomaterialsSelected != null &&
                            g.BiomaterialsSelected.Find(bs => bs.BiomaterialId == biom.Id) == null &&
                            g.Biomaterials != null &&
                            g.Biomaterials.Find(ba => ba.BiomaterialId == biom.Id) != null
                        ) != null
                    );

                    foreach (var p in productsAnother)
                        biom.Another.Add(int.Parse(p.OrderProductGuid));
                }

                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }

        public bool CreateFormLaboratoryChooseOfProduct()
        {
            var products = new List<ProductInfoForGUI>();
            var productGroups = new List<ProductGroupInfoForGUI>();

            foreach (var prod in AllProducts)
            {
                products.Add(new ProductInfoForGUI()
                {
                    OrderProductGuid = Guid.NewGuid().ToString(),
                    Id = prod.ID.ToString(),
                    Code = prod.Code,
                    Name = (prod.Duration > 0 ? $"{prod.Name} ({prod.Duration} дн.)" : prod.Name),
                    ProductGroupGuid = null
                });
            }
            PrepareBiomaterialsForChooseForm(products);
            var form = new FormLaboratoryChooseOfProduct(null, products, productGroups);
            return form.ShowDialog() == DialogResult.OK;
        }

        public bool ProcessOrderGUIAction(eOrderAction _Action, Order _Order, ref OrderModelForGUI _OrderModel, ProductInfoForGUI _Product)
        {
            LastException = null;
            try
            {
                if (_Action == eOrderAction.RemoveProduct)
                {
                    int productIndex = _OrderModel.ProductsInfo.IndexOf(_Product);
                    _OrderModel.ProductsInfo.Remove(_Product);

                    var details = (GemotestOrderDetail)_Order.OrderDetail;
                    details.DeleteProduct(productIndex);

                    for (int i = 0; i < _OrderModel.ProductsInfo.Count; i++)
                        _OrderModel.ProductsInfo[i].OrderProductGuid = i.ToString();

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    details.AddBiomaterialsFromProducts();
                    details.DeleteObsoleteDetails();

                    RebuildBiomaterialGroups(details, _OrderModel);
                    GenerateSamples(_Order, _OrderModel);
                    return true;
                }

                if (_Action == eOrderAction.AddProduct)
                {
                    var products = new List<ProductInfoForGUI>();
                    var groups = new List<ProductGroupInfoForGUI>();

                    foreach (var prod in AllProducts)
                    {
                        if (laboratory.Dicts.Directory != null && laboratory.Dicts.Directory.TryGetValue(prod.ID, out var svc) && svc != null)
                        {
                            if (svc.service_type == 3 || svc.service_type == 4)
                                continue;
                        }

                        products.Add(new ProductInfoForGUI()
                        {
                            OrderProductGuid = Guid.NewGuid().ToString(),
                            Id = prod.ID,
                            Code = prod.Code,
                            Name = (prod.Duration > 0 ? $"{prod.Name} ({prod.Duration} дн.)" : prod.Name),
                            ProductGroupGuid = null
                        });
                    }
                    PrepareBiomaterialsForChooseForm(products);
                    var form = new FormLaboratoryChooseOfProduct(null, products, groups);
                    if (form.ShowDialog() != DialogResult.OK)
                        return false;

                    var details = (GemotestOrderDetail)_Order.OrderDetail;
                    var productNew = products.Find(x => x.OrderProductGuid == form.selectedProductGuid);
                    var selectedBioIds = GetSelectedBiomaterialIds(productNew);
                    if (productNew == null)
                        return false;

                    if (details.Products != null &&
                        details.Products.Any(p => string.Equals(p.ProductId, productNew.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Эта услуга уже есть в заказе.", "Добавление услуги",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }

                    int newIndex = details.Products.Count;
                    productNew.OrderProductGuid = newIndex.ToString();
                    _OrderModel.ProductsInfo.Add(productNew);

                    details.Products.Add(new GemotestOrderDetail.GemotestProductDetail
                    {
                        OrderProductGuid = productNew.OrderProductGuid,
                        ProductId = productNew.Id,
                        ProductCode = productNew.Code,
                        ProductName = NormalizeServiceName(productNew.Name)
                    });

                    ApplyAutoInsertServices(details, _OrderModel);

                    details.AddBiomaterialsFromProducts();
                    RebuildBiomaterialGroups(details, _OrderModel);
                    ApplySelectedBiomaterialsToAddedProduct(_Order, _OrderModel, newIndex, selectedBioIds);

                    GenerateSamples(_Order, _OrderModel);

                    PrintServiceMetaToConsole(productNew.Id);

                    return true;
                }

                if (_Action == eOrderAction.PrepareOrderForSend)
                {

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;


                    if (!EnsureSupplementalsIfNeeded(_Order, _OrderModel))
                        return false;


                    if (!EnsureAdditionalPatientInfo(_Order, _OrderModel))
                        return false;

                    GenerateSamples(_Order, _OrderModel);

                    _Order.State = OrderState.Prepared;
                    return true;
                }

                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }

        private bool SendOrderToGemotest(Order order)
        {
            LastException = null;
            try
            {
                if (order == null)
                    throw new InvalidOperationException("Заказ не задан.");

                var details = order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                if (details.Products == null || details.Products.Count == 0)
                    throw new InvalidOperationException("В заказе нет ни одной услуги.");

                if (globalOptions == null)
                    globalOptions = new OptionsGemotest();

                var sender = new GemotestOrderSender(
                    globalOptions.UrlAdress,
                    globalOptions.Contractor_Code,
                    globalOptions.Salt,
                    globalOptions.Login,
                    globalOptions.Password
                );

                string errorMessage;
                if (!sender.CreateOrder(order, out errorMessage))
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        MessageBox.Show(errorMessage, "Ошибка отправки заказа в Гемотест",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                order.State = OrderState.Sended;
                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                MessageBox.Show(ex.Message, "Ошибка подготовки заказа",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            group.BiomaterialsSelected = group.BiomaterialsSelected ?? new List<BiomaterialInfoForGUI>();

            if (details.Products == null || productIndex < 0 || productIndex >= details.Products.Count)
                return group;

            var productDetail = details.Products[productIndex];

            var linkedBioms = details.BioMaterials
                .Where(b => b.Mandatory.Contains(productIndex) || b.Chosen.Contains(productIndex) || b.Another.Contains(productIndex))
                .ToList();


            foreach (var biom in linkedBioms)
            {
                var info = new BiomaterialInfoForGUI
                {
                    BiomaterialId = biom.Id,
                    BiomaterialCode = biom.Code,
                    BiomaterialName = biom.Name
                };

                var transport = ResolveTransport(productDetail.ProductId, biom.Id);

                if (transport != null)
                {
                    info.ContainerId = transport.id;
                    info.ContainerCode = transport.id;
                    info.ContainerName = transport.name;
                }
                else
                {
                    info.ContainerId = "";
                    info.ContainerCode = "";
                    info.ContainerName = "";
                }

                group.Biomaterials.Add(info);
            }

            bool isMarketingComplex = IsMarketingComplex(productDetail.ProductId);


            if (isMarketingComplex)
            {
                group.SelectOnlyOne = false;
                group.BiomaterialsSelected.Clear();
                foreach (var bi in group.Biomaterials)
                    group.BiomaterialsSelected.Add(bi);
                return group;
            }


            group.SelectOnlyOne = true;
            group.BiomaterialsSelected.Clear();


            var mandatory = linkedBioms.Where(b => b.Mandatory.Contains(productIndex)).ToList();
            if (mandatory.Count > 0)
            {

                var mand = mandatory[0];
                var mandInfo = group.Biomaterials.FirstOrDefault(x => x.BiomaterialId == mand.Id);
                if (mandInfo != null)
                    group.BiomaterialsSelected.Add(mandInfo);
                return group;
            }


            if (group.Biomaterials.Count == 1)
            {
                group.BiomaterialsSelected.Add(group.Biomaterials[0]);
                return group;
            }


            if (group.Biomaterials.Count > 1)
                group.BiomaterialsSelected.Add(group.Biomaterials[0]);

            return group;
        }

        private DictionaryTransport ResolveTransport(string serviceId, string biomaterialId)
        {
            DictionaryTransport transport = null;

            if (!string.IsNullOrEmpty(serviceId) &&
                laboratory.Dicts.ServiceParameters != null &&
                laboratory.Dicts.ServiceParameters.TryGetValue(serviceId, out var paramsList) &&
                paramsList != null && paramsList.Count > 0)
            {
                var param = paramsList.FirstOrDefault(p =>
                    p != null &&
                    string.Equals(p.service_id, serviceId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.biomaterial_id ?? "", biomaterialId ?? "", StringComparison.OrdinalIgnoreCase));

                if (param != null && !string.IsNullOrEmpty(param.transport_id))
                {
                    laboratory.Dicts.Transport.TryGetValue(param.transport_id, out transport);
                }
            }

            if (transport == null && !string.IsNullOrEmpty(serviceId))
            {
                if (laboratory.Dicts.Directory != null && laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) && svc != null)
                {
                    if (!string.IsNullOrEmpty(svc.transport_id))
                        laboratory.Dicts.Transport.TryGetValue(svc.transport_id, out transport);
                }
            }

            return transport;
        }

        private bool IsMarketingComplex(string serviceId)
        {

            if (string.IsNullOrEmpty(serviceId))
                return false;

            return laboratory.Dicts.MarketingComplexByComplexId != null &&
                   laboratory.Dicts.MarketingComplexByComplexId.ContainsKey(serviceId);
        }


        private void PrintServiceMetaToConsole(string serviceId)
        {
            if (string.IsNullOrEmpty(serviceId))
                return;

            if (laboratory.Dicts.Directory == null || !laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) || svc == null)
            {
                Console.WriteLine($"[ServiceMeta] {serviceId}: not found in directory");
                return;
            }

            string kind = "обычная";
            if (IsMarketingComplex(serviceId))
                kind = "маркетинговый комплекс (МК)";


            bool isMicro = false;
            if (laboratory.Dicts.SamplesServices != null &&
                laboratory.Dicts.SamplesServices.TryGetValue(serviceId, out var ssList) &&
                ssList != null)
            {
                isMicro = ssList.Any(s => s != null && !string.IsNullOrEmpty(s.microbiology_biomaterial_id));
            }

            if (isMicro)
                kind = "микробиология";

            Console.WriteLine($"[ServiceMeta] id={svc.id} name={svc.name}");
            Console.WriteLine($"            type={svc.type} service_type={(svc.service_type.HasValue ? svc.service_type.Value.ToString() : "null")} kind={kind}");
            Console.WriteLine($"            passport_required={svc.is_passport_required} address_required={svc.is_address_required}");
        }


        private bool EnsureSupplementalsIfNeeded(Order order, OrderModelForGUI model)
        {
            try
            {
                var details = order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    return true;


                var serviceIds = model.ProductsInfo?.Where(p => !string.IsNullOrEmpty(p.Id)).Select(p => p.Id).ToList()
                                ?? new List<string>();


                return SupplementalsWorkflow.EnsureSupplementals(details, null, serviceIds);
            }
            catch
            {


                return true;
            }
        }


        private bool EnsureAdditionalPatientInfo(Order order, OrderModelForGUI model)
        {
            bool needPassport = false;
            bool needAddress = false;
            bool needSnils = false;

            foreach (var p in model.ProductsInfo)
            {
                if (laboratory.Dicts.Directory == null || !laboratory.Dicts.Directory.TryGetValue(p.Id, out var svc) || svc == null)
                    continue;

                if (svc.is_passport_required)
                    needPassport = true;
                if (svc.is_address_required)
                    needAddress = true;
            }

            if (!needPassport && !needAddress && !needSnils)
                return true;

            using (var form = new FormAdditionalPatientInfo(order, needPassport, needAddress, needSnils))
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return false;
            }

            return true;
        }


        private void ApplyAutoInsertServices(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details == null || details.Products == null)
                return;

            if (laboratory.Dicts.ServiceAutoInsert == null || laboratory.Dicts.ServiceAutoInsert.Count == 0)
                return;

            var existingServiceIds = new HashSet<string>(
                details.Products.Where(p => !string.IsNullOrEmpty(p.ProductId)).Select(p => p.ProductId),
                StringComparer.OrdinalIgnoreCase
            );

            if (AllProducts == null || AllProducts.Count == 0)
                return;


            foreach (var sid in existingServiceIds.ToList())
            {
                if (!laboratory.Dicts.ServiceAutoInsert.TryGetValue(sid, out var rules) || rules == null || rules.Count == 0)
                    continue;

                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    if (rule == null) continue;
                    if (rule.archive != 0) continue;

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

        private List<DictionaryBiomaterials> ResolveBiomaterialsForService(DictionaryService service)
        {
            var result = new List<DictionaryBiomaterials>();
            if (service == null || laboratory?.Dicts == null)
                return result;

            var dicts = laboratory.Dicts;
            if (!string.IsNullOrEmpty(service.biomaterial_id) &&
                !string.Equals(service.biomaterial_id, "Drugoe", StringComparison.OrdinalIgnoreCase))
            {
                if (dicts.Biomaterials != null &&
                    dicts.Biomaterials.TryGetValue(service.biomaterial_id, out var biom) && biom != null)
                {
                    result.Add(biom);
                }
            }
            if (service.service_type == 0 && dicts.ServiceParameters != null)
            {
                if (dicts.ServiceParameters.TryGetValue(service.id, out var paramsList) &&
                    paramsList != null && paramsList.Count > 0)
                {
                    var ids = paramsList
                        .Select(p => p?.biomaterial_id)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var id in ids)
                    {
                        if (dicts.Biomaterials != null &&
                            dicts.Biomaterials.TryGetValue(id, out var biom) && biom != null &&
                            !result.Any(r => string.Equals(r.id, biom.id, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Add(biom);
                        }
                    }
                }
            }
            if (service.service_type == 1 || service.service_type == 2)
            {
                List<DictionaryMarketingComplex> complexItems = null;

                if (service.service_type == 2)
                {
                    dicts.MarketingComplexByComplexId?.TryGetValue(service.id, out complexItems);
                }
                else
                {
                    dicts.MarketingComplexByServiceId?.TryGetValue(service.id, out complexItems);
                }

                if (complexItems != null && complexItems.Count > 0)
                {
                    var ids = complexItems
                        .Select(m => m?.biomaterial_id)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var id in ids)
                    {
                        if (dicts.Biomaterials != null &&
                            dicts.Biomaterials.TryGetValue(id, out var biom) && biom != null &&
                            !result.Any(r => string.Equals(r.id, biom.id, StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Add(biom);
                        }
                    }
                }
            }

            if (string.Equals(service.biomaterial_id, "Drugoe", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(service.other_biomaterial))
            {
                if (!result.Any(b => string.Equals(b.id, "Drugoe", StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(new DictionaryBiomaterials
                    {
                        id = "Drugoe",
                        name = service.other_biomaterial,
                        archive = 0
                    });
                }
            }

            return result;
        }

        private BiomaterialGroupForGUI BuildBiomaterialGroupForService(string serviceId)
        {
            var group = new BiomaterialGroupForGUI();
            group.BiomaterialsSelected = group.BiomaterialsSelected ?? new List<BiomaterialInfoForGUI>();

            if (string.IsNullOrEmpty(serviceId) || laboratory?.Dicts?.Directory == null)
                return group;

            if (!laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) || svc == null)
                return group;

            var bioms = ResolveBiomaterialsForService(svc);
            foreach (var biom in bioms)
            {
                if (biom == null || string.IsNullOrEmpty(biom.id))
                    continue;

                var info = new BiomaterialInfoForGUI
                {
                    BiomaterialId = biom.id,
                    BiomaterialCode = biom.id,
                    BiomaterialName = biom.name
                };

                var transport = ResolveTransport(serviceId, biom.id);
                if (transport != null)
                {
                    info.ContainerId = transport.id;
                    info.ContainerCode = transport.id;
                    info.ContainerName = transport.name;
                }
                else
                {
                    info.ContainerId = "";
                    info.ContainerCode = "";
                    info.ContainerName = "";
                }

                group.Biomaterials.Add(info);
            }

            if (IsMarketingComplex(serviceId))
            {
                group.SelectOnlyOne = false;
                group.BiomaterialsSelected.Clear();
                foreach (var b in group.Biomaterials)
                    group.BiomaterialsSelected.Add(b);
                return group;
            }

            group.SelectOnlyOne = true;
            group.BiomaterialsSelected.Clear();
            if (group.Biomaterials.Count > 0)
                group.BiomaterialsSelected.Add(group.Biomaterials[0]);

            return group;
        }

        private void PrepareBiomaterialsForChooseForm(List<ProductInfoForGUI> products)
        {
            if (products == null || laboratory?.Dicts == null)
                return;

            foreach (var p in products)
            {
                p.BiomaterialGroups?.Clear();
                p.BiomaterialGroups?.Add(BuildBiomaterialGroupForService(p.Id));
            }
        }

        private static List<string> GetSelectedBiomaterialIds(ProductInfoForGUI p)
        {
            var g = p?.BiomaterialGroups?.FirstOrDefault();
            if (g?.BiomaterialsSelected == null)
                return new List<string>();

            return g.BiomaterialsSelected
                .Where(x => x != null && !string.IsNullOrEmpty(x.BiomaterialId))
                .Select(x => x.BiomaterialId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ApplySelectedBiomaterialsToAddedProduct(
            Order order,
            OrderModelForGUI model,
            int productIndex,
            List<string> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
                return;

            var details = order?.OrderDetail as GemotestOrderDetail;
            if (details == null)
                return;

            bool hasMandatory = details.BioMaterials != null &&
                                details.BioMaterials.Any(b => b?.Mandatory != null && b.Mandatory.Contains(productIndex));
            if (hasMandatory)
                return;

            var prod = model?.ProductsInfo?.ElementAtOrDefault(productIndex);
            var group = prod?.BiomaterialGroups?.FirstOrDefault();
            if (group?.Biomaterials == null)
                return;

            var chosen = group.Biomaterials
                .Where(b => b != null && selectedIds.Contains(b.BiomaterialId, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (chosen.Count == 0)
                return;

            group.BiomaterialsSelected = group.BiomaterialsSelected ?? new List<BiomaterialInfoForGUI>();
            group.BiomaterialsSelected.Clear();

            if (group.SelectOnlyOne)
            {
                group.BiomaterialsSelected.Add(chosen[0]);
            }
            else
            {
                foreach (var b in chosen)
                    group.BiomaterialsSelected.Add(b);
            }

            SaveOrderModelForGUIToDetails(order, model);
        }

        public bool ValidateGUIModel(OrderModelForGUI _Model) => true;
        public bool PrintLaboratoryDocument(Order _Order, ref ResultsCollection _ResultsCollection, DocumentInfoForGUI _DocumentInfo, bool _Preview) => true;
        public void ShowOrderDetail(Order _Order) { }
        public bool PrintStikers(Order _Order, List<SampleInfoForGUI> _SelectedSamples) => false;

        public bool GenerateSamples(Order _Order, OrderModelForGUI _Model, bool _force) => true;
    }
}
