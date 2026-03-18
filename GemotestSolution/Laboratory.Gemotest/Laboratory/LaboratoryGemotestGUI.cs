using Laboratory.Gemotest.Options;
using SiMed.Laboratory;
using Laboratory.Gemotest.SourseClass;
using Laboratory.Gemotest.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using StatisticsCollectionSystemClient;
using static Laboratory.Gemotest.SourseClass.GemotestOrderDetail;
using Laboratory.Gemotest.GemotestRequests;
using SiMed.Clinic;
using PrintCommon;
using System.Data;
using System.Drawing.Printing;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;
using System.Globalization;

namespace Laboratory.Gemotest
{
    public class LaboratoryGemotestGUI : ILaboratoryGUI
    {
        private LaboratoryGemotest laboratory;
        private LocalOptions localOptions;
        private SystemOptions globalOptions;
        private ProductsCollection AllProducts;
        private INumerator numerator;

        public bool SetOptions(LaboratoryGemotest lab, ProductsCollection products, LocalOptions local, SystemOptions global, INumerator _Numerator)
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
                var details = _Order?.OrderDetail as GemotestOrderDetail;
                if (details != null && details.Samples != null)
                {
                    foreach (var sampleNew in samples)
                    {
                        if (!string.IsNullOrWhiteSpace(sampleNew.Barcode))
                            continue;

                        foreach (var sampleFromDetails in details.Samples)
                        {
                            if (sampleFromDetails == null)
                                continue;

                            if (!string.Equals(sampleFromDetails.BiomCode, sampleNew.Biomaterial.BiomaterialCode, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (!string.Equals(sampleFromDetails.ContCode, sampleNew.Biomaterial.ContainerCode, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var oldSet = string.Join(",", sampleFromDetails.OrderProductGuidList.OrderBy(x => x));
                            var newSet = string.Join(",", sampleNew.OrderProductGuids.OrderBy(x => x));

                            if (oldSet != newSet)
                                continue;

                            sampleNew.OrderSampleGuid = sampleFromDetails.OrderSampleGuid;
                            sampleNew.Barcode = sampleFromDetails.Barcode;
                            break;
                        }
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

                if (_ReadOnly)
                    return BuildReadOnlyModelFromDetails(_Order, details, _Model);

                FillPriceListsForModel(false, details, _Model);

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

                AddDocumentsForModel(details, _Model);

                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return false;
            }
        }

        private bool BuildReadOnlyModelFromDetails(Order order, GemotestOrderDetail details, OrderModelForGUI model)
        {
            try
            {
                FillPriceListsForModel(true, details, model);

                BuildReadOnlyProductsFromDetails(details, model);
                BuildReadOnlySamplesFromDetails(details, model);
                BuildReadOnlyFieldsFromDetails(details, model);

                AddDocumentsForModel(details, model);

                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                return false;
            }
        }

        private void BuildReadOnlyProductsFromDetails(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details.Products == null)
                return;

            foreach (var product in details.Products)
            {
                if (product == null)
                    continue;

                var productNew = new ProductInfoForGUI
                {
                    OrderProductGuid = string.IsNullOrWhiteSpace(product.OrderProductGuid)
                        ? details.Products.IndexOf(product).ToString()
                        : product.OrderProductGuid,
                    Id = product.ProductId ?? string.Empty,
                    Code = product.ProductCode ?? string.Empty,
                    Name = product.ProductName ?? string.Empty,
                    ProductGroupGuid = null,
                    BiomaterialGroups = new List<BiomaterialGroupForGUI>()
                };

                var group = new BiomaterialGroupForGUI
                {
                    GroupNum = 0,
                    SelectOnlyOne = false,
                    Optional = true
                };

                if (details.Samples != null)
                {
                    foreach (var sampleInfo in details.Samples)
                    {
                        if (sampleInfo == null || sampleInfo.OrderProductGuidList == null)
                            continue;

                        if (!sampleInfo.OrderProductGuidList.Contains(productNew.OrderProductGuid))
                            continue;

                        var biomInfo = new BiomaterialInfoForGUI
                        {
                            BiomaterialId = sampleInfo.BiomId ?? string.Empty,
                            BiomaterialCode = sampleInfo.BiomCode ?? string.Empty,
                            BiomaterialName = sampleInfo.BiomName ?? string.Empty,
                            ContainerId = sampleInfo.ContId ?? string.Empty,
                            ContainerCode = sampleInfo.ContCode ?? string.Empty,
                            ContainerName = sampleInfo.ContName ?? string.Empty
                        };

                        bool alreadyExists = group.Biomaterials.Any(x =>
                            string.Equals(x.BiomaterialCode, biomInfo.BiomaterialCode, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.ContainerCode, biomInfo.ContainerCode, StringComparison.OrdinalIgnoreCase));

                        if (!alreadyExists)
                        {
                            group.Biomaterials.Add(biomInfo);
                            group.BiomaterialsSelected.Add(biomInfo);
                        }
                    }
                }

                if (group.Biomaterials.Count == 0)
                {
                    // fallback — чтобы продукт не открывался пустым
                    var fallbackBiom = ResolveFallbackBiomaterialForProduct(details, product);
                    if (fallbackBiom != null)
                    {
                        group.Biomaterials.Add(fallbackBiom);
                        group.BiomaterialsSelected.Add(fallbackBiom);
                    }
                }

                productNew.BiomaterialGroups.Add(group);
                model.ProductsInfo.Add(productNew);
            }
        }

        private BiomaterialInfoForGUI ResolveFallbackBiomaterialForProduct(
    GemotestOrderDetail details,
    GemotestOrderDetail.GemotestProductDetail product)
        {
            if (details == null || product == null || details.Dicts == null)
                return null;

            var dicts = details.Dicts;

            if (dicts.Directory == null)
                return null;

            if (!dicts.Directory.TryGetValue(product.ProductId ?? string.Empty, out var service) || service == null)
                return null;

            string biomId = service.biomaterial_id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(biomId))
                return null;

            if (dicts.Biomaterials == null)
                return null;

            if (!dicts.Biomaterials.TryGetValue(biomId, out var biom) || biom == null)
                return null;

            DictionaryTransport transport = null;

            if (!string.IsNullOrWhiteSpace(service.transport_id) &&
                dicts.Transport != null)
            {
                dicts.Transport.TryGetValue(service.transport_id, out transport);
            }

            if (transport == null &&
                dicts.ServiceParameters != null &&
                dicts.ServiceParameters.TryGetValue(service.id ?? string.Empty, out var parameters) &&
                parameters != null)
            {
                var param = parameters.FirstOrDefault(x =>
                    x != null &&
                    string.Equals(x.biomaterial_id ?? string.Empty, biomId, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(x.transport_id));

                if (param == null)
                {
                    param = parameters.FirstOrDefault(x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(x.transport_id));
                }

                if (param != null && dicts.Transport != null)
                    dicts.Transport.TryGetValue(param.transport_id, out transport);
            }

            if (transport == null)
                TryResolveTransportFromSamplesServices(service.id, biomId, out transport);

            return new BiomaterialInfoForGUI
            {
                BiomaterialId = biom.id ?? string.Empty,
                BiomaterialCode = biom.id ?? string.Empty,
                BiomaterialName = biom.name ?? string.Empty,

                ContainerId = transport != null ? (transport.id ?? string.Empty) : string.Empty,
                ContainerCode = transport != null ? (transport.id ?? string.Empty) : string.Empty,
                ContainerName = transport != null ? (transport.name ?? string.Empty) : "-"
            };
        }
        private void BuildReadOnlySamplesFromDetails(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details.Samples == null)
                return;

            foreach (var sampleInfo in details.Samples)
            {
                if (sampleInfo == null)
                    continue;

                var sampleNew = new SampleInfoForGUI
                {
                    OrderSampleGuid = sampleInfo.OrderSampleGuid ?? string.Empty,
                    Barcode = sampleInfo.Barcode ?? string.Empty,
                    Biomaterial = new BiomaterialInfoForGUI
                    {
                        BiomaterialId = sampleInfo.BiomId ?? string.Empty,
                        BiomaterialCode = sampleInfo.BiomCode ?? string.Empty,
                        BiomaterialName = sampleInfo.BiomName ?? string.Empty,
                        ContainerId = sampleInfo.ContId ?? string.Empty,
                        ContainerCode = sampleInfo.ContCode ?? string.Empty,
                        ContainerName = sampleInfo.ContName ?? string.Empty
                    },
                    OrderProductGuids = sampleInfo.OrderProductGuidList != null
                        ? new List<string>(sampleInfo.OrderProductGuidList)
                        : new List<string>()
                };

                model.Samples.Add(sampleNew);
            }
        }

        private void BuildReadOnlyFieldsFromDetails(GemotestOrderDetail details, OrderModelForGUI model)
        {
            if (details.Details == null)
                return;

            foreach (var detail in details.Details)
            {
                if (detail == null)
                    continue;

                var fieldNew = new FieldInfoForGUI
                {
                    Id = GetDetailKey(detail),
                    Description = string.IsNullOrWhiteSpace(detail.Name) ? GetDetailKey(detail) : detail.Name,
                    Value = detail.Value ?? string.Empty,
                    DisplayValue = string.IsNullOrWhiteSpace(detail.DisplayValue) ? (detail.Value ?? string.Empty) : detail.DisplayValue,
                    Regex = detail.regex,
                    Mandatory = detail.MandatoryProducts != null && detail.MandatoryProducts.Count > 0,
                    FieldDataType = detail.dictionaryId.HasValue ? FieldDataType.Dictionary : FieldDataType.Text
                };

                if (detail.MandatoryProducts != null)
                {
                    foreach (var idx in detail.MandatoryProducts)
                    {
                        if (idx >= 0 && idx < details.Products.Count)
                            fieldNew.OrderProductGuidList.Add(details.Products[idx].OrderProductGuid);
                    }
                }

                if (detail.OptionalProducts != null)
                {
                    foreach (var idx in detail.OptionalProducts)
                    {
                        if (idx >= 0 && idx < details.Products.Count)
                        {
                            string guid = details.Products[idx].OrderProductGuid;
                            if (!fieldNew.OrderProductGuidList.Contains(guid))
                                fieldNew.OrderProductGuidList.Add(guid);
                        }
                    }
                }

                model.Fields.Add(fieldNew);
            }

            model.Fields = model.Fields
                .OrderBy(x => x.Description)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private void AddDocumentsForModel(GemotestOrderDetail details, OrderModelForGUI model)
        {
            model.Documents.Add(new DocumentInfoForGUI()
            {
                Name = "Сопроводительный бланк",
                DocType = LaboratoryPrintDocumentType.Blank,
                PreviewAvailable = true,
                PrintAvailable = true
            });

            bool hasRealBarcodes =
                details.Samples != null &&
                details.Samples.Any(x => x != null && !string.IsNullOrWhiteSpace(x.Barcode));

            if (hasRealBarcodes)
            {
                model.Documents.Add(new DocumentInfoForGUI()
                {
                    Name = "Наклейки",
                    DocType = LaboratoryPrintDocumentType.Stikers,
                    PrintAvailable = true
                });
            }

            bool hasResults = (details.Results != null && details.Results.Count > 0) || !string.IsNullOrWhiteSpace(details.ResultsRawXml);
            bool hasAttachments = details.Attachments != null && details.Attachments.Any(x => x != null && !string.IsNullOrWhiteSpace(x.FileUrl));

            if (hasResults)
            {
                model.Documents.Add(new DocumentInfoForGUI()
                {
                    DocType = LaboratoryPrintDocumentType.ToolStripSeparator,
                    PreviewAvailable = true,
                    PrintAvailable = true
                });

                model.Documents.Add(new DocumentInfoForGUI()
                {
                    Name = "Сводные результаты",
                    DocType = LaboratoryPrintDocumentType.ConsolidationResults,
                    PreviewAvailable = true,
                    PrintAvailable = true
                });
            }

            if (hasAttachments)
            {
                model.Documents.Add(new DocumentInfoForGUI()
                {
                    DocType = LaboratoryPrintDocumentType.ToolStripSeparator,
                    PreviewAvailable = true,
                    PrintAvailable = true
                });

                int idx = 1;
                foreach (var attachment in details.Attachments.Where(x => x != null && !string.IsNullOrWhiteSpace(x.FileUrl)))
                {
                    model.Documents.Add(new DocumentInfoForGUI()
                    {
                        Name = !string.IsNullOrWhiteSpace(attachment.DisplayName)
                            ? attachment.DisplayName
                            : $"Файл результатов #{idx}.pdf",
                        DocType = LaboratoryPrintDocumentType.ResultsFile,
                        Num = idx - 1,
                        PreviewAvailable = true,
                        PrintAvailable = true
                    });

                    idx++;
                }
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

                var oldSamples = details.Samples != null ? new List<GemotestSampleDetail>(details.Samples) : new List<GemotestSampleDetail>();

                if (_Order.State == OrderState.NotSended || _Order.State == OrderState.Prepared)
                {
                    details.Samples.Clear();

                    if (_Model.Samples != null)
                    {
                        foreach (var sampleFromModel in _Model.Samples)
                        {
                            if (sampleFromModel == null || sampleFromModel.Biomaterial == null)
                                continue;

                            var oldSample = oldSamples.FirstOrDefault(x =>
                                x != null &&
                                x.OrderSampleGuid == sampleFromModel.OrderSampleGuid);

                            details.Samples.Add(new GemotestSampleDetail()
                            {
                                OrderSampleGuid = sampleFromModel.OrderSampleGuid,
                                Barcode = oldSample != null ? oldSample.Barcode : sampleFromModel.Barcode,

                                SampleId = oldSample != null ? oldSample.SampleId : string.Empty,
                                SampleIdentifier = oldSample != null ? oldSample.SampleIdentifier : string.Empty,
                                SampleDescription = oldSample != null ? oldSample.SampleDescription : string.Empty,

                                BiomId = sampleFromModel.Biomaterial.BiomaterialId,
                                BiomCode = sampleFromModel.Biomaterial.BiomaterialCode,
                                BiomName = sampleFromModel.Biomaterial.BiomaterialName,

                                ContId = sampleFromModel.Biomaterial.ContainerId,
                                ContCode = sampleFromModel.Biomaterial.ContainerCode,
                                ContName = sampleFromModel.Biomaterial.ContainerName,

                                LocalizationId = oldSample != null ? oldSample.LocalizationId : string.Empty,
                                LocalizationName = oldSample != null ? oldSample.LocalizationName : string.Empty,

                                TransportId = oldSample != null ? oldSample.TransportId : sampleFromModel.Biomaterial.ContainerId,
                                TransportName = oldSample != null ? oldSample.TransportName : sampleFromModel.Biomaterial.ContainerName,

                                LabCenterId = oldSample != null ? oldSample.LabCenterId : string.Empty,

                                OrderProductGuidList = sampleFromModel.OrderProductGuids != null ? new List<string>(sampleFromModel.OrderProductGuids) : new List<string>()


                            });
                        }
                    }
                }

                SaveFieldsToDetails(details, _Model);
                details.DeleteObsoleteDetails();
                details.PriceListCode = details.PriceListCode ?? string.Empty;
                details.PriceListName = details.PriceListName ?? string.Empty;
                details.PriceList = details.PriceList ?? string.Empty;
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

        /* private GemotestBlankReportDataSetV2 FillDataSetForBlankReport(Order _Order)
         {
             var details = (GemotestOrderDetail)_Order.OrderDetail;
             var dataset = new GemotestBlankReportDataSetV2();

             DataRow row = dataset.BlankTable.NewRow();
             row["DateofFormation"] = _Order.Date.ToString("dd.MM.yyyy");
             row["LaboratoryName"] = "Гемотест";
             row["ClinicName"] = !string.IsNullOrWhiteSpace(details.PriceListName)
                 ? details.PriceListName
                 : (details.PriceList ?? "");
             row["OrderCode"] = _Order.Number ?? "";

             string sexText = "";
             if (_Order.Patient != null)
             {
                 if (_Order.Patient.Sex == Sex.Male) sexText = "Муж";
                 else if (_Order.Patient.Sex == Sex.Female) sexText = "Жен";
             }

             string fio =
                 (((_Order.Patient?.Surname) ?? "") + " " +
                  ((_Order.Patient?.Name) ?? "") + " " +
                  ((_Order.Patient?.Patronimic) ?? "")).Trim();

             row["Pacient_FIO"] = string.IsNullOrWhiteSpace(sexText)
                 ? fio
                 : fio + " (" + sexText + ")";

             row["PriceListName"] = details.PriceListName ?? details.PriceList ?? "";
             dataset.BlankTable.Rows.Add(row);

             AddPatientParameter(dataset, "Дата рождения:", _Order.Patient != null ? _Order.Patient.Birthday.ToString("dd.MM.yyyy") : "");
             AddPatientParameter(dataset, "Email:", GetDetailValue(details, "email", "Email", "Patient_Email"));
             AddPatientParameter(dataset, "Телефон:", GetDetailValue(details, "mobile_phone", "Patient_Phone", "phone", "Phone"));
             AddPatientParameter(dataset, "СНИЛС:", GetDetailValue(details, "snils", "SNILS", "Patient_SNILS"));
             AddPatientParameter(dataset, "ОМС:", GetDetailValue(details, "oms", "OMS"));
             AddPatientParameter(dataset, "Адрес:", GetDetailValue(details, "address", "Address"));

             foreach (var detail in details.Details ?? new System.Collections.Generic.List<GemotestDetail>())
             {
                 if (detail == null)
                     continue;

                 string code = (detail.Code ?? "").Trim();
                 if (string.IsNullOrWhiteSpace(code))
                     continue;

                 if (IsBlankSystemField(code))
                     continue;

                 string value = !string.IsNullOrWhiteSpace(detail.DisplayValue)
                     ? detail.DisplayValue
                     : (detail.Value ?? "");

                 if (string.IsNullOrWhiteSpace(value))
                     continue;

                 AddPatientParameter(dataset, detail.Name ?? code, value);
             }

             foreach (var product in details.Products ?? new System.Collections.Generic.List<GemotestOrderDetail.GemotestProductDetail>())
             {
                 if (product == null)
                     continue;

                 var linkedSamples = (details.Samples ?? new System.Collections.Generic.List<GemotestSampleDetail>())
                     .Where(s => s != null &&
                                 s.OrderProductGuidList != null &&
                                 s.OrderProductGuidList.Contains(product.OrderProductGuid))
                     .ToList();

                 if (linkedSamples.Count == 0)
                 {
                     DataRow prodRow = dataset.Products.NewRow();
                     prodRow["PROD_CODE"] = product.ProductCode ?? "";
                     prodRow["PROD_NAME"] = product.ProductName ?? "";
                     dataset.Products.Rows.Add(prodRow);
                     continue;
                 }

                 foreach (var sample in linkedSamples)
                 {
                     DataRow prodRow = dataset.Products.NewRow();
                     prodRow["PROD_CODE"] = product.ProductCode ?? "";
                     prodRow["PROD_NAME"] = product.ProductName ?? "";

                     prodRow["SAMPLE_BARCODE"] = sample.Barcode ?? "";
                     prodRow["SAMPLE_ID"] = sample.SampleId ?? "";
                     prodRow["SAMPLE_IDENTIFIER"] = sample.SampleIdentifier ?? "";

                     prodRow["BIOMATERIAL_NAME"] = sample.BiomName ?? "";
                     prodRow["CONTAINER_NAME"] = sample.ContName ?? "";
                     prodRow["LOCALIZATION_NAME"] = sample.LocalizationName ?? "";
                     prodRow["TRANSPORT_NAME"] = sample.TransportName ?? "";
                     prodRow["LAB_CENTER_ID"] = sample.LabCenterId ?? "";
                     prodRow["SAMPLE_DESCRIPTION"] = sample.SampleDescription ?? "";

                     dataset.Products.Rows.Add(prodRow);
                 }
             }

             return dataset;
         }

         private void AddPatientParameter(GemotestBlankReportDataSetV2 dataset, string name, string value)
         {
             if (dataset == null || string.IsNullOrWhiteSpace(value))
                 return;

             DataRow row = dataset.PatientParameters.NewRow();
             row["PatientParamName"] = name ?? "";
             row["PatientParamValue"] = value ?? "";
             dataset.PatientParameters.Rows.Add(row);
         }
 */
        private string GetDetailValue(GemotestOrderDetail details, params string[] codes)
        {
            if (details == null || details.Details == null || codes == null)
                return "";

            foreach (string code in codes)
            {
                var d = details.Details.FirstOrDefault(x =>
                    x != null &&
                    !string.IsNullOrWhiteSpace(x.Code) &&
                    string.Equals(x.Code.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase));

                if (d == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(d.DisplayValue))
                    return d.DisplayValue;

                if (!string.IsNullOrWhiteSpace(d.Value))
                    return d.Value;
            }

            return "";
        }

        private bool IsBlankSystemField(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            code = code.Trim().ToLowerInvariant();

            switch (code)
            {
                case "email":
                case "patient_email":
                case "mobile_phone":
                case "patient_phone":
                case "phone":
                case "home_phone":
                case "snils":
                case "patient_snils":
                case "oms":
                case "address":
                    return true;
                default:
                    return false;
            }
        }

        public bool PrintStikers(Order _Order, List<SampleInfoForGUI> _SelectedSamples)
        {
            LastException = null;

            try
            {
                if (_Order == null)
                    throw new ArgumentNullException(nameof(_Order));

                if (_SelectedSamples == null || _SelectedSamples.Count == 0)
                    return true;

                Encoding encoding = Encoding.UTF8;
                if (localOptions != null)
                {
                    if (localOptions.LabelEncoding == Options.LabelEncoding.Code866)
                        encoding = Encoding.GetEncoding(866);
                    else if (localOptions.LabelEncoding == Options.LabelEncoding.Windows1251)
                        encoding = Encoding.GetEncoding("windows-1251");
                }

                PrinterSettings settings;
                if (localOptions != null &&
                    localOptions.StickerPrinterSettings != null &&
                    localOptions.StickerPrinterSettings.IsValid)
                {
                    settings = localOptions.StickerPrinterSettings;
                }
                else
                {
                    settings = Print.GetDefaultPrinterSettingsAccorgingToFormat();
                }

                string labelTemplate = ResolveStickerTemplate();
                if (string.IsNullOrWhiteSpace(labelTemplate))
                    throw new InvalidOperationException("Не задан шаблон печати этикеток.");

                var details = _Order.OrderDetail as GemotestOrderDetail;
                var detailSamples = details != null ? details.Samples : new List<GemotestSampleDetail>();

                foreach (var sample in _SelectedSamples)
                {
                    if (sample == null || string.IsNullOrWhiteSpace(sample.Barcode))
                        continue;

                    var fullSample = detailSamples.FirstOrDefault(x =>
                        x != null &&
                        (
                            (!string.IsNullOrWhiteSpace(x.OrderSampleGuid) && x.OrderSampleGuid == sample.OrderSampleGuid) ||
                            (!string.IsNullOrWhiteSpace(x.Barcode) && x.Barcode == sample.Barcode)
                        ));

                    string line1 = BuildStickerLine1(_Order);
                    string line2 = BuildStickerLine2(sample, fullSample);
                    string barcode = sample.Barcode ?? "";
                    string line4 = BuildStickerLine4(_Order, fullSample);

                    string raw = string.Format(
                        labelTemplate,
                        EscapeLabel(line1),
                        EscapeLabel(line2),
                        EscapeLabel(barcode),
                        EscapeLabel(line4));

                    Print.SendStringToPrinter(settings, raw, encoding);
                }

                return true;
            }
            catch (Exception exc)
            {
                LastException = exc;
                return false;
            }
        }
        private string ResolveStickerTemplate()
        {
            if (localOptions == null)
                return Options.LocalOptions.GetDefaultLabelTemplate(Options.LabelType.EPL);

            if (localOptions.LabelType == Options.LabelType.ZPL)
                return Options.LocalOptions.GetDefaultLabelTemplate(Options.LabelType.ZPL);

            if (localOptions.LabelType == Options.LabelType.EPL)
                return Options.LocalOptions.GetDefaultLabelTemplate(Options.LabelType.EPL);

            if (localOptions.LabelType == Options.LabelType.Custom)
                return localOptions.CustomLabelTemplate ?? "";

            return Options.LocalOptions.GetDefaultLabelTemplate(Options.LabelType.EPL);
        }

        private string BuildStickerLine1(Order order)
        {
            if (order == null || order.Patient == null)
                return "";

            string fio =
                ((order.Patient.Surname ?? "") + " " +
                 (order.Patient.Name ?? "") + " " +
                 (order.Patient.Patronimic ?? "")).Trim();

            return fio;
        }

        private string BuildStickerLine2(SampleInfoForGUI guiSample, GemotestSampleDetail fullSample)
        {
            string biom = guiSample?.Biomaterial?.BiomaterialName ?? "";
            string cont = guiSample?.Biomaterial?.ContainerName ?? "";

            if (string.IsNullOrWhiteSpace(cont))
                cont = fullSample?.ContName ?? "";

            if (!string.IsNullOrWhiteSpace(fullSample?.LocalizationName))
                return (biom + " / " + cont + " / " + fullSample.LocalizationName).Trim(' ', '/');

            return (biom + " / " + cont).Trim(' ', '/');
        }

        private string BuildStickerLine4(Order order, GemotestSampleDetail fullSample)
        {
            string orderNum = order != null ? (order.Number ?? "") : "";
            string date = order != null ? order.Date.ToString("dd.MM.yy") : "";

            if (!string.IsNullOrWhiteSpace(fullSample?.LabCenterId))
                return (orderNum + " " + date + " ЛЦ:" + fullSample.LabCenterId).Trim();

            return (orderNum + " " + date).Trim();
        }

        private string EscapeLabel(string value)
        {
            return (value ?? "")
                .Replace("\"", "'")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
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

        private byte[] DownloadAttachmentBytes(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Ссылка на вложение пуста.");

            using (var wc = new System.Net.WebClient())
            {
                return wc.DownloadData(url);
            }
        }

        public bool PrintLaboratoryDocument(Order _Order, ref ResultsCollection _Results, DocumentInfoForGUI _Document, bool _Preview)
        {
            LastException = null;

            try
            {
                if (_Order == null)
                    throw new ArgumentNullException(nameof(_Order));

                if (_Document == null)
                    throw new ArgumentNullException(nameof(_Document));
                if (_Document.DocType == LaboratoryPrintDocumentType.ResultsFile)
                {
                    var details = _Order.OrderDetail as GemotestOrderDetail;
                    if (details == null)
                        throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                    if (!_Document.Num.HasValue)
                        throw new InvalidOperationException("Не задан индекс файла результатов.");

                    int index = _Document.Num.Value;
                    if (details.Attachments == null || index < 0 || index >= details.Attachments.Count)
                        throw new InvalidOperationException("Файл результатов не найден в заказе.");

                    var attachment = details.Attachments[index];
                    if (attachment == null || string.IsNullOrWhiteSpace(attachment.FileUrl))
                        throw new InvalidOperationException("Пустая ссылка на файл результатов.");

                    byte[] fileBytes = DownloadAttachmentBytes(attachment.FileUrl);

                    if (_Preview)
                    {
                        Print.OpenPdf(fileBytes);
                    }
                    else
                    {
                        PrinterSettings settings;
                        if (localOptions != null && localOptions.PdfPrinterSettings != null)
                            settings = localOptions.PdfPrinterSettings;
                        else
                            settings = Print.GetDefaultPrinterSettingsAccorgingToFormat();

                        Print.PrintPdf(settings, fileBytes);
                    }

                    return true;
                }

                if (_Document.DocType == LaboratoryPrintDocumentType.Stikers)
                {
                    if (_Order.State == OrderState.NotSended)
                    {
                        MessageBox.Show(
                            "Сначала необходимо подготовить или отправить заказ, чтобы получить штрихкоды.",
                            "Гемотест",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return false;
                    }

                    var samplesForGui = BuildStickerSamplesForGui(_Order);
                    if (samplesForGui == null || samplesForGui.Count == 0)
                    {
                        MessageBox.Show(
                            "Для заказа нет образцов для печати наклеек.",
                            "Гемотест",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return false;
                    }

                    var form = new FormLaboratoryPrintStickers(this, _Order, samplesForGui);
                    form.ShowDialog();
                    return true;
                }
                if (_Document.DocType == LaboratoryPrintDocumentType.ConsolidationResults)
                {
                    if (_Order.State != OrderState.FullResultReceived &&
                        _Order.State != OrderState.PartialResultReceived)
                    {
                        MessageBox.Show("Сначала необходимо получить результаты");
                        return false;
                    }

                    var details = _Order.OrderDetail as GemotestOrderDetail;
                    if (details == null)
                        throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

                    if (details.Results == null || details.Results.Count == 0)
                    {
                        MessageBox.Show("В заказе нет сохранённых результатов для сводного отчёта.");
                        return false;
                    }

                    PrinterSettings settings;
                    if (localOptions != null && localOptions.PdfPrinterSettings != null)
                        settings = localOptions.PdfPrinterSettings;
                    else
                        settings = Print.GetDefaultPrinterSettingsAccorgingToFormat();

                    List<Print.RDLReportParameter> parameters = new List<Print.RDLReportParameter>();

                    DataSetForReportGemotest dataset = FillDataSetForConsolidatedReport(_Order);
                    List<Print.RDLReportDataSet> datasets = new List<Print.RDLReportDataSet>();

                    BindingSource bs = new BindingSource();
                    bs.DataSource = new DataView(dataset.ConsolidatedReportParameters);
                    datasets.Add(new Print.RDLReportDataSet("dtsParameters", bs));

                    bs = new BindingSource();
                    bs.DataSource = new DataView(dataset.ConsolidatedReportHeader);
                    datasets.Add(new Print.RDLReportDataSet("dtsHeader", bs));

                    Print.PrintRDLCReport(settings, "LaboratoryConsolidatedReport.rdlc", parameters, datasets, !_Preview);
                    return true;
                }
                if (_Document.DocType == LaboratoryPrintDocumentType.Blank)
                {
                    if (_Order.State == OrderState.NotSended)
                    {
                        MessageBox.Show("Сначала необходимо подготовить заказ к отправке в лабораторию");
                        return false;
                    }

                    PrinterSettings settings;
                    if (localOptions != null && localOptions.PdfPrinterSettings != null)
                        settings = localOptions.PdfPrinterSettings;
                    else
                        settings = Print.GetDefaultPrinterSettingsAccorgingToFormat();

                    var parameters = new List<Print.RDLReportParameter>();

                    GemotestBlankReportDataSetV2 dataset = FillDataSetForBlankReport(_Order);

                    var datasets = new List<Print.RDLReportDataSet>();

                    BindingSource bs = new BindingSource();
                    bs.DataSource = new DataView(dataset.BlankTable);
                    datasets.Add(new Print.RDLReportDataSet("Blank", bs));

                    bs = new BindingSource();
                    bs.DataSource = new DataView(dataset.Products);
                    datasets.Add(new Print.RDLReportDataSet("Products", bs));

                    bs = new BindingSource();
                    bs.DataSource = new DataView(dataset.PatientParameters);
                    datasets.Add(new Print.RDLReportDataSet("PatientParams", bs));

                    Print.PrintRDLCReport(settings, "LaboratoryGemotestBlankReport.rdlc", parameters, datasets, !_Preview);
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

        private List<SampleInfoForGUI> BuildStickerSamplesForGui(Order _Order)
        {
            var result = new List<SampleInfoForGUI>();

            var details = _Order?.OrderDetail as GemotestOrderDetail;
            if (details == null || details.Samples == null)
                return result;

            foreach (var sample in details.Samples)
            {
                if (sample == null)
                    continue;

                result.Add(new SampleInfoForGUI()
                {
                    OrderSampleGuid = sample.OrderSampleGuid,
                    Barcode = sample.Barcode ?? string.Empty,
                    Biomaterial = new BiomaterialInfoForGUI()
                    {
                        BiomaterialId = sample.BiomId ?? string.Empty,
                        BiomaterialCode = sample.BiomCode ?? string.Empty,
                        BiomaterialName = sample.BiomName ?? string.Empty,

                        ContainerId = sample.ContId ?? string.Empty,
                        ContainerCode = sample.ContCode ?? string.Empty,
                        ContainerName = sample.ContName ?? string.Empty
                    },
                    OrderProductGuids = sample.OrderProductGuidList != null
                        ? new List<string>(sample.OrderProductGuidList)
                        : new List<string>()
                });
            }

            return result;
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

        //----
        private GemotestBlankReportDataSetV2 FillDataSetForBlankReport(Order _Order)
        {
            var details = _Order.OrderDetail as GemotestOrderDetail;
            if (details == null)
                throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

            var dataset = new GemotestBlankReportDataSetV2();

            FillBlankHeader(_Order, details, dataset);
            FillPatientParameters(_Order, details, dataset);
            FillProductsForBlank(details, dataset);

            return dataset;
        }

        private DataSetForReportGemotest FillDataSetForConsolidatedReport(Order order)
        {
            var details = order.OrderDetail as GemotestOrderDetail;
            if (details == null)
                throw new InvalidOperationException("OrderDetail не является GemotestOrderDetail.");

            var dataset = new DataSetForReportGemotest();

            FillConsolidatedHeader(order, dataset);
            FillConsolidatedParameters(order, details, dataset);

            return dataset;
        }

        private void FillConsolidatedHeader(Order order, DataSetForReportGemotest dataset)
        {
            DataRow row = dataset.ConsolidatedReportHeader.NewRow();

            DateTime orderDate = order.Date;
            if (orderDate <= DateTime.MinValue.AddDays(1))
                orderDate = DateTime.Now;

            row["ORDER_DATE"] = orderDate;
            row["ORDER_NUMBER"] = order.Number ?? string.Empty;
            row["PER_FIO"] = BuildPlainPatientFio(order);

            if (order.Patient != null && order.Patient.Birthday > DateTime.MinValue.AddDays(1))
                row["PER_BORN_DATE"] = order.Patient.Birthday;
            else
                row["PER_BORN_DATE"] = DateTime.Now.Date;

            if (order.Patient != null && order.Patient.Sex == Sex.Male)
                row["PER_SEX"] = "Муж";
            else if (order.Patient != null && order.Patient.Sex == Sex.Female)
                row["PER_SEX"] = "Жен";
            else
                row["PER_SEX"] = string.Empty;

            dataset.ConsolidatedReportHeader.Rows.Add(row);
        }

        private void FillConsolidatedParameters(Order order, GemotestOrderDetail details, DataSetForReportGemotest dataset)
        {
            if (details.Results == null || details.Results.Count == 0)
                return;

            int num = 0;

            foreach (var result in details.Results)
            {
                if (result == null)
                    continue;

                DataRow row = dataset.ConsolidatedReportParameters.NewRow();

                GemotestOrderDetail.GemotestProductDetail productFind = null;
                if (details.Products != null)
                {
                    if (!string.IsNullOrWhiteSpace(result.OrderProductGuid))
                    {
                        productFind = details.Products.FirstOrDefault(x =>
                            x != null &&
                            string.Equals(x.OrderProductGuid ?? "", result.OrderProductGuid ?? "", StringComparison.OrdinalIgnoreCase));
                    }

                    if (productFind == null && !string.IsNullOrWhiteSpace(result.ServiceId))
                    {
                        productFind = details.Products.FirstOrDefault(x =>
                            x != null &&
                            (
                                string.Equals(x.ProductId ?? "", result.ServiceId ?? "", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(x.ProductCode ?? "", result.ServiceId ?? "", StringComparison.OrdinalIgnoreCase)
                            ));
                    }
                }

                if (productFind != null)
                {
                    row["PROD_CODE"] = productFind.ProductCode ?? string.Empty;
                    row["PROD_NAME"] = productFind.ProductName ?? string.Empty;
                }
                else
                {
                    row["PROD_CODE"] = result.ServiceId ?? string.Empty;
                    row["PROD_NAME"] = !string.IsNullOrWhiteSpace(result.TestRusName)
                        ? result.TestRusName
                        : (result.Name ?? string.Empty);
                }

                var sampleFind = ResolveSampleForResult(details, result);

                if (sampleFind != null && !string.IsNullOrWhiteSpace(sampleFind.BiomName))
                    row["TEST_NAME"] = sampleFind.BiomName;
                else if (!string.IsNullOrWhiteSpace(result.SectionName))
                    row["TEST_NAME"] = result.SectionName;
                else
                    row["TEST_NAME"] = string.Empty;

                row["PARAM_NUMBER"] = num;
                row["PARAM_NAME"] = !string.IsNullOrWhiteSpace(result.TestRusName)
                    ? result.TestRusName
                    : (!string.IsNullOrWhiteSpace(result.Name) ? result.Name : (result.ServiceId ?? string.Empty));

                row["PARAM_UNIT"] = result.MeasurementUnit ?? string.Empty;
                row["PARAM_VALUE_TEXT"] = result.Value ?? string.Empty;
                row["PARAM_REF_TEXT"] = BuildRefText(result);

                row["PARAM_VALUE"] = ParseDoubleOrZero(result.Value);
                row["PARAM_MIN"] = ParseDoubleOrMin(result.RefMin);
                row["PARAM_MAX"] = ParseDoubleOrMax(result.RefMax);

                if ((double)row["PARAM_MIN"] == double.MinValue &&
                    (double)row["PARAM_MAX"] == double.MaxValue)
                {
                    row["PARAM_VALUE"] = 0.0;
                }

                dataset.ConsolidatedReportParameters.Rows.Add(row);
                num++;
            }
        }

        private GemotestSampleDetail ResolveSampleForResult(GemotestOrderDetail details, GemotestResultDetail result)
        {
            if (details == null || result == null || details.Samples == null || details.Samples.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(result.OrderProductGuid))
            {
                var byGuid = details.Samples.FirstOrDefault(x =>
                    x != null &&
                    x.OrderProductGuidList != null &&
                    x.OrderProductGuidList.Contains(result.OrderProductGuid));
                if (byGuid != null)
                    return byGuid;
            }

            if (!string.IsNullOrWhiteSpace(result.ServiceId))
            {
                var product = details.Products != null
                    ? details.Products.FirstOrDefault(x =>
                        x != null &&
                        (
                            string.Equals(x.ProductId ?? "", result.ServiceId ?? "", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x.ProductCode ?? "", result.ServiceId ?? "", StringComparison.OrdinalIgnoreCase)
                        ))
                    : null;

                if (product != null)
                {
                    var byService = details.Samples.FirstOrDefault(x =>
                        x != null &&
                        x.OrderProductGuidList != null &&
                        x.OrderProductGuidList.Contains(product.OrderProductGuid));
                    if (byService != null)
                        return byService;
                }
            }

            return details.Samples.FirstOrDefault(x => x != null);
        }

        private string BuildRefText(GemotestResultDetail result)
        {
            if (result == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(result.RefText))
                return result.RefText;

            if (!string.IsNullOrWhiteSpace(result.RefRange))
                return result.RefRange;

            if (!string.IsNullOrWhiteSpace(result.RefMin) || !string.IsNullOrWhiteSpace(result.RefMax))
                return (result.RefMin ?? "") + " - " + (result.RefMax ?? "");

            return string.Empty;
        }

        private string BuildPlainPatientFio(Order order)
        {
            if (order == null || order.Patient == null)
                return string.Empty;

            return string.Format(
                "{0} {1} {2}",
                order.Patient.Surname ?? string.Empty,
                order.Patient.Name ?? string.Empty,
                order.Patient.Patronimic ?? string.Empty).Trim();
        }

        private double ParseDoubleOrZero(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0.0;

            double parsed;
            if (double.TryParse(
                value.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                     .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator),
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out parsed))
                return parsed;

            return 0.0;
        }

        private double ParseDoubleOrMin(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return double.MinValue;

            double parsed;
            if (double.TryParse(
                value.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                     .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator),
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out parsed))
                return parsed;

            return double.MinValue;
        }

        private double ParseDoubleOrMax(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return double.MaxValue;

            double parsed;
            if (double.TryParse(
                value.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                     .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator),
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out parsed))
                return parsed;

            return double.MaxValue;
        }

        private void FillBlankHeader(Order order, GemotestOrderDetail details, GemotestBlankReportDataSetV2 dataset)
        {
            DataRow row = dataset.BlankTable.NewRow();

            row["DateofFormation"] = order.Date.ToString("dd.MM.yyyy");
            row["DateSampling"] = order.Date.ToString("dd.MM.yyyy");
            row["ClinicName"] = GetClinicNameForBlank(details);
            row["OrderCode"] = order.Number ?? string.Empty;
            row["Pacient_FIO"] = BuildPatientFio(order);
            row["LaboratoryName"] = "Гемотест";
            row["OrderCodeBarcode"] = BuildOrderBarcodeBytes(order.Number ?? string.Empty);

            dataset.BlankTable.Rows.Add(row);
        }

        private void FillPatientParameters(Order order, GemotestOrderDetail details, GemotestBlankReportDataSetV2 dataset)
        {
            if (order.Patient != null)
            {
                AddPatientParameter(dataset, "Дата рождения:", order.Patient.Birthday.ToString("dd.MM.yyyy"));

                if (!string.IsNullOrWhiteSpace(order.Patient.Phone))
                    AddPatientParameter(dataset, "Телефон:", order.Patient.Phone);

                if (!string.IsNullOrWhiteSpace(order.Patient.EMail))
                    AddPatientParameter(dataset, "Email:", order.Patient.EMail);

                if (!string.IsNullOrWhiteSpace(order.Patient.SNILS))
                    AddPatientParameter(dataset, "СНИЛС:", order.Patient.SNILS);
            }

            if (details.Details == null)
                return;

            foreach (var detail in details.Details)
            {
                if (detail == null)
                    continue;

                if (string.IsNullOrWhiteSpace(detail.Value) && string.IsNullOrWhiteSpace(detail.DisplayValue))
                    continue;

                string caption = NormalizeFieldCaption(detail.Name, detail.Code);
                if (string.IsNullOrWhiteSpace(caption))
                    continue;

                string value = !string.IsNullOrWhiteSpace(detail.DisplayValue)
                    ? detail.DisplayValue
                    : (detail.Value ?? string.Empty);

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (IsDuplicatePatientParam(dataset, caption, value))
                    continue;

                AddPatientParameter(dataset, caption, value);
            }
        }

        private void FillProductsForBlank(GemotestOrderDetail details, GemotestBlankReportDataSetV2 dataset)
        {
            if (details.Products == null || details.Products.Count == 0)
                return;

            foreach (var product in details.Products)
            {
                if (product == null)
                    continue;

                var linkedSamples = new List<GemotestSampleDetail>();

                if (details.Samples != null)
                {
                    linkedSamples = details.Samples
                        .Where(s => s != null &&
                                    s.OrderProductGuidList != null &&
                                    s.OrderProductGuidList.Contains(product.OrderProductGuid))
                        .ToList();
                }

                if (linkedSamples.Count == 0)
                {
                    DataRow row = dataset.Products.NewRow();
                    row["ProductCode"] = product.ProductCode ?? string.Empty;
                    row["ProductName"] = product.ProductName ?? string.Empty;
                    row["SubOrderInfo"] = "Образец не определён";
                    dataset.Products.Rows.Add(row);
                    continue;
                }

                foreach (var sample in linkedSamples)
                {
                    DataRow row = dataset.Products.NewRow();
                    row["ProductCode"] = product.ProductCode ?? string.Empty;
                    row["ProductName"] = product.ProductName ?? string.Empty;
                    row["SubOrderInfo"] = BuildSubOrderInfo(sample);
                    dataset.Products.Rows.Add(row);
                }
            }
        }

        private string BuildSubOrderInfo(GemotestSampleDetail sample)
        {
            var lines = new List<string>();

            var line1Parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sample.Barcode))
                line1Parts.Add("ШК: " + sample.Barcode);

            if (!string.IsNullOrWhiteSpace(sample.SampleId))
                line1Parts.Add("Проба №" + sample.SampleId);

            if (!string.IsNullOrWhiteSpace(sample.SampleIdentifier) &&
                !string.Equals(sample.SampleIdentifier, sample.Barcode, StringComparison.OrdinalIgnoreCase))
                line1Parts.Add("Идентификатор: " + sample.SampleIdentifier);

            if (line1Parts.Count > 0)
                lines.Add(string.Join(", ", line1Parts));

            var line2Parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sample.BiomName))
                line2Parts.Add("Биоматериал: " + sample.BiomName);

            if (!string.IsNullOrWhiteSpace(sample.ContName))
                line2Parts.Add("Контейнер: " + sample.ContName);

            if (line2Parts.Count > 0)
                lines.Add(string.Join(", ", line2Parts));

            var line3Parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sample.LocalizationName))
                line3Parts.Add("Локализация: " + sample.LocalizationName);

            if (!string.IsNullOrWhiteSpace(sample.LabCenterId))
                line3Parts.Add("Лаборатория: " + sample.LabCenterId);

            if (line3Parts.Count > 0)
                lines.Add(string.Join(", ", line3Parts));

            if (!string.IsNullOrWhiteSpace(sample.SampleDescription))
                lines.Add("Описание: " + sample.SampleDescription);

            if (lines.Count == 0)
                return "Информация по образцу отсутствует";

            return string.Join(Environment.NewLine, lines);
        }

        private void AddPatientParameter(GemotestBlankReportDataSetV2 dataset, string name, string value)
        {
            if (dataset == null || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                return;

            DataRow row = dataset.PatientParameters.NewRow();
            row["PatientParamName"] = name;
            row["PatientParamValue"] = value;
            dataset.PatientParameters.Rows.Add(row);
        }

        private string BuildPatientFio(Order order)
        {
            if (order == null || order.Patient == null)
                return string.Empty;

            string fio = string.Format(
                "{0} {1} {2}",
                order.Patient.Surname ?? string.Empty,
                order.Patient.Name ?? string.Empty,
                order.Patient.Patronimic ?? string.Empty).Trim();

            string sex = string.Empty;
            if (order.Patient.Sex == Sex.Male)
                sex = "Муж";
            else if (order.Patient.Sex == Sex.Female)
                sex = "Жен";

            if (!string.IsNullOrWhiteSpace(sex))
                return fio + " (" + sex + ")";

            return fio;
        }

        private string GetClinicNameForBlank(GemotestOrderDetail details)
        {
            if (!string.IsNullOrWhiteSpace(details.PriceListName))
                return details.PriceListName;

            if (!string.IsNullOrWhiteSpace(details.PriceList))
                return details.PriceList;

            if (!string.IsNullOrWhiteSpace(details.PriceListCode))
                return details.PriceListCode;

            return "Клиника не указана";
        }

        private byte[] BuildOrderBarcodeBytes(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return new byte[0];

            var options = new EncodingOptions
            {
                Height = 50,
                Width = 260,
                Margin = 2
            };

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = options
            };

            using (Bitmap bitmap = writer.Write(orderNumber))
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private string NormalizeFieldCaption(string name, string code)
        {
            string caption = !string.IsNullOrWhiteSpace(name) ? name.Trim() : (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(caption))
                return string.Empty;

            caption = caption
                .Replace("Пациент: ", "")
                .Replace("Продукт: ", "")
                .Replace("Заявка: ", "")
                .Replace("Образец: ", "");

            caption = caption.Trim();

            if (caption.Length == 0)
                return string.Empty;

            return char.ToUpper(caption[0]) + caption.Substring(1) + ":";
        }

        private bool IsDuplicatePatientParam(GemotestBlankReportDataSetV2 dataset, string caption, string value)
        {
            foreach (DataRow row in dataset.PatientParameters.Rows)
            {
                string existingCaption = row["PatientParamName"] as string;
                string existingValue = row["PatientParamValue"] as string;

                if (string.Equals(existingCaption, caption, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existingValue, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
