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
using Laboratory.Gemotest.GemotestRequests; // Для Dictionaries.Directory и Dictionaries.Biomaterials

namespace Laboratory.Gemotest.SourseClass
{
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

        public string DisplayValue { get; set; }
        public GemotestDetail()
        {
            MandatoryProducts = new List<int>();
            OptionalProducts = new List<int>();
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
        public List<GemotestDetail> Details { get; set; }
        public List<GemotestBioMaterial> BioMaterials { get; set; }
        public List<Product> DefectProductList { get; set; }
        public string PriceList { get; set; }
        
        public List<GemotestProductDetail> Products { get; set; }
        public string DefectsMessages { get; set; }
        public GemotestOrderDetail() : base()
        {
            Details = new List<GemotestDetail>();
            BioMaterials = new List<GemotestBioMaterial>();
            DefectProductList = new List<Product>();
            Products = new List<GemotestProductDetail>();

        }

        [Serializable]
        public class GemotestProductDetail
        {
            /// <summary>
            /// Идентификатор позиции заказа
            /// </summary>
            public string OrderProductGuid;
            /// <summary>
            /// Идентификатор продукта
            /// </summary>
            public string ProductId;
            /// <summary>
            /// Код продукта
            /// </summary>
            public string ProductCode;
            /// <summary>
            /// Название продукта
            /// </summary>
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

            // 1. Прямой biomaterial_id из Directory, если он есть и не "Drugoe"
            if (!string.IsNullOrEmpty(service.biomaterial_id) &&
                service.biomaterial_id != "Drugoe")
            {
                var biom = Dictionaries.Biomaterials
                    .FirstOrDefault(b => b.id == service.biomaterial_id);
                if (biom != null)
                    result.Add(biom);
            }

            // 2. service_type 0 → Service_parameters
            if (service.service_type == 0 && Dictionaries.ServiceParameters != null)
            {
                var paramsList = Dictionaries.ServiceParameters
                    .Where(p => p.service_id == service.id)
                    .ToList();

                if (paramsList.Any())
                {
                    var ids = paramsList
                        .Select(p => p.biomaterial_id)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct();

                    foreach (var id in ids)
                    {
                        var biom = Dictionaries.Biomaterials.FirstOrDefault(b => b.id == id);
                        if (biom != null && !result.Any(r => r.id == biom.id))
                            result.Add(biom);
                    }
                }
            }

            // 3. service_type 1/2 → Marketing_complex_composition
            if ((service.service_type == 1 || service.service_type == 2) &&
                Dictionaries.MarketingComplexComposition != null)
            {
                Func<DictionaryMarketingComplex, bool> filter =
                    service.service_type == 2
                        ? new Func<DictionaryMarketingComplex, bool>(m => m.complex_id == service.id)
                        : new Func<DictionaryMarketingComplex, bool>(m => m.service_id == service.id);

                var complexItems = Dictionaries.MarketingComplexComposition
                    .Where(filter)
                    .ToList();

                if (complexItems.Any())
                {
                    var ids = complexItems
                        .Select(m => m.biomaterial_id)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct();

                    foreach (var id in ids)
                    {
                        var biom = Dictionaries.Biomaterials.FirstOrDefault(b => b.id == id);
                        if (biom != null && !result.Any(r => r.id == biom.id))
                            result.Add(biom);
                    }
                }
            }

            // 4. Особый случай: biomaterial_id == "Drugoe"
            if (service.biomaterial_id == "Drugoe" &&
                !string.IsNullOrEmpty(service.other_biomaterial))
            {
                if (!result.Any(b => b.id == "Drugoe"))
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


        /// <summary>
        /// Добавляет биоматериалы, id которых совпадает с biomaterial_id в выбранных продуктах Products.
        /// Связывает с AllProducts (Dictionaries.Directory) по ProductId, затем находит DictionaryBiomaterials по biomaterial_id.
        /// Добавляет в BioMaterials с Mandatory = {индекс продукта}.
        /// Если биоматериал уже существует, добавляет индекс продукта в Mandatory/Chosen.
        /// </summary>
        public void AddBiomaterialsFromProducts()
        {
            if (Products == null || Products.Count == 0)
                return;

            if (Dictionaries.Directory == null || Dictionaries.Biomaterials == null)
                return;

            // Если были старые данные – очищаем
            if (BioMaterials == null)
                BioMaterials = new List<GemotestBioMaterial>();
            else
                BioMaterials.Clear();

            // 1. Собираем связи «продукт -> биоматериалы»
            for (int productIndex = 0; productIndex < Products.Count; productIndex++)
            {
                var product = Products[productIndex];

                var service = Dictionaries.Directory.FirstOrDefault(s => s.id == product.ProductId);
                if (service == null)
                    continue;

                var biomaterialsForService = ResolveBiomaterialsForService(service);
                if (!biomaterialsForService.Any())
                    continue;

                // Комплексы (service_type == 2) – все биоматериалы обязательные
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
                        // Для типов 0 и 1: варианты, из которых надо выбрать один
                        if (!existing.Another.Contains(productIndex) &&
                            !existing.Chosen.Contains(productIndex) &&
                            !existing.Mandatory.Contains(productIndex))
                        {
                            existing.Another.Add(productIndex);
                        }
                    }
                }
            }

            // 2. Для продуктов, где есть выбор (service_type 0/1), назначаем дефолтный биоматериал:
            for (int i = 0; i < Products.Count; i++)
            {
                // Если уже есть обязательный или выбранный – не трогаем
                bool alreadyChosen = BioMaterials.Any(b =>
                    b.Chosen.Contains(i) || b.Mandatory.Contains(i));

                if (alreadyChosen)
                    continue;

                // Берём первый доступный вариант из Another
                var candidate = BioMaterials.FirstOrDefault(b => b.Another.Contains(i));
                if (candidate != null)
                {
                    candidate.Another.Remove(i);
                    candidate.Chosen.Add(i);
                }
            }

            // 3. Подчистим мусор (если вдруг есть биоматериалы, не привязанные ни к одному продукту)
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
                return Encoding.UTF8.GetString(memStream.ToArray()); // Исправлено: ToArray() вместо GetBuffer()
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
            //Удаляем биоматериалы
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