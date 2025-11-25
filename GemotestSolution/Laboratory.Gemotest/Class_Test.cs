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
using Laboratory.Gemotest.SourseClass; // Для ProductGemotest

namespace Laboratory.Gemotest.SourseClass
{
    public class GemotestDetail_test
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
        public GemotestDetail_test()
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

    public class GemotestBioMaterial_test
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Code { get; set; }
        public List<int> Chosen { get; set; }
        public List<int> Another { get; set; }
        public List<int> Mandatory { get; set; }
        public GemotestBioMaterial_test()
        {
            Chosen = new List<int>();
            Another = new List<int>();
            Mandatory = new List<int>();
        }
    }

    // Класс для связи продукта с выбранным биоматериалом и пробиркой (для формы заказа)
    [Serializable]
    public class ProductBiomaterialLink
    {
        public string ProductId { get; set; } // ID продукта
        public string SelectedBiomaterialId { get; set; } // Выбранный биоматериал (один, даже если несколько обязательных)
        public string ContainerCode { get; set; } // Код пробирки (container)
        public string ContainerName { get; set; } // Название пробирки

        public ProductGemotest ProductGemotest { get; set; }
        public DictionaryBiomaterials SelectedBiomaterial { get; set; }
    }

    [Serializable]
    public class GemotestOrderDetail_test : BaseOrderDetail
    {
        public List<GemotestDetail_test> Details { get; set; }
        public List<GemotestBioMaterial_test> BioMaterials { get; set; }
        public List<Product> DefectProductList { get; set; }
        public string PriceList { get; set; }

        public List<GemotestProductDetail> Products { get; set; }
        public List<ProductBiomaterialLink> ProductLinks { get; set; } // Новое: связи продукт-биоматериал-пробирка
        public string DefectsMessages { get; set; }

        public GemotestOrderDetail_test() : base()
        {
            Details = new List<GemotestDetail_test>();
            BioMaterials = new List<GemotestBioMaterial_test>();
            DefectProductList = new List<Product>();
            Products = new List<GemotestProductDetail>();
            ProductLinks = new List<ProductBiomaterialLink>();
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

        /// <summary>
        /// Добавляет биоматериалы из ProductGemotest по ProductId. 
        /// Связывает с Directory для fallback, но использует BioMaterials из ProductGemotest.
        /// Добавляет в ProductLinks выбранный биоматериал и пробирку (по умолчанию пустая).
        /// Для нескольких обязательных — добавляет в Mandatory, но в ProductLinks — один (первый или выбранный).
        /// </summary>
        public void AddBiomaterialsFromProducts(List<ProductGemotest> allProductsGemotest = null)
        {
            if (Products == null || !Products.Any() || Dictionaries.Directory == null || Dictionaries.Biomaterials == null)
                return;

            if (allProductsGemotest == null) allProductsGemotest = new List<ProductGemotest>();

            for (int productIndex = 0; productIndex < Products.Count; productIndex++)
            {
                var product = Products[productIndex];
                var productGemotest = allProductsGemotest.FirstOrDefault(p => p.ID == product.ProductId);
                var service = Dictionaries.Directory.FirstOrDefault(s => s.id == product.ProductId);

                if (productGemotest != null && productGemotest.BioMaterials.Any())
                {
                    // Используем BioMaterials из ProductGemotest
                    foreach (var biomaterialItem in productGemotest.BioMaterials)
                    {
                        var existingBioMat = BioMaterials.FirstOrDefault(bm => bm.Id == biomaterialItem.id);
                        bool isMandatory = productGemotest.ServiceType == 2; // Для комплексов все обязательные

                        if (existingBioMat != null)
                        {
                            if (isMandatory && !existingBioMat.Mandatory.Contains(productIndex))
                                existingBioMat.Mandatory.Add(productIndex);
                        }
                        else
                        {
                            var newBioMat = new GemotestBioMaterial_test
                            {
                                Id = biomaterialItem.id,
                                Code = biomaterialItem.id,
                                Name = biomaterialItem.name,
                                Mandatory = isMandatory ? new List<int> { productIndex } : new List<int>()
                            };
                            BioMaterials.Add(newBioMat);
                        }

                        // Добавляем связь в ProductLinks (один биоматериал на продукт; для нескольких — первый)
                        if (!ProductLinks.Any(pl => pl.ProductId == product.ProductId))
                        {
                            ProductLinks.Add(new ProductBiomaterialLink
                            {
                                ProductId = product.ProductId,
                                SelectedBiomaterialId = biomaterialItem.id, // Первый как выбранный
                                ContainerCode = "", // Пробирка по умолчанию
                                ContainerName = "",
                                ProductGemotest = productGemotest,
                                SelectedBiomaterial = biomaterialItem
                            });
                        }
                    }
                }
                else if (service != null && !string.IsNullOrEmpty(service.biomaterial_id))
                {
                    // Fallback на Directory
                    var biomaterialItem = Dictionaries.Biomaterials.FirstOrDefault(b => b.id == service.biomaterial_id);
                    if (biomaterialItem != null)
                    {
                        var existingBioMat = BioMaterials.FirstOrDefault(bm => bm.Id == biomaterialItem.id);
                        if (existingBioMat != null)
                        {
                            if (!existingBioMat.Mandatory.Contains(productIndex))
                                existingBioMat.Mandatory.Add(productIndex);
                        }
                        else
                        {
                            var newBioMat = new GemotestBioMaterial_test
                            {
                                Id = biomaterialItem.id,
                                Code = biomaterialItem.id,
                                Name = biomaterialItem.name,
                                Mandatory = new List<int> { productIndex }
                            };
                            BioMaterials.Add(newBioMat);
                        }

                        // Связь в ProductLinks
                        if (!ProductLinks.Any(pl => pl.ProductId == product.ProductId))
                        {
                            ProductLinks.Add(new ProductBiomaterialLink
                            {
                                ProductId = product.ProductId,
                                SelectedBiomaterialId = biomaterialItem.id,
                                ContainerCode = "",
                                ContainerName = "",
                                SelectedBiomaterial = biomaterialItem
                            });
                        }
                    }
                }
            }

            BioMaterials = BioMaterials.GroupBy(bm => bm.Id).Select(g => g.First()).ToList();
            ProductLinks = ProductLinks.GroupBy(pl => pl.ProductId).Select(g => g.First()).ToList(); // Один на продукт
        }

        // Метод для обновления выбранного биоматериала и пробирки по продукту (вызов из GUI)
        public void UpdateProductLink(string productId, string selectedBiomaterialId, string containerCode, string containerName)
        {
            var link = ProductLinks.FirstOrDefault(pl => pl.ProductId == productId);
            if (link != null)
            {
                link.SelectedBiomaterialId = selectedBiomaterialId;
                link.ContainerCode = containerCode;
                link.ContainerName = containerName;

                // Обновляем Chosen/Mandatory в BioMaterials
                var biomIndex = BioMaterials.FindIndex(bm => bm.Id == selectedBiomaterialId);
                if (biomIndex >= 0)
                {
                    var productIndex = Products.FindIndex(p => p.ProductId == productId);
                    if (productIndex >= 0)
                    {
                        var biom = BioMaterials[biomIndex];
                        biom.Chosen.Add(productIndex); // Выбранный
                        if (!biom.Mandatory.Contains(productIndex)) biom.Mandatory.Add(productIndex); // Если был обязательным
                    }
                }
            }
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
                new XmlSerializer(typeof(GemotestOrderDetail_test)).Serialize(memStream, this);
                memStream.Position = 0;
                return Encoding.UTF8.GetString(memStream.ToArray());
            }
        }

        public override BaseOrderDetail Unpack(string _Source)
        {
            try
            {
                return (GemotestOrderDetail_test)new XmlSerializer(typeof(GemotestOrderDetail_test)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(_Source)));
            }
            catch (Exception e)
            {
                LogEvent.SaveExceptionToLog(e, GetType().Name);
                return null;
            }
        }

        internal void DeleteProduct(int productIndex)
        {
            List<GemotestBioMaterial_test> BioMaterialsForDelete = new List<GemotestBioMaterial_test>();
            foreach (GemotestBioMaterial_test bioMaterial in BioMaterials)
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
                        bioMaterial.Another[i]--;
                for (int i = 0; i < bioMaterial.Chosen.Count; i++)
                    if (bioMaterial.Chosen[i] > productIndex)
                        bioMaterial.Chosen[i]--;
                for (int i = 0; i < bioMaterial.Mandatory.Count; i++)
                    if (bioMaterial.Mandatory[i] > productIndex)
                        bioMaterial.Mandatory[i]--;
                if (bioMaterial.Chosen.Count == 0 &&
                    bioMaterial.Another.Count == 0 &&
                    bioMaterial.Mandatory.Count == 0)
                    BioMaterialsForDelete.Add(bioMaterial);
            }
            foreach (var bioMaterial in BioMaterialsForDelete)
                BioMaterials.Remove(bioMaterial);

            // Удаляем из ProductLinks
            var productId = Products[productIndex].ProductId;
            ProductLinks.RemoveAll(pl => pl.ProductId == productId);

            DeleteProductFromDetails(productIndex);
        }

        public void DeleteProductFromDetails(int productIndex)
        {
            List<GemotestDetail_test> toDelete = new List<GemotestDetail_test>();
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
            foreach (GemotestDetail_test item in toDelete)
                Details.Remove(item);
        }
    }

}