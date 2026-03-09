using Laboratory.Gemotest.Options;
using SiMed.Laboratory;
using Laboratory.Gemotest.SourseClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public bool GetGuiOptions(out List<GuiOption> _Options)
        {
            _Options = new List<GuiOption>
            {
                new GuiOption() { OptionName = eGuiOptionName.CanAddProduct },
                new GuiOption() { OptionName = eGuiOptionName.CanRemoveProduct },
                new GuiOption() { OptionName = eGuiOptionName.CanCheckResultsIfCommited }
            };
            return true;
        }

        public bool GenerateSamples(Order _Order, OrderModelForGUI _Model)
        {
            LastException = null;
            try
            {
                var samples = new List<SampleInfoForGUI>();

                foreach (var productInfo in _Model.ProductsInfo)
                {
                    if (productInfo?.BiomaterialGroups == null)
                        continue;

                    foreach (var biomGroupInfo in productInfo.BiomaterialGroups)
                    {
                        if (biomGroupInfo?.BiomaterialsSelected == null)
                            continue;

                        foreach (var biomInfo in biomGroupInfo.BiomaterialsSelected)
                        {
                            if (biomInfo == null)
                                continue;

                            var sameCodeProducts = _Model.ProductsInfo
                                .Where(x => x.Code == productInfo.Code)
                                .Select(x => x.OrderProductGuid)
                                .ToList();

                            SampleInfoForGUI sampleFind = null;

                            foreach (var sample in samples)
                            {
                                if (sample.Biomaterial == null)
                                    continue;

                                if (!string.Equals(sample.Biomaterial.BiomaterialCode, biomInfo.BiomaterialCode, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                if (!string.Equals(sample.Biomaterial.ContainerCode, biomInfo.ContainerCode, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                if (sample.OrderProductGuids.Any(x => sameCodeProducts.Contains(x)))
                                    continue;

                                sampleFind = sample;
                                break;
                            }

                            if (sampleFind == null)
                            {
                                var sampleNew = new SampleInfoForGUI
                                {
                                    OrderSampleGuid = Guid.NewGuid().ToString(),
                                    Biomaterial = biomInfo
                                };
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

                // Сохранить старые guid/barcode, если образец по сути тот же
                foreach (var sampleNew in samples)
                {
                    foreach (var oldSample in _Model.Samples)
                    {
                        if (oldSample?.Biomaterial == null)
                            continue;

                        if (!string.Equals(oldSample.Biomaterial.BiomaterialCode, sampleNew.Biomaterial.BiomaterialCode, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.Equals(oldSample.Biomaterial.ContainerCode, sampleNew.Biomaterial.ContainerCode, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var oldSet = string.Join(",", oldSample.OrderProductGuids.OrderBy(x => x));
                        var newSet = string.Join(",", sampleNew.OrderProductGuids.OrderBy(x => x));

                        if (oldSet != newSet)
                            continue;

                        sampleNew.OrderSampleGuid = oldSample.OrderSampleGuid;
                        sampleNew.Barcode = oldSample.Barcode;
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

        public bool GenerateFields(Order _Order, OrderModelForGUI _Model)
        {
            LastException = null;
            try
            {
                var details = _Order?.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    return true;

                var fields = new List<FieldInfoForGUI>();

                foreach (var productInfo in _Model.ProductsInfo)
                {
                    if (productInfo == null || string.IsNullOrWhiteSpace(productInfo.Id))
                        continue;

                    if (laboratory?.Dicts?.ServicesSupplementals == null)
                        continue;

                    if (!laboratory.Dicts.ServicesSupplementals.TryGetValue(productInfo.Id, out var supplementals) || supplementals == null)
                        continue;

                    foreach (var supp in supplementals)
                    {
                        if (supp == null)
                            continue;

                        string id = (supp.test_id ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        string description = string.IsNullOrWhiteSpace(supp.name) ? id : supp.name.Trim();
                        bool isDictionary = !string.IsNullOrWhiteSpace(supp.value);

                        AddSupplementalFieldIfNotExists(
                            fields,
                            id,
                            description,
                            productInfo.OrderProductGuid,
                            supp.required,
                            isDictionary ? FieldDataType.Dictionary : FieldDataType.Text,
                            supp.value);
                    }
                }

                if (details.Details != null)
                {
                    foreach (var detail in details.Details)
                    {
                        if (detail == null)
                            continue;

                        string fieldId = GetDetailKey(detail);
                        if (string.IsNullOrWhiteSpace(fieldId))
                            continue;

                        FieldInfoForGUI field = fields.FirstOrDefault(x => string.Equals(x.Id, fieldId, StringComparison.OrdinalIgnoreCase));
                        if (field == null)
                        {
                            field = new FieldInfoForGUI()
                            {
                                Id = fieldId,
                                Description = string.IsNullOrWhiteSpace(detail.Name) ? fieldId : detail.Name,
                                Mandatory = detail.MandatoryProducts != null && detail.MandatoryProducts.Count > 0,
                                Regex = detail.regex,
                                FieldDataType = FieldDataType.Text
                            };

                            foreach (string guid in ConvertProductIndexesToOrderGuids(details, detail))
                            {
                                if (!field.OrderProductGuidList.Contains(guid))
                                    field.OrderProductGuidList.Add(guid);
                            }

                            fields.Add(field);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(detail.regex) && string.IsNullOrWhiteSpace(field.Regex))
                                field.Regex = detail.regex;

                            if (detail.MandatoryProducts != null && detail.MandatoryProducts.Count > 0)
                                field.Mandatory = true;

                            foreach (string guid in ConvertProductIndexesToOrderGuids(details, detail))
                            {
                                if (!field.OrderProductGuidList.Contains(guid))
                                    field.OrderProductGuidList.Add(guid);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(detail.Value) || !string.IsNullOrWhiteSpace(detail.DisplayValue))
                        {
                            field.Value = detail.Value;
                            field.DisplayValue = string.IsNullOrWhiteSpace(detail.DisplayValue) ? detail.Value : detail.DisplayValue;
                        }
                    }
                }

                _Model.Fields = fields.OrderBy(x => x.Description).ToList();
                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
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
                var details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    return true;

                details.Dicts = laboratory.Dicts;

                FillPriceListsForModel(_ReadOnly, details, _Model);

                foreach (var product in details.Products)
                {
                    var p = new ProductInfoForGUI
                    {
                        OrderProductGuid = string.IsNullOrWhiteSpace(product.OrderProductGuid)
                            ? details.Products.IndexOf(product).ToString()
                            : product.OrderProductGuid,
                        Id = product.ProductId,
                        Code = product.ProductCode,
                        Name = product.ProductName,
                        ProductGroupGuid = null
                    };

                    PrintServiceMetaToConsole(p.Id);
                    _Model.ProductsInfo.Add(p);
                }

                RebuildBiomaterialGroups(details, _Model);

                if (!GenerateSamples(_Order, _Model))
                    return false;

                if (!GenerateFields(_Order, _Model))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return false;
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

                SavePriceListToDetails(details, _Model);

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

                SaveFieldsToDetails(details, _Model);
                details.DeleteObsoleteDetails();

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
                    Name = prod.Name,
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

                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

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
                            Name = prod.Name,
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
                        ProductName = productNew.Name
                    });

                    ApplyAutoInsertServices(details, _OrderModel);

                    details.AddBiomaterialsFromProducts();
                    RebuildBiomaterialGroups(details, _OrderModel);
                    ApplySelectedBiomaterialsToAddedProduct(_Order, _OrderModel, newIndex, selectedBioIds);

                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

                    PrintServiceMetaToConsole(productNew.Id);
                    return true;
                }

                if (_Action == eOrderAction.PrepareOrderForSend)
                {
                    if (!ValidateGUIModel(_OrderModel))
                        return false;

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

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

                string contractorCode = (details.PriceListCode ?? string.Empty).Trim();
                if (contractorCode.Length == 0)
                {
                    int plCount = globalOptions.PriceLists != null ? globalOptions.PriceLists.Count : 0;
                    if (plCount > 1)
                        throw new InvalidOperationException("Прайс-лист не выбран. Выберите контрагента перед отправкой заказа.");

                    contractorCode = (globalOptions.Contractor_Code ?? string.Empty).Trim();
                }

                var sender = new GemotestOrderSender(
                    globalOptions.UrlAdress,
                    contractorCode,
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

        private void FillPriceListsForModel(bool readOnly, GemotestOrderDetail details, OrderModelForGUI model)
        {
            model.PriceLists.Clear();
            model.PriceListSelected = null;

            if (details == null)
                return;

            if (readOnly)
            {
                string roName = !string.IsNullOrWhiteSpace(details.PriceListName)
                    ? details.PriceListName
                    : (details.PriceList ?? "");

                string roId = !string.IsNullOrWhiteSpace(details.PriceListCode)
                    ? details.PriceListCode
                    : roName;

                if (!string.IsNullOrWhiteSpace(roName) || !string.IsNullOrWhiteSpace(roId))
                {
                    model.PriceLists.Add(new PriceListForGUI
                    {
                        Id = roId ?? "",
                        Name = roName ?? ""
                    });
                    model.PriceListSelected = model.PriceLists[0];
                }

                return;
            }

            if (globalOptions != null && globalOptions.PriceLists != null && globalOptions.PriceLists.Count > 0)
            {
                // Если прайсов больше одного — первым идет "не определен"
                if (globalOptions.PriceLists.Count > 1)
                {
                    model.PriceLists.Add(new PriceListForGUI
                    {
                        Id = "",
                        Name = "не определен"
                    });

                    model.PriceListSelected = model.PriceLists[0];
                }

                for (int i = 0; i < globalOptions.PriceLists.Count; i++)
                {
                    var pl = globalOptions.PriceLists[i];
                    if (pl == null) continue;

                    model.PriceLists.Add(new PriceListForGUI
                    {
                        Id = i.ToString(),
                        Name = pl.Name ?? ""
                    });
                }

                // Восстановление ранее сохраненного выбора
                if (!string.IsNullOrWhiteSpace(details.PriceListCode))
                {
                    for (int i = 0; i < globalOptions.PriceLists.Count; i++)
                    {
                        var pl = globalOptions.PriceLists[i];
                        if (pl == null) continue;

                        if (string.Equals((pl.ContractorCode ?? "").Trim(),
                                          details.PriceListCode.Trim(),
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            model.PriceListSelected = model.PriceLists.FirstOrDefault(x => x.Id == i.ToString());
                            break;
                        }
                    }
                }
                else if (globalOptions.PriceLists.Count == 1)
                {
                    // Автовыбор допустим только если прайс один
                    model.PriceListSelected = model.PriceLists.FirstOrDefault(x => x.Id == "0");
                }

                return;
            }

            // фолбэк на старую single-contractor схему
            if (!string.IsNullOrWhiteSpace(globalOptions?.Contractor) ||
                !string.IsNullOrWhiteSpace(globalOptions?.Contractor_Code))
            {
                model.PriceLists.Add(new PriceListForGUI
                {
                    Id = globalOptions?.Contractor_Code ?? "",
                    Name = globalOptions?.Contractor ?? ""
                });
                model.PriceListSelected = model.PriceLists[0];
            }
        }
        private void SavePriceListToDetails(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details == null)
                return;

            details.PriceList = "";
            details.PriceListName = "";
            details.PriceListCode = "";

            PriceListForGUI selected = model != null ? model.PriceListSelected : null;
            if (selected == null)
                return;

            // placeholder "не определен"
            if (string.IsNullOrWhiteSpace(selected.Id))
                return;

            int idx;
            if (int.TryParse(selected.Id, out idx) &&
                globalOptions != null &&
                globalOptions.PriceLists != null &&
                idx >= 0 &&
                idx < globalOptions.PriceLists.Count)
            {
                var pl = globalOptions.PriceLists[idx];
                if (pl != null)
                {
                    details.PriceList = pl.Name ?? "";
                    details.PriceListName = pl.Name ?? "";
                    details.PriceListCode = pl.ContractorCode ?? "";
                    return;
                }
            }

            if (globalOptions != null && globalOptions.PriceLists != null && globalOptions.PriceLists.Count > 0)
            {
                var pl = globalOptions.PriceLists.FirstOrDefault(x =>
                    x != null &&
                    string.Equals((x.Name ?? "").Trim(),
                                  (selected.Name ?? "").Trim(),
                                  StringComparison.OrdinalIgnoreCase));

                if (pl != null)
                {
                    details.PriceList = pl.Name ?? "";
                    details.PriceListName = pl.Name ?? "";
                    details.PriceListCode = pl.ContractorCode ?? "";
                    return;
                }
            }

            details.PriceList = selected.Name ?? "";
            details.PriceListName = selected.Name ?? "";
            details.PriceListCode = selected.Id ?? "";
        }
        private void SaveFieldsToDetails(GemotestOrderDetail details, OrderModelForGUI model)
        {
            details.Details.Clear();

            if (model?.Fields == null)
                return;

            foreach (var field in model.Fields)
            {
                if (field == null)
                    continue;

                var d = new GemotestDetail
                {
                    Code = field.Id,
                    Name = field.Description,
                    Value = field.Value,
                    DisplayValue = string.IsNullOrWhiteSpace(field.DisplayValue) ? field.Value : field.DisplayValue,
                    regex = field.Regex,
                    isStdField = false
                };

                if (field.OrderProductGuidList != null)
                {
                    foreach (var guid in field.OrderProductGuidList)
                    {
                        int idx = details.Products.FindIndex(p => p.OrderProductGuid == guid);
                        if (idx < 0)
                            continue;

                        if (field.Mandatory)
                        {
                            if (!d.MandatoryProducts.Contains(idx))
                                d.MandatoryProducts.Add(idx);
                        }
                        else
                        {
                            if (!d.OptionalProducts.Contains(idx))
                                d.OptionalProducts.Add(idx);
                        }
                    }
                }

                details.Details.Add(d);
            }
        }

        private string GetDetailKey(GemotestDetail detail)
        {
            if (detail == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(detail.Code))
                return detail.Code.Trim();

            if (!string.IsNullOrWhiteSpace(detail.Name))
                return detail.Name.Trim();

            return detail.ID > 0 ? $"DETAIL_{detail.ID}" : string.Empty;
        }

        private IEnumerable<string> ConvertProductIndexesToOrderGuids(GemotestOrderDetail details, GemotestDetail detail)
        {
            var result = new List<string>();

            if (details?.Products == null || detail == null)
                return result;

            var indexes = new List<int>();
            if (detail.MandatoryProducts != null)
                indexes.AddRange(detail.MandatoryProducts);
            if (detail.OptionalProducts != null)
                indexes.AddRange(detail.OptionalProducts);

            foreach (var idx in indexes.Distinct())
            {
                if (idx < 0 || idx >= details.Products.Count)
                    continue;

                string guid = details.Products[idx].OrderProductGuid;
                if (!string.IsNullOrWhiteSpace(guid))
                    result.Add(guid);
            }

            return result;
        }

        private void AddSupplementalFieldIfNotExists(
            List<FieldInfoForGUI> fields,
            string id,
            string description,
            string orderProductGuid,
            bool mandatory,
            FieldDataType fieldDataType,
            string rawDictionaryValues)
        {
            FieldInfoForGUI field = fields.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                field = new FieldInfoForGUI()
                {
                    Id = id,
                    Description = description,
                    Mandatory = mandatory,
                    FieldDataType = fieldDataType
                };

                if (!string.IsNullOrWhiteSpace(orderProductGuid))
                    field.OrderProductGuidList.Add(orderProductGuid);

                if (fieldDataType == FieldDataType.Dictionary)
                    field.DictionaryValues = BuildDictionaryValues(rawDictionaryValues);

                fields.Add(field);
            }
            else
            {
                if (mandatory)
                    field.Mandatory = true;

                if (!string.IsNullOrWhiteSpace(orderProductGuid) && !field.OrderProductGuidList.Contains(orderProductGuid))
                    field.OrderProductGuidList.Add(orderProductGuid);

                if (fieldDataType == FieldDataType.Dictionary)
                {
                    field.FieldDataType = FieldDataType.Dictionary;
                    MergeDictionaryValues(field, rawDictionaryValues);
                }
            }
        }

        private List<FieldDictionaryValue> BuildDictionaryValues(string rawDictionaryValues)
        {
            var result = new List<FieldDictionaryValue>();
            if (string.IsNullOrWhiteSpace(rawDictionaryValues))
                return result;

            foreach (var item in rawDictionaryValues
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                result.Add(new FieldDictionaryValue()
                {
                    Value = item,
                    DisplayText = item
                });
            }

            return result;
        }

        private void MergeDictionaryValues(FieldInfoForGUI field, string rawDictionaryValues)
        {
            if (field == null || string.IsNullOrWhiteSpace(rawDictionaryValues))
                return;

            field.DictionaryValues = field.DictionaryValues ?? new List<FieldDictionaryValue>();
            var existing = new HashSet<string>(
                field.DictionaryValues.Where(x => x != null).Select(x => x.Value ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);

            foreach (var item in BuildDictionaryValues(rawDictionaryValues))
            {
                if (item == null)
                    continue;

                if (existing.Add(item.Value ?? string.Empty))
                    field.DictionaryValues.Add(item);
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
                    info.ContainerId = string.Empty;
                    info.ContainerCode = string.Empty;
                    info.ContainerName = string.Empty;
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
                    string.Equals(p.biomaterial_id ?? string.Empty, biomaterialId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

                if (param != null && !string.IsNullOrEmpty(param.transport_id))
                    laboratory.Dicts.Transport.TryGetValue(param.transport_id, out transport);
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
                    dicts.MarketingComplexByComplexId?.TryGetValue(service.id, out complexItems);
                else
                    dicts.MarketingComplexByServiceId?.TryGetValue(service.id, out complexItems);

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
                    info.ContainerId = string.Empty;
                    info.ContainerCode = string.Empty;
                    info.ContainerName = string.Empty;
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
                group.BiomaterialsSelected.Add(chosen[0]);
            else
                foreach (var b in chosen)
                    group.BiomaterialsSelected.Add(b);

            SaveOrderModelForGUIToDetails(order, model);
        }

        public bool ValidateGUIModel(OrderModelForGUI _Model)
        {
            LastException = null;
            try
            {
                _Model.Errors.Clear();

                if (_Model.PriceLists != null &&
                    _Model.PriceLists.Count > 1 &&
                    (_Model.PriceListSelected == null || string.IsNullOrWhiteSpace(_Model.PriceListSelected.Id)))
                {
                    _Model.Errors.Add(new ErrorMessage()
                    {
                        NeedSelectPriceList = true,
                        ErrorText = "Выберите прайс-лист"
                    });
                }

                if (_Model.Fields != null)
                {
                    foreach (var field in _Model.Fields)
                    {
                        if (field == null)
                            continue;

                        if (field.Mandatory && string.IsNullOrWhiteSpace(field.Value))
                        {
                            _Model.Errors.Add(new ErrorMessage()
                            {
                                ErrorText = $"Поле '{field.Description}' обязательно для заполнения"
                            });
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(field.Value) && !string.IsNullOrWhiteSpace(field.Regex))
                        {
                            var regex = new Regex(field.Regex);
                            if (!regex.IsMatch(field.Value))
                            {
                                _Model.Errors.Add(new ErrorMessage()
                                {
                                    ErrorText = $"Поле '{field.Description}' не соответствует формату '{field.Regex}'"
                                });
                            }
                        }
                    }
                }

                return _Model.Errors.Count == 0;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }
        public bool PrintLaboratoryDocument(Order _Order, ref ResultsCollection _ResultsCollection, DocumentInfoForGUI _DocumentInfo, bool _Preview) => true;
        public void ShowOrderDetail(Order _Order) { }
        public bool PrintStikers(Order _Order, List<SampleInfoForGUI> _SelectedSamples) => false;
    }
}
