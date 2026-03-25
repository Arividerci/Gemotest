using SiMed.Clinic;
using SiMed.Clinic.Logger;
using SiMed.Laboratory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Laboratory.Gemotest.GemotestRequests;

namespace Laboratory.Gemotest.SourseClass
{
    [Serializable]
    public class GemotestSampleDetail
    {
        public string OrderSampleGuid;
        public string Barcode;

        public string SampleId;
        public string SampleIdentifier;

        public string SampleDescription;

        public string BiomId;
        public string BiomCode;
        public string BiomName;

        public string ContId;
        public string ContCode;
        public string ContName;

        public string LocalizationId;
        public string LocalizationName;

        public string TransportId;
        public string TransportName;

        public string LabCenterId;

        public List<string> OrderProductGuidList;

        public GemotestSampleDetail()
        {
            OrderProductGuidList = new List<string>();
        }
    }

    public class GemotestDetail
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public List<int> MandatoryProducts { get; set; }
        public List<int> OptionalProducts { get; set; }
        public int? dictionaryId { get; set; } = null;
        public string regex { get; set; } = null;
        public bool replaced { get; set; } = false;
        public bool isStdField { get; set; } = false;
        public List<GemotestResultDetail> Results { get; set; }
        public List<GemotestAttachmentDetail> Attachments { get; set; }

