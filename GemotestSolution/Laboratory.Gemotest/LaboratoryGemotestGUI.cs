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
                        productNew.OrderProductGuid = Guid.NewGuid().ToString();
                        productNew.Id = product.ProductId;
                        productNew.Code = product.ProductCode;
                        productNew.Name = product.ProductName;
                        productNew.ProductGroupGuid = null;

                        BiomaterialGroupForGUI groupForGUI = new BiomaterialGroupForGUI();

                        Console.WriteLine(details.BioMaterials.Count);
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
                                    biomInfo.ContainerCode = transport.id;   // кода в справочнике нет – можно использовать id как код
                                    biomInfo.ContainerName = transport.name; // нормальное отображаемое имя пробирки
                                }
                                else
                                {
                                    biomInfo.ContainerId = "";
                                    biomInfo.ContainerCode = "";
                                    biomInfo.ContainerName = "";
                                }

                                groupForGUI.Biomaterials.Add(biomInfo);
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

                // сохранение информации о выбранных биоматериалах
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
                if (_Action == eOrderAction.RemoveProduct)
                {
                    int productIndex = _OrderModel.ProductsInfo.IndexOf(_Product);
                    _OrderModel.ProductsInfo.Remove(_Product);
                    GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;
                    details.DeleteProduct(productIndex);

                    for (int i = 0; i < _OrderModel.ProductsInfo.Count; i++)
                    {
                        ProductInfoForGUI curProductInfo = _OrderModel.ProductsInfo[i];
                        curProductInfo.OrderProductGuid = i.ToString();
                    }
                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    details.AddBiomaterialsFromProducts();
                    details.DeleteObsoleteDetails();

                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

                    return true;
                    return true;
                }

                if (_Action == eOrderAction.AddProduct)
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

                    GemotestOrderDetail details = (GemotestOrderDetail)_Order.OrderDetail;
                    ProductInfoForGUI productNew = products.Find(x => x.OrderProductGuid == form.selectedProductGuid);
                    productNew.OrderProductGuid = details.Products.Count.ToString();
                    _OrderModel.ProductsInfo.Add(productNew);

                    GemotestProductDetail productInfo = new GemotestProductDetail()
                    {
                        OrderProductGuid = productNew.OrderProductGuid,
                        ProductId = productNew.Id,
                        ProductCode = productNew.Code,
                        ProductName = productNew.Name
                    };

                    SiMed.Laboratory.Product orderProductNew = new SiMed.Laboratory.Product()
                    {
                        ID = productNew.Id,
                        Code = productNew.Code,
                        Name = productNew.Name
                    };

                    details.Products.Add(productInfo);

                   /* details.FillBioMaterials(infoBank, details.Products);

                    List<BiomaterialGroupForGUI> biomaterialGroups;
                    if (!GetBiomaterialsGroupsForProductInfo(_Order, details.Products.Count - 1, out biomaterialGroups))
                        return false;

                    productNew.BiomaterialGroups = biomaterialGroups;*/

                    if (!SaveOrderModelForGUIToDetails(_Order, _OrderModel))
                        return false;

                    if (!GenerateSamples(_Order, _OrderModel))
                        return false;

                    if (!GenerateFields(_Order, _OrderModel))
                        return false;

                    return true;
                }
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
    }
}
