using Laboratory.Gemotest.GemotestRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiMed.Laboratory;
using System.Reflection;

namespace Laboratory.Gemotest.SourseClass
{
    public class ProductGemotest : Product
    {
        private readonly Dictionaries _dicts;
public int Type { get; set; }
        public int? ServiceType { get; set; }
        public bool IsBlocked { get; set; }
        public float Price { get; set; }
        public int IncreasePeriod { get; set; }

        public List<DictionaryLocalization> Localization { get; set; } = new List<DictionaryLocalization>();
        public List<DictionaryBiomaterials> BioMaterials { get; set; } = new List<DictionaryBiomaterials>();
        public List<DictionaryTransport> Transports { get; set; } = new List<DictionaryTransport>();
        public ProductGemotest(DictionaryService service, string other_biomaterial = null, Dictionaries dicts = null)
        {
            _dicts = dicts;
ID = service.id;
            Code = service.code;
            Name = service.name;
            Duration = service.execution_period;
            DurationInfo = "Срок выполнения услуги в днях";
            Type = service.type;
            ServiceType = service.service_type;
            IsBlocked = service.is_blocked || (service.service_type == 3);
            Price = service.price;
            IncreasePeriod = service.increase_period;

            LoadRelatedData(service, other_biomaterial);
        }


        private void LoadRelatedData(DictionaryService service, string other_biomaterial)
        {
            if (_dicts == null) return;
if (!string.IsNullOrEmpty(service.localization_id))
            {
                if (_dicts.Localization.TryGetValue(service.localization_id, out var loc) && loc != null)
                    Localization = new List<DictionaryLocalization> { loc };
            }
            if (!string.IsNullOrEmpty(service.transport_id))
            {
                if (_dicts.Transport.TryGetValue(service.transport_id, out var tr) && tr != null)
                    Transports = new List<DictionaryTransport> { tr };
            }
            if (!string.IsNullOrEmpty(service.biomaterial_id))
            {
                LoadSingleBiomaterial(service.biomaterial_id);
            }

            if (ServiceType == 0)
            {
                LoadBiomaterialsFromServiceParameters(service);
            }
            else if (ServiceType == 1 || ServiceType == 2)
            {
                LoadBiomaterialsFromMarketingComplex(service);
            }
            if (service.biomaterial_id == "Drugoe" && !string.IsNullOrEmpty(other_biomaterial))
            {
                var custom = new DictionaryBiomaterials { id = "Drugoe", name = other_biomaterial, archive = 0 };
                if (!BioMaterials.Any(b => b.id == "Drugoe")) BioMaterials.Add(custom);
            }

            BioMaterials = BioMaterials.GroupBy(b => b.id).Select(g => g.First()).ToList();

        }

        private void LoadBiomaterialsFromServiceParameters(DictionaryService service)
        {
            if (_dicts == null) return;

            if (_dicts.ServiceParameters == null ||
                !_dicts.ServiceParameters.TryGetValue(service.id, out var paramsList) ||
                paramsList == null || paramsList.Count == 0)
                return;

            var uniqueIds = paramsList.Select(p => p.biomaterial_id).Distinct().Where(id => !string.IsNullOrEmpty(id)).ToList();
            foreach (var id in uniqueIds)
            {
                LoadSingleBiomaterial(id);
                var param = paramsList.FirstOrDefault(p => p.biomaterial_id == id);
                if (param != null)
                {
                    if (!string.IsNullOrEmpty(param.localization_id) && !Localization.Any(l => l.id == param.localization_id))
                        if (_dicts.Localization.TryGetValue(param.localization_id, out var loc) && loc != null)
                            Localization.Add(loc);
                    if (!string.IsNullOrEmpty(param.transport_id) && !Transports.Any(t => t.id == param.transport_id))
                        if (_dicts.Transport.TryGetValue(param.transport_id, out var tr) && tr != null)
                            Transports.Add(tr);
                }
            }
        }