        public string DisplayValue { get; set; }
        public GemotestDetail()
        {
            MandatoryProducts = new List<int>();
            OptionalProducts = new List<int>();
            Results = new List<GemotestResultDetail>();
            Attachments = new List<GemotestAttachmentDetail>();
        }
        public bool IsValid(bool _EmptyAvailable, out string _ErrorText)
        {
            _ErrorText = "";
            if (!_EmptyAvailable && String.IsNullOrEmpty(Value))
            {
                _ErrorText = "Поле обязательно для заполнения";
                return false;
            }
            if (!String.IsNullOrEmpty(Value) && !String.IsNullOrEmpty(regex))
            {
                Regex r = new Regex(regex);
                if (!r.IsMatch(Value))
                {
                    _ErrorText = $"Поле не соответствует формату заполнения '{regex}'";
                    return false;
                }
            }
            return true;
        }
    }

    [Serializable]
    public class GemotestResultDetail
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
        public string OrderProductGuid;
    }

    [Serializable]
    public class GemotestAttachmentDetail
    {
        public string SectionName;
        public string FileUrl;
        public string OrderProductGuid;
        public string OrderSampleGuid;
        public string DisplayName;
    }

    public class GemotestBioMaterial
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Code { get; set; }
        public List<int> Chosen { get; set; }
        public List<int> Another { get; set; }
        public List<int> Mandatory { get; set; }
        public GemotestBioMaterial()
        {
            Chosen = new List<int>();
            Another = new List<int>();
            Mandatory = new List<int>();
        }
    }

    [Serializable]
    public class GemotestOrderDetail : BaseOrderDetail
    {
        public string ResultsRawXml { get; set; }
        public string ResultsOrderNum { get; set; }
        public string ResultsExtNum { get; set; }
        public List<GemotestDetail> Details { get; set; }
        public List<GemotestBioMaterial> BioMaterials { get; set; }
        public List<Product> DefectProductList { get; set; }
        public string PriceList { get; set; }
        public string PriceListCode { get; set; }  
        public string PriceListName { get; set; } 
        public string PriceListNum { get; set; }
        public List<GemotestSampleDetail> Samples { get; set; }

        [XmlIgnore]
        public Dictionaries Dicts { get; set; }
        public List<GemotestProductDetail> Products { get; set; }
        public string DefectsMessages { get; set; }
        public List<GemotestResultDetail> Results { get; set; }
        public List<GemotestAttachmentDetail> Attachments { get; set; }
        public GemotestOrderDetail() : base()
        {
            ResultsRawXml = string.Empty;
            ResultsOrderNum = string.Empty;
            ResultsExtNum = string.Empty;
            Samples = new List<GemotestSampleDetail>();
            Details = new List<GemotestDetail>();
            BioMaterials = new List<GemotestBioMaterial>();
            DefectProductList = new List<Product>();
            Products = new List<GemotestProductDetail>();
            Results = new List<GemotestResultDetail>();
            Attachments = new List<GemotestAttachmentDetail>();
            LaboratoryType = (LaboratoryType)24;

        }

        [Serializable]
        public class GemotestProductDetail
        {

            public string OrderProductGuid;

            public string ProductId;

            public string ProductCode;


            public string ProductName;
            public Product AsProduct()
            {
                Product p = new Product(ProductName, ProductId, ProductCode);
                return p;
            }
        }

                private List<DictionaryBiomaterials> ResolveBiomaterialsForService(DictionaryService service)
        {
            var result = new List<DictionaryBiomaterials>();
            if (service == null)
                return result;

            if (Dicts == null)
                return result;

            if (!string.IsNullOrEmpty(service.biomaterial_id) &&
                !string.Equals(service.biomaterial_id, "Drugoe", StringComparison.OrdinalIgnoreCase))
            {
                if (Dicts.Biomaterials.TryGetValue(service.biomaterial_id, out var biom) && biom != null)
                    result.Add(biom);
            }


            if (service.service_type == 0 && Dicts.ServiceParameters != null)
            {
                if (Dicts.ServiceParameters.TryGetValue(service.id, out var paramsList) &&
                    paramsList != null && paramsList.Count > 0)
                {
                    var ids = paramsList
                        .Select(p => p.biomaterial_id)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var id in ids)
                    {
                        if (Dicts.Biomaterials.TryGetValue(id, out var biom) && biom != null &&
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
                    if (Dicts.MarketingComplexByComplexId != null)
                        Dicts.MarketingComplexByComplexId.TryGetValue(service.id, out complexItems);
                }
                else
                {
                    if (Dicts.MarketingComplexByServiceId != null)
                        Dicts.MarketingComplexByServiceId.TryGetValue(service.id, out complexItems);
                }

                if (complexItems != null && complexItems.Count > 0)
                {
                    var ids = complexItems
                        .Select(m => m.biomaterial_id)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var id in ids)
                    {
                        if (Dicts.Biomaterials.TryGetValue(id, out var biom) && biom != null &&
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


        public void AddBiomaterialsFromProducts()
        {
            if (Products == null || Products.Count == 0)
                return;

            if (Dicts == null)
                return;

            if (Dicts.Directory == null || Dicts.Biomaterials == null)
                return;

            if (BioMaterials == null)
                BioMaterials = new List<GemotestBioMaterial>();
            else
                BioMaterials.Clear();


            for (int productIndex = 0; productIndex < Products.Count; productIndex++)
            {
                var product = Products[productIndex];

                if (!Dicts.Directory.TryGetValue(product.ProductId, out var service) || service == null)
                    continue;

                var biomaterialsForService = ResolveBiomaterialsForService(service);
                if (!biomaterialsForService.Any())
                    continue;


                bool allMandatory = service.service_type == 2;

                foreach (var biom in biomaterialsForService)
                {
                    if (biom == null || string.IsNullOrEmpty(biom.id))
                        continue;

                    var existing = BioMaterials.FirstOrDefault(b => b.Id == biom.id);
                    if (existing == null)
                    {
                        existing = new GemotestBioMaterial
                        {
                            Id = biom.id,
                            Code = biom.id,
                            Name = biom.name
                        };
                        BioMaterials.Add(existing);
                    }

                    if (allMandatory)
                    {
                        if (!existing.Mandatory.Contains(productIndex))
                            existing.Mandatory.Add(productIndex);
                        if (!existing.Chosen.Contains(productIndex))
                            existing.Chosen.Add(productIndex);
                    }
                    else
                    {

                        if (!existing.Another.Contains(productIndex) &&
                            !existing.Chosen.Contains(productIndex) &&
                            !existing.Mandatory.Contains(productIndex))
                        {
                            existing.Another.Add(productIndex);
                        }
                    }
                }
            }

            for (int i = 0; i < Products.Count; i++)
            {

                bool alreadyChosen = BioMaterials.Any(b =>
                    b.Chosen.Contains(i) || b.Mandatory.Contains(i));

                if (alreadyChosen)
                    continue;


                var candidate = BioMaterials.FirstOrDefault(b => b.Another.Contains(i));
                if (candidate != null)
                {
                    candidate.Another.Remove(i);
                    candidate.Chosen.Add(i);
                }
            }

            BioMaterials = BioMaterials
                .Where(b => b.Chosen.Count > 0 || b.Another.Count > 0 || b.Mandatory.Count > 0)
                .ToList();

             Console.WriteLine($"[AddBiomaterialsFromProducts] BioMaterials.Count = {BioMaterials.Count}");
        }

        public void DeleteObsoleteDetails()
        {
            List<int> toDelete = new List<int>();
            for (int i = 0; i < Details.Count; i++)
                if (Details[i].MandatoryProducts.Count == 0 && Details[i].OptionalProducts.Count == 0)
                    toDelete.Add(i);
            foreach (int index in toDelete)
                Details.RemoveAt(index);
        }
        List<int> FindIndexesByCode(string code, OrderItemsCollection products)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < products.Count; i++)
                if (products[i].Product.Code == code)
                    indexes.Add(i);
            return indexes;
        }

        public override string Pack()
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                new XmlSerializer(typeof(GemotestOrderDetail)).Serialize(memStream, this);
                memStream.Position = 0;
                return Encoding.UTF8.GetString(memStream.ToArray());
            }
        }
        public override BaseOrderDetail Unpack(string _Source)
        {
            try
            {
                return (GemotestOrderDetail)new XmlSerializer(typeof(GemotestOrderDetail)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(_Source)));
            }
            catch (Exception e)
            {
                LogEvent.SaveExceptionToLog(e, GetType().Name);
                return null;
            }
        }

        internal void DeleteProduct(int productIndex)
        {

            List<GemotestBioMaterial> BioMaterialsForDelete = new List<GemotestBioMaterial>();
            foreach (GemotestBioMaterial bioMaterial in BioMaterials)
            {
                for (int i = 0; i < bioMaterial.Another.Count; i++)
                    if (bioMaterial.Another[i] == productIndex)
                    {
                        bioMaterial.Another.RemoveAt(i);
                        break;
                    }
                for (int i = 0; i < bioMaterial.Chosen.Count; i++)
                    if (bioMaterial.Chosen[i] == productIndex)
                    {
                        bioMaterial.Chosen.RemoveAt(i);
                        break;
                    }
                for (int i = 0; i < bioMaterial.Mandatory.Count; i++)
                    if (bioMaterial.Mandatory[i] == productIndex)
                    {
                        bioMaterial.Mandatory.RemoveAt(i);
                        break;
                    }

                for (int i = 0; i < bioMaterial.Another.Count; i++)
                    if (bioMaterial.Another[i] > productIndex)
                        bioMaterial.Another[i] = bioMaterial.Another[i] - 1;
                for (int i = 0; i < bioMaterial.Chosen.Count; i++)
                    if (bioMaterial.Chosen[i] > productIndex)
                        bioMaterial.Chosen[i] = bioMaterial.Chosen[i] - 1;
                for (int i = 0; i < bioMaterial.Mandatory.Count; i++)
                    if (bioMaterial.Mandatory[i] > productIndex)
                        bioMaterial.Mandatory[i] = bioMaterial.Mandatory[i] - 1;
                if (bioMaterial.Chosen.Count == 0 &&
                    bioMaterial.Another.Count == 0 &&
                    bioMaterial.Mandatory.Count == 0)
                    BioMaterialsForDelete.Add(bioMaterial);
            }
            foreach (var bioMaterial in BioMaterialsForDelete)
                BioMaterials.Remove(bioMaterial);

            DeleteProductFromDetails(productIndex);
        }

        public List<string> GetServiceIdsForCreateOrder()
        {
            var result = new List<string>();

            if (Dicts == null)
                return result;
            if (Products == null)
                return result;

            foreach (var p in Products)
            {
                if (string.IsNullOrEmpty(p.ProductId))
                    continue;

                if (!Dicts.Directory.TryGetValue(p.ProductId, out var service) || service == null)
                    continue;

                if (service.service_type == 2)
                {
                    if (!result.Contains(p.ProductId))
                        result.Add(p.ProductId);

                    if (Dicts.MarketingComplexByComplexId != null &&
                        Dicts.MarketingComplexByComplexId.TryGetValue(p.ProductId, out var complexItems) &&
                        complexItems != null && complexItems.Count > 0)
                    {
                        foreach (var item in complexItems)
                        {
                            if (!string.IsNullOrEmpty(item.service_id) &&
                                !result.Contains(item.service_id))
                            {
                                result.Add(item.service_id);
                            }
                        }
                    }
                }
                else
                {
                    if (!result.Contains(p.ProductId))
                        result.Add(p.ProductId);
                }
            }

            return result;
        }


        public void DeleteProductFromDetails(int productIndex)
        {
            List<GemotestDetail> toDelete = new List<GemotestDetail>();
            for (int i = 0; i < Details.Count; i++)
            {
                if (Details[i].MandatoryProducts.Contains(productIndex) &&
                    Details[i].MandatoryProducts.Count == 1 &&
                    Details[i].OptionalProducts.Count == 0 ||
                    Details[i].OptionalProducts.Contains(productIndex) &&
                    Details[i].MandatoryProducts.Count == 0 &&
                    Details[i].OptionalProducts.Count == 1)
                {
                    toDelete.Add(Details[i]);
                    continue;
                }
                for (int j = 0; j < Details[i].MandatoryProducts.Count; j++)
                    if (Details[i].MandatoryProducts[j] == productIndex)
                        Details[i].MandatoryProducts.RemoveAt(j);
                    else if (Details[i].MandatoryProducts[j] > productIndex)
                        Details[i].MandatoryProducts[j]--;
                for (int j = 0; j < Details[i].OptionalProducts.Count; j++)
                    if (Details[i].OptionalProducts[j] == productIndex)
                        Details[i].OptionalProducts.RemoveAt(j);
                    else if (Details[i].OptionalProducts[j] > productIndex)
                        Details[i].OptionalProducts[j]--;
            }
            foreach (GemotestDetail item in toDelete)
                Details.Remove(item);
        }
    }
}
