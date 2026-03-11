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
using SiMed.Clinic;

namespace Laboratory.Gemotest
{
    public class LaboratoryGemotestGUI : ILaboratoryGUI
    {
        private LaboratoryGemotest laboratory;
        private LocalOptionsGemotest localOptions;
        private OptionsGemotest globalOptions;
        private ProductsCollection AllProducts;
        private INumerator numerator;

        public bool SetOptions(LaboratoryGemotest lab, ProductsCollection products, LocalOptionsGemotest local, OptionsGemotest global, INumerator _Numerator)
        {
            numerator = _Numerator;
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
                                FieldDataType = FieldDataType.Text,
                            };

                            foreach (var idx in (detail.MandatoryProducts ?? new List<int>()).Concat(detail.OptionalProducts ?? new List<int>()).Distinct())
                            {
                                if (idx >= 0 && idx < details.Products.Count)
                                    field.OrderProductGuidList.Add(details.Products[idx].OrderProductGuid);
                            }

                            fields.Add(field);
                        }

                        field.Value = detail.Value ?? string.Empty;
                        field.DisplayValue = string.IsNullOrWhiteSpace(detail.DisplayValue) ? (detail.Value ?? string.Empty) : detail.DisplayValue;
                        if (!string.IsNullOrWhiteSpace(detail.regex))
                            field.Regex = detail.regex;
                    }
                }

                _Model.Fields = fields.OrderBy(x => x.Description).ThenBy(x => x.Id).ToList();
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
            LastException = null;
            try
            {
                _Model.Errors.Clear();

               

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
                                Field = field,
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
                                    Field = field,
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

                details.BioMaterials.Clear();
                details.AddBiomaterialsFromProducts();

                ApplyBiomaterialSelectionFromModel(details, _Model);

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

        public bool ProcessOrderGUIAction(eOrderAction _Action, Order _Order, ref OrderModelForGUI _OrderModel, ProductInfoForGUI _Product)
        {
            LastException = null;

            try
            {
                if (_Order == null)
                    throw new InvalidOperationException("Заказ не задан.");

                var details = _Order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                details.Dicts = laboratory.Dicts;

                if ((_Action == eOrderAction.AddProduct || _Action == eOrderAction.RemoveProduct) &&
                    _Order.State != OrderState.NotSended)
                {
                    MessageBox.Show(
                        "Из уже подготовленного или отправленного заказа нельзя добавлять или удалять услуги.",
                        "Гемотест",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return false;
                }

                if (_Action == eOrderAction.RemoveProduct)
                {
                    if (_OrderModel?.ProductsInfo == null || _OrderModel.ProductsInfo.Count == 0 || _Product == null)
                        return false;

                    int productIndex = -1;

                    if (!string.IsNullOrWhiteSpace(_Product.OrderProductGuid))
                    {
                        productIndex = _OrderModel.ProductsInfo.FindIndex(x =>
                            x != null &&
                            string.Equals(x.OrderProductGuid, _Product.OrderProductGuid, StringComparison.OrdinalIgnoreCase));
                    }

                    if (productIndex < 0)
                        productIndex = _OrderModel.ProductsInfo.IndexOf(_Product);

                    if (productIndex < 0 || productIndex >= _OrderModel.ProductsInfo.Count)
                        throw new IndexOutOfRangeException("Не удалось определить удаляемый продукт в модели заказа.");

                    string removedGuid = _OrderModel.ProductsInfo[productIndex].OrderProductGuid ?? string.Empty;

                    _OrderModel.ProductsInfo.RemoveAt(productIndex);

                    if (_OrderModel.Fields != null)
                    {
                        for (int i = _OrderModel.Fields.Count - 1; i >= 0; i--)
                        {
                            var field = _OrderModel.Fields[i];
                            if (field == null)
                                continue;

                            if (field.OrderProductGuidList != null && field.OrderProductGuidList.Count > 0)
                            {
                                field.OrderProductGuidList.RemoveAll(g =>
                                    string.Equals(g, removedGuid, StringComparison.OrdinalIgnoreCase));

                                if (field.OrderProductGuidList.Count == 0)
                                {
                                    _OrderModel.Fields.RemoveAt(i);
                                    continue;
                                }
                            }
                        }
                    }

                    for (int i = 0; i < _OrderModel.ProductsInfo.Count; i++)
                        _OrderModel.ProductsInfo[i].OrderProductGuid = i.ToString();

                    if (details.Products != null && productIndex >= 0 && productIndex < details.Products.Count)
                        details.DeleteProduct(productIndex);

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

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
                        if (laboratory.Dicts.Directory != null &&
                            laboratory.Dicts.Directory.TryGetValue(prod.ID, out var svc) &&
                            svc != null)
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

                    var productNew = products.Find(x =>
                        x != null &&
                        string.Equals(x.OrderProductGuid, form.selectedProductGuid, StringComparison.OrdinalIgnoreCase));

                    if (productNew == null)
                        return false;

                    var selectedBioIds = GetSelectedBiomaterialIds(productNew);

                    if (details.Products != null &&
                        details.Products.Any(p => string.Equals(p.ProductId, productNew.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show(
                            "Эта услуга уже есть в заказе.",
                            "Добавление услуги",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return false;
                    }

                    int newIndex = _OrderModel.ProductsInfo.Count;
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

                    for (int i = 0; i < _OrderModel.ProductsInfo.Count; i++)
                        _OrderModel.ProductsInfo[i].OrderProductGuid = i.ToString();

                    if (details.Products != null)
                    {
                        for (int i = 0; i < details.Products.Count; i++)
                            details.Products[i].OrderProductGuid = i.ToString();
                    }

                    details.BioMaterials.Clear();
                    details.AddBiomaterialsFromProducts();

                    RebuildBiomaterialGroups(details, _OrderModel);
                    ApplySelectedBiomaterialsToAddedProduct(_Order, _OrderModel, newIndex, selectedBioIds);

                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    PrintServiceMetaToConsole(productNew.Id);
                    return true;
                }

                if (_Action == eOrderAction.PrepareOrderForSend)
                {
                    if (_OrderModel != null &&
                        _OrderModel.PriceLists != null &&
                        _OrderModel.PriceLists.Count > 1 &&
                        (_OrderModel.PriceListSelected == null || string.IsNullOrWhiteSpace(_OrderModel.PriceListSelected.Id)))
                    {
                        MessageBox.Show(
                            "Сначала выберите прайс-лист.",
                            "Гемотест",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return false;
                    }

                    if (!ValidateGUIModel(_OrderModel))
                        return false;

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    details.DeleteObsoleteDetails();

                    if (string.IsNullOrWhiteSpace(_Order.Number))
                    {
                        if (numerator == null)
                            throw new InvalidOperationException("Не задан numerator для Gemotest.");

                        int nextNumber = numerator.GetNextNumber(
                            "GemotestOrderNum",
                            DateTime.Now,
                            "",
                            "",
                            ""
                        );

                        if (nextNumber <= 0)
                            throw new Exception("Не удалось получить номер заказа Gemotest через numerator.");

                        _Order.Number = "SiMed-" + nextNumber.ToString();
                    }

                    _Order.State = OrderState.Prepared;
                    
                    return true;
                }

                if (_Action == eOrderAction.CheckOrderState)
                {
                    if (_Order.State == OrderState.Sended)
                    {
                        _Order.State = OrderState.Commited;
                        return true;
                    }
                    return true;
                }

                if (_Action == eOrderAction.CancelOrder)
                {
                    _Order.State = OrderState.Canceled;
                    return true;
                }

                if (_Action == eOrderAction.RemoveService)
                {
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

        public bool PrintStikers(Order _Order, List<SampleInfoForGUI> _SelectedSamples)
        {
            return false;
        }

        public void ShowOrderDetail(Order _Order)
        {
            try
            {
                string text = _Order?.OrderDetail?.ToString() ?? string.Empty;
                MessageBox.Show(text, "Подробности заказа", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                LastException = exc;
            }
        }

        public bool PrintLaboratoryDocument(Order _Order, ref ResultsCollection _Results, DocumentInfoForGUI _Document, bool _Preview)
        {
            return true;
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
                    : (details.PriceList ?? string.Empty);

                string roId = !string.IsNullOrWhiteSpace(details.PriceListCode)
                    ? details.PriceListCode
                    : roName;

                if (!string.IsNullOrWhiteSpace(roName) || !string.IsNullOrWhiteSpace(roId))
                {
                    model.PriceLists.Add(new PriceListForGUI()
                    {
                        Id = roId ?? string.Empty,
                        Name = roName ?? string.Empty
                    });
                    model.PriceListSelected = model.PriceLists[0];
                }

                return;
            }

            if (globalOptions != null && globalOptions.PriceLists != null && globalOptions.PriceLists.Count > 0)
            {
                /*if (globalOptions.PriceLists.Count > 1)
                {
                    model.PriceLists.Add(new PriceListForGUI()
                    {
                        Id = string.Empty,
                        Name = "не определен"
                    });
                }*/

                for (int i = 0; i < globalOptions.PriceLists.Count; i++)
                {
                    var pl = globalOptions.PriceLists[i];
                    if (pl == null) continue;

                    model.PriceLists.Add(new PriceListForGUI()
                    {
                        Id = i.ToString(),
                        Name = pl.Name ?? string.Empty
                    });
                }

                if (!string.IsNullOrWhiteSpace(details.PriceListCode))
                {
                    for (int i = 0; i < globalOptions.PriceLists.Count; i++)
                    {
                        var pl = globalOptions.PriceLists[i];
                        if (pl == null) continue;

                        if (string.Equals((pl.ContractorCode ?? string.Empty).Trim(), details.PriceListCode.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            model.PriceListSelected = model.PriceLists.FirstOrDefault(x => x.Id == i.ToString());
                            break;
                        }
                    }
                }
                else if (globalOptions.PriceLists.Count == 1)
                {
                    model.PriceListSelected = model.PriceLists.FirstOrDefault(x => x.Id == "0");
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(globalOptions?.Contractor_Code))
            {
                model.PriceLists.Add(new PriceListForGUI()
                {
                    Id = globalOptions.Contractor_Code ?? string.Empty,
                    Name = globalOptions.Contractor ?? string.Empty
                });
                model.PriceListSelected = model.PriceLists[0];
            }
        }

        private void SavePriceListToDetails(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details == null)
                return;

            details.PriceList = string.Empty;
            details.PriceListName = string.Empty;
            details.PriceListCode = string.Empty;

            var selected = model?.PriceListSelected;
            if (selected == null)
                return;

            if (string.IsNullOrWhiteSpace(selected.Id))
                return;

            int idx;
            if (int.TryParse(selected.Id, out idx) &&
                globalOptions != null &&
                globalOptions.PriceLists != null &&
                idx >= 0 && idx < globalOptions.PriceLists.Count)
            {
                var pl = globalOptions.PriceLists[idx];
                if (pl != null)
                {
                    details.PriceList = pl.Name ?? string.Empty;
                    details.PriceListName = pl.Name ?? string.Empty;
                    details.PriceListCode = pl.ContractorCode ?? string.Empty;
                    return;
                }
            }

            var byName = globalOptions?.PriceLists?.FirstOrDefault(x =>
                x != null &&
                string.Equals((x.Name ?? string.Empty).Trim(), (selected.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

            if (byName != null)
            {
                details.PriceList = byName.Name ?? string.Empty;
                details.PriceListName = byName.Name ?? string.Empty;
                details.PriceListCode = byName.ContractorCode ?? string.Empty;
                return;
            }

            details.PriceList = selected.Name ?? string.Empty;
            details.PriceListName = selected.Name ?? string.Empty;
            details.PriceListCode = selected.Id ?? string.Empty;
        }

        private string GetDetailKey(GemotestDetail d)
        {
            if (d == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(d.Code))
                return d.Code;

            return !string.IsNullOrWhiteSpace(d.Name) ? d.Name : string.Empty;
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

        private void ApplyBiomaterialSelectionFromModel(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details == null || model == null || model.ProductsInfo == null || details.BioMaterials == null)
                return;

            for (int productIndex = 0; productIndex < model.ProductsInfo.Count; productIndex++)
            {
                var product = model.ProductsInfo[productIndex];
                var group = product?.BiomaterialGroups?.FirstOrDefault();

                var selectedIds = new HashSet<string>(
                    (group?.BiomaterialsSelected ?? new List<BiomaterialInfoForGUI>())
                        .Where(x => x != null && !string.IsNullOrWhiteSpace(x.BiomaterialId))
                        .Select(x => x.BiomaterialId),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var biom in details.BioMaterials.Where(b =>
                             b != null &&
                             (b.Mandatory.Contains(productIndex) ||
                              b.Chosen.Contains(productIndex) ||
                              b.Another.Contains(productIndex))))
                {
                    if (biom.Mandatory.Contains(productIndex))
                    {
                        if (!biom.Chosen.Contains(productIndex))
                            biom.Chosen.Add(productIndex);

                        biom.Another.Remove(productIndex);
                        continue;
                    }

                    biom.Chosen.Remove(productIndex);
                    biom.Another.Remove(productIndex);

                    if (selectedIds.Contains(biom.Id))
                        biom.Chosen.Add(productIndex);
                    else
                        biom.Another.Add(productIndex);
                }
            }
        }

        private void AddSupplementalFieldIfNotExists(
            List<FieldInfoForGUI> fields,
            string id,
            string description,
            string orderProductGuid,
            bool mandatory,
            FieldDataType fieldType,
            string rawDictionaryValues)
        {
            var field = fields.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                field = new FieldInfoForGUI()
                {
                    Id = id,
                    Description = description,
                    Mandatory = mandatory,
                    FieldDataType = fieldType
                };

                if (fieldType == FieldDataType.Dictionary)
                    field.DictionaryValues = BuildDictionaryValues(id, rawDictionaryValues);

                fields.Add(field);
            }
            else
            {
                if (mandatory)
                    field.Mandatory = true;

                if (field.FieldDataType != FieldDataType.Dictionary && fieldType == FieldDataType.Dictionary)
                    field.FieldDataType = FieldDataType.Dictionary;

                if (field.FieldDataType == FieldDataType.Dictionary)
                    MergeDictionaryValues(field, rawDictionaryValues);
            }

            if (!string.IsNullOrWhiteSpace(orderProductGuid) && !field.OrderProductGuidList.Contains(orderProductGuid))
                field.OrderProductGuidList.Add(orderProductGuid);
        }

        private List<FieldDictionaryValue> BuildDictionaryValues(string fieldId, string rawDictionaryValues)
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
                string display = item;

                if (string.Equals(fieldId, "Contingent", StringComparison.OrdinalIgnoreCase) &&
                    laboratory?.Dicts?.Contingents != null &&
                    laboratory.Dicts.Contingents.TryGetValue(item, out var mapped) &&
                    !string.IsNullOrWhiteSpace(mapped))
                {
                    display = mapped;
                }

                result.Add(new FieldDictionaryValue()
                {
                    Value = item,
                    DisplayText = display
                });
            }

            return result;
        }

        private void MergeDictionaryValues(FieldInfoForGUI field, string rawDictionaryValues)
        {
            if (field == null)
                return;

            field.DictionaryValues = field.DictionaryValues ?? new List<FieldDictionaryValue>();

            foreach (var item in BuildDictionaryValues(field.Id, rawDictionaryValues))
            {
                if (!field.DictionaryValues.Any(x => string.Equals(x.Value, item.Value, StringComparison.OrdinalIgnoreCase)))
                    field.DictionaryValues.Add(item);
            }
        }

        private void PrintServiceMetaToConsole(string serviceId)
        {
            try
            {
                if (laboratory?.Dicts?.Directory != null && laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) && svc != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Gemotest service: id={svc.id}; code={svc.code}; type={svc.type}; service_type={svc.service_type}");
                }
            }
            catch
            {
            }
        }

        private static string BuildBiomaterialDisplayName(string biomaterialName, string containerName)
        {
            biomaterialName = (biomaterialName ?? string.Empty).Trim();
            containerName = (containerName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(containerName) || containerName == "не указан")
                return biomaterialName;

            if (biomaterialName.IndexOf(containerName, StringComparison.OrdinalIgnoreCase) >= 0)
                return biomaterialName;

            return $"{biomaterialName} ({containerName})";
        }

        private static string NormalizeContainerName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == "-")
                return "не указан";

            return raw.Trim();
        }

        private List<DictionaryMarketingComplex> GetMarketingComplexItems(DictionaryService service)
        {
            var result = new List<DictionaryMarketingComplex>();

            if (service == null || laboratory?.Dicts == null)
                return result;

            if (laboratory.Dicts.MarketingComplexByComplexId != null &&
                laboratory.Dicts.MarketingComplexByComplexId.TryGetValue(service.id, out var byComplex) &&
                byComplex != null)
            {
                result.AddRange(byComplex.Where(x => x != null));
            }

            if (laboratory.Dicts.MarketingComplexByServiceId != null &&
                laboratory.Dicts.MarketingComplexByServiceId.TryGetValue(service.id, out var byService) &&
                byService != null)
            {
                foreach (var item in byService.Where(x => x != null))
                {
                    if (!result.Any(r =>
                        string.Equals(r.complex_id, item.complex_id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(r.service_id, item.service_id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(r.main_service, item.main_service, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        private void AddBiomaterialsFromBaseService(DictionaryService service, List<DictionaryBiomaterials> result)
        {
            if (service == null || laboratory?.Dicts == null || result == null)
                return;

            var dicts = laboratory.Dicts;

            if (dicts.ServiceParameters != null &&
                dicts.ServiceParameters.TryGetValue(service.id, out var parameters) &&
                parameters != null)
            {
                var ids = parameters
                    .Select(p => p?.biomaterial_id)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var id in ids)
                {
                    if (dicts.Biomaterials != null &&
                        dicts.Biomaterials.TryGetValue(id, out var biom) &&
                        biom != null &&
                        !result.Any(r => string.Equals(r.id, biom.id, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(biom);
                    }
                }
            }

            if (!string.IsNullOrEmpty(service.biomaterial_id) &&
                dicts.Biomaterials != null &&
                dicts.Biomaterials.TryGetValue(service.biomaterial_id, out var baseBiom) &&
                baseBiom != null &&
                !result.Any(r => string.Equals(r.id, baseBiom.id, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(baseBiom);
            }

            if (string.Equals(service.biomaterial_id, "Drugoe", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(service.other_biomaterial) &&
                !result.Any(r => string.Equals(r.id, "Drugoe", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(new DictionaryBiomaterials
                {
                    id = "Drugoe",
                    name = service.other_biomaterial,
                    archive = 0
                });
            }
        }

        private List<DictionaryBiomaterials> ResolveBiomaterialsForService(DictionaryService service)
        {
            var result = new List<DictionaryBiomaterials>();

            if (service == null || laboratory?.Dicts == null)
                return result;

            var dicts = laboratory.Dicts;
            var complexItems = GetMarketingComplexItems(service);

            if (complexItems.Count > 0)
            {
                var mcBiomIds = complexItems
                    .Select(m => m?.biomaterial_id)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var id in mcBiomIds)
                {
                    if (dicts.Biomaterials != null &&
                        dicts.Biomaterials.TryGetValue(id, out var biom) &&
                        biom != null &&
                        !result.Any(r => string.Equals(r.id, biom.id, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(biom);
                    }
                }

                if (result.Count > 0)
                    return result;

                var mainServiceIds = complexItems
                    .Select(m => m?.main_service)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var mainServiceId in mainServiceIds)
                {
                    if (dicts.Directory != null &&
                        dicts.Directory.TryGetValue(mainServiceId, out var mainService) &&
                        mainService != null)
                    {
                        AddBiomaterialsFromBaseService(mainService, result);
                    }
                }

                if (result.Count > 0)
                    return result;
            }

            AddBiomaterialsFromBaseService(service, result);

            return result;
        }

        private bool TryResolveTransportFromSamplesServices(string serviceId, string biomaterialId, out DictionaryTransport transport)
        {
            transport = null;

            if (string.IsNullOrEmpty(serviceId) || laboratory?.Dicts == null)
                return false;

            if (laboratory.Dicts.SamplesServices == null ||
                !laboratory.Dicts.SamplesServices.TryGetValue(serviceId, out var rows) ||
                rows == null || rows.Count == 0)
                return false;

            var row = rows.FirstOrDefault(r =>
                r != null &&
                string.Equals(r.service_id, serviceId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.biomaterial_id ?? string.Empty, biomaterialId ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                r.sample_id > 0);

            if (row == null)
            {
                row = rows.FirstOrDefault(r =>
                    r != null &&
                    string.Equals(r.service_id, serviceId, StringComparison.OrdinalIgnoreCase) &&
                    r.sample_id > 0);
            }

            if (row == null || laboratory.Dicts.Samples == null)
                return false;

            if (!laboratory.Dicts.Samples.TryGetValue(row.sample_id.ToString(), out var sample) || sample == null)
                return false;

            if (string.IsNullOrEmpty(sample.transport_id) || laboratory.Dicts.Transport == null)
                return false;

            return laboratory.Dicts.Transport.TryGetValue(sample.transport_id, out transport) && transport != null;
        }

        private bool TryResolveBaseTransport(string serviceId, string biomaterialId, out DictionaryTransport transport)
        {
            transport = null;

            if (string.IsNullOrEmpty(serviceId) || laboratory?.Dicts == null)
                return false;

            if (laboratory.Dicts.ServiceParameters != null &&
                laboratory.Dicts.ServiceParameters.TryGetValue(serviceId, out var paramsList) &&
                paramsList != null && paramsList.Count > 0)
            {
                var param = paramsList.FirstOrDefault(p =>
                    p != null &&
                    string.Equals(p.service_id, serviceId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.biomaterial_id ?? string.Empty, biomaterialId ?? string.Empty, StringComparison.OrdinalIgnoreCase));

                if (param != null &&
                    !string.IsNullOrEmpty(param.transport_id) &&
                    laboratory.Dicts.Transport != null &&
                    laboratory.Dicts.Transport.TryGetValue(param.transport_id, out transport) &&
                    transport != null)
                {
                    return true;
                }
            }

            if (laboratory.Dicts.Directory != null &&
                laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) &&
                svc != null &&
                !string.IsNullOrEmpty(svc.transport_id) &&
                laboratory.Dicts.Transport != null &&
                laboratory.Dicts.Transport.TryGetValue(svc.transport_id, out transport) &&
                transport != null)
            {
                return true;
            }

            return false;
        }

        private DictionaryTransport ResolveTransport(string serviceId, string biomaterialId)
        {
            DictionaryTransport transport = null;

            if (string.IsNullOrEmpty(serviceId) || laboratory?.Dicts == null || laboratory.Dicts.Directory == null)
                return null;

            if (!laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) || svc == null)
                return null;

            var complexItems = GetMarketingComplexItems(svc);

            if (complexItems.Count > 0)
            {
                var mcItem = complexItems.FirstOrDefault(m =>
                    m != null &&
                    !string.IsNullOrEmpty(m.transport_id) &&
                    (string.Equals(m.biomaterial_id ?? string.Empty, biomaterialId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                     || string.IsNullOrEmpty(m.biomaterial_id)));

                if (mcItem != null &&
                    laboratory.Dicts.Transport != null &&
                    laboratory.Dicts.Transport.TryGetValue(mcItem.transport_id, out transport) &&
                    transport != null)
                {
                    return transport;
                }

                var mainServiceIds = complexItems
                    .Select(m => m?.main_service)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var mainServiceId in mainServiceIds)
                {
                    if (TryResolveTransportFromSamplesServices(mainServiceId, biomaterialId, out transport))
                        return transport;

                    if (TryResolveBaseTransport(mainServiceId, biomaterialId, out transport))
                        return transport;
                }
            }

            if (TryResolveTransportFromSamplesServices(serviceId, biomaterialId, out transport))
                return transport;

            if (TryResolveBaseTransport(serviceId, biomaterialId, out transport))
                return transport;

            return null;
        }

        private bool IsMarketingComplex(string serviceId)
        {
            if (string.IsNullOrEmpty(serviceId) || laboratory?.Dicts == null || laboratory.Dicts.Directory == null)
                return false;

            if (!laboratory.Dicts.Directory.TryGetValue(serviceId, out var svc) || svc == null)
                return false;

            return svc.service_type == 1 || svc.service_type == 2 ||
                   (laboratory.Dicts.MarketingComplexByComplexId != null && laboratory.Dicts.MarketingComplexByComplexId.ContainsKey(serviceId)) ||
                   (laboratory.Dicts.MarketingComplexByServiceId != null && laboratory.Dicts.MarketingComplexByServiceId.ContainsKey(serviceId));
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

                var transport = ResolveTransport(serviceId, biom.id);
                string containerName = transport != null ? (transport.name ?? string.Empty) : "не указан";

                var info = new BiomaterialInfoForGUI
                {
                    BiomaterialId = biom.id,
                    BiomaterialCode = biom.id,
                    BiomaterialName = BuildBiomaterialDisplayName(biom.name, containerName),
                    ContainerId = transport != null ? transport.id : string.Empty,
                    ContainerCode = transport != null ? transport.id : string.Empty,
                    ContainerName = containerName
                };

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
                var transport = ResolveTransport(productDetail.ProductId, biom.Id);
                string containerName = NormalizeContainerName(transport != null ? transport.name : string.Empty);

                var info = new BiomaterialInfoForGUI
                {
                    BiomaterialId = biom.Id,
                    BiomaterialCode = biom.Code,
                    BiomaterialName = biom.Name,
                    ContainerId = transport != null ? transport.id : string.Empty,
                    ContainerCode = transport != null ? transport.id : string.Empty,
                    ContainerName = containerName
                };

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
            if (group.Biomaterials.Count > 0)
                group.BiomaterialsSelected.Add(group.Biomaterials[0]);

            return group;
        }

        private void RebuildBiomaterialGroups(GemotestOrderDetail details, OrderModelForGUI model)
        {
            foreach (var p in model.ProductsInfo)
            {
                p.BiomaterialGroups.Clear();
            }

            for (int i = 0; i < model.ProductsInfo.Count; i++)
            {
                var g = BuildBiomaterialGroupForProduct(details, i);
                g.GroupNum = 1;
                g.Optional = false;
                g.RefreshFieldsOnSelectionSet = true;
                g.RefreshFieldsOnSelectionRemove = true;
                model.ProductsInfo[i].BiomaterialGroups.Add(g);
            }
        }

        private void PrepareBiomaterialsForChooseForm(List<ProductInfoForGUI> products)
        {
            if (products == null)
                return;

            foreach (var product in products)
            {
                product.BiomaterialGroups.Clear();
                var g = BuildBiomaterialGroupForService(product.Id);
                g.GroupNum = 1;
                g.Optional = false;
                g.RefreshFieldsOnSelectionSet = true;
                g.RefreshFieldsOnSelectionRemove = true;
                product.BiomaterialGroups.Add(g);
            }
        }

        private List<string> GetSelectedBiomaterialIds(ProductInfoForGUI product)
        {
            if (product?.BiomaterialGroups == null)
                return new List<string>();

            return product.BiomaterialGroups
                .Where(g => g?.BiomaterialsSelected != null)
                .SelectMany(g => g.BiomaterialsSelected)
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.BiomaterialId))
                .Select(x => x.BiomaterialId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ApplySelectedBiomaterialsToAddedProduct(Order order, OrderModelForGUI model, int productIndex, List<string> selectedBioIds)
        {
            if (model?.ProductsInfo == null || productIndex < 0 || productIndex >= model.ProductsInfo.Count)
                return;

            var group = model.ProductsInfo[productIndex].BiomaterialGroups.FirstOrDefault();
            if (group == null)
                return;

            if (selectedBioIds == null || selectedBioIds.Count == 0)
                return;

            group.BiomaterialsSelected.Clear();
            foreach (var biom in group.Biomaterials)
            {
                if (biom != null && selectedBioIds.Contains(biom.BiomaterialId))
                    group.BiomaterialsSelected.Add(biom);
            }
        }

        private void ApplyAutoInsertServices(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details?.Products == null || laboratory?.Dicts?.ServiceAutoInsert == null)
                return;

            bool added;
            do
            {
                added = false;
                var existingIds = new HashSet<string>(
                 details.Products
                     .Where(p => p != null && !string.IsNullOrWhiteSpace(p.ProductId))
                     .Select(p => p.ProductId),
                 StringComparer.OrdinalIgnoreCase);
                var toInsert = new List<string>();

                foreach (var p in details.Products)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.ProductId))
                        continue;

                    if (laboratory.Dicts.ServiceAutoInsert.TryGetValue(p.ProductId, out var rows) && rows != null)
                    {
                        foreach (var row in rows)
                        {
                            if (row == null || string.IsNullOrWhiteSpace(row.auto_service_id))
                                continue;

                            if (!existingIds.Contains(row.auto_service_id) &&
                                !toInsert.Contains(row.auto_service_id, StringComparer.OrdinalIgnoreCase))
                            {
                                toInsert.Add(row.auto_service_id);
                            }
                        }
                    }
                }

                foreach (var addId in toInsert)
                {
                    if (laboratory?.Dicts?.Directory == null || !laboratory.Dicts.Directory.TryGetValue(addId, out var svc) || svc == null)
                        continue;

                    int newIndex = model.ProductsInfo.Count;
                    var p = new ProductInfoForGUI
                    {
                        OrderProductGuid = newIndex.ToString(),
                        Id = svc.id,
                        Code = svc.code,
                        Name = svc.name,
                        ProductGroupGuid = null
                    };

                    model.ProductsInfo.Add(p);
                    details.Products.Add(new GemotestProductDetail
                    {
                        OrderProductGuid = p.OrderProductGuid,
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = p.Name
                    });

                    existingIds.Add(addId);
                    added = true;
                }
            }
            while (added);
        }
    }
}