                private void LoadBiomaterialsFromMarketingComplex(DictionaryService service)
        {
            if (_dicts == null) return;
if (service == null || string.IsNullOrEmpty(service.id))
                return;

            List<DictionaryMarketingComplex> complexItems = null;

            if (ServiceType == 2)
                _dicts.MarketingComplexByComplexId.TryGetValue(service.id, out complexItems);
            else
                _dicts.MarketingComplexByServiceId.TryGetValue(service.id, out complexItems);

            if (complexItems == null || complexItems.Count == 0)
                return;

            var uniqueIds = complexItems
                .Select(m => m.biomaterial_id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            foreach (var id in uniqueIds)
            {
                LoadSingleBiomaterial(id);

                var item = complexItems.FirstOrDefault(c =>
                    c != null && string.Equals(c.biomaterial_id ?? "", id, StringComparison.OrdinalIgnoreCase));

                if (item != null && !string.IsNullOrEmpty(item.localization_id ?? ""))
                {
                    if (_dicts.Localization.TryGetValue(item.localization_id, out var loc) && loc != null &&
                        !Localization.Any(l => string.Equals(l.id, loc.id, StringComparison.OrdinalIgnoreCase)))
                    {
                        Localization.Add(loc);
                    }
                }
            }
        }


        private void LoadSingleBiomaterial(string biomId)
        {
            if (_dicts == null) return;

            _dicts.Biomaterials.TryGetValue(biomId ?? "", out var biom);
            if (biom != null && !BioMaterials.Any(b => b.id == biom.id))
            {
                BioMaterials.Add(biom);
            }
        }

        public string DefaultBioMaterialName => BioMaterials.FirstOrDefault()?.name ?? "";

        public void PrintRelatedData()
        {
            if (ServiceType == 3)
            {
                Console.WriteLine($"=== Продукт Type 3: {Name} (ID: {ID}, Code: {Code}) - Заблокирован для выбора ===");
                return;
            }

            Console.WriteLine($"=== Продукт: {Name} (ID: {ID}, Code: {Code}) ===");
            Console.WriteLine($" Type: {Type}, ServiceType: {ServiceType}, IsBlocked: {IsBlocked}");
            Console.WriteLine($" Price: {Price}, IncreasePeriod: {IncreasePeriod}");

            Console.WriteLine($" Локализации ({Localization.Count}):");
            if (Localization.Any())
            {
                var primaryLoc = Localization.FirstOrDefault();
                Console.WriteLine($"  Основная: ID = {primaryLoc?.id ?? "N/A"}, Name = {primaryLoc?.name ?? "N/A"}");
                foreach (var loc in Localization)
                {
                    Console.WriteLine($"  - ID: {loc.id}, Name: {loc.name}, Archive: {loc.archive}");
                }
            }
            else
            {
                Console.WriteLine("  Нет локализаций");
            }

            Console.WriteLine($" Биоматериалы ({BioMaterials.Count}):");
            if (BioMaterials.Any())
            {
                var primaryBiom = BioMaterials.FirstOrDefault();
                Console.WriteLine($"  Основной: ID = {primaryBiom?.id ?? "N/A"}, Name = {primaryBiom?.name ?? "N/A"}");
                foreach (var biom in BioMaterials)
                {
                    Console.WriteLine($"  - ID: {biom.id}, Name: {biom.name}, Archive: {biom.archive}");
                }
                if (ServiceType == 0) Console.WriteLine("  Логика: Выбрать 1 из списка (PDF стр.17).");
                else if (ServiceType == 2) Console.WriteLine("  Логика: Все обязательные (PDF стр.21).");
            }
            else
            {
                Console.WriteLine("  Нет биоматериалов");
            }

            Console.WriteLine($" Транспорты ({Transports.Count}):");
            if (Transports.Any())
            {
                var primaryTrans = Transports.FirstOrDefault();
                Console.WriteLine($"  Основной: ID = {primaryTrans?.id ?? "N/A"}, Name = {primaryTrans?.name ?? "N/A"}");
                foreach (var trans in Transports)
                {
                    Console.WriteLine($"  - ID: {trans.id}, Name: {trans.name}, Archive: {trans.archive}");
                }
            }
            else
            {
                Console.WriteLine("  Нет транспортов");
            }

            Console.WriteLine("=== Конец продукта ===\n");
        }

        public static void PrintAllProductsRelatedData(List<ProductGemotest> products)
        {
            Console.WriteLine("=== Все продукты и их связанные данные (Type 0/1/2) ===");
            var relevant = products.Where(p => p.ServiceType != 3 && !p.IsBlocked).Take(10).ToList();
            foreach (var p in relevant)
            {
                p.PrintRelatedData();
            }
            Console.WriteLine("=== Конец списка ===");
        }
    }
}
