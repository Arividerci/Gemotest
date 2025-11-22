using Laboratory.Gemotest.GemotestRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Laboratory.Gemotest.GemotestRequests;
using SiMed.Laboratory;
using System.Reflection;

namespace Laboratory.Gemotest.SourseClass
{
    public class ProductGemotest : Product
    {
        public int Type { get; set; } // type из Directory (0-основная, 1-доп., 2-маркетинг, 3-тех.комплекс)
        public int? ServiceType { get; set; } // service_type из Directory
        public bool IsBlocked { get; set; }
        public float Price { get; set; }
        public int IncreasePeriod { get; set; }

        // Оставляем только списки готовых классов для localization, biomaterials, transport
        public List<DictionaryLocalization> Localization { get; set; } = new List<DictionaryLocalization>(); // Локализации
        public List<DictionaryBiomaterials> BioMaterials { get; set; } = new List<DictionaryBiomaterials>(); // Биоматериалы
        public List<DictionaryTransport> Transports { get; set; } = new List<DictionaryTransport>(); // Транспорт

        public ProductGemotest(DictionaryService service, string other_biomaterial = null)
        {
            ID = service.id;
            Code = service.code;
            Name = service.name;
            Duration = service.execution_period;
            DurationInfo = "Duration - Срок выполнения услуги в днях";
            Type = service.type;
            ServiceType = service.service_type;
            IsBlocked = service.is_blocked;
            Price = service.price;
            IncreasePeriod = service.increase_period;

            // Логика загрузки списков в зависимости от Type/ServiceType
            LoadRelatedData(service, other_biomaterial);
        }

        /// <summary>
        /// Загружает списки Localization, BioMaterials, Transports в зависимости от Type/ServiceType.
        /// </summary>
        private void LoadRelatedData(DictionaryService service, string other_biomaterial)
        {
            // Биоматериалы (всегда из Directory.biomaterial_id)
            if (!string.IsNullOrEmpty(service.biomaterial_id))
            {
                BioMaterials = Dictionaries.Biomaterials.Where(b => b.id == service.biomaterial_id).ToList();
            }
            if (service.biomaterial_id == "Drugoe" && !string.IsNullOrEmpty(other_biomaterial))
            {
                // Для "Drugoe" — добавляем кастомный, но поскольку это List<DictionaryBiomaterials>, можно добавить dummy или пропустить
                BioMaterials.Add(new DictionaryBiomaterials { id = "Drugoe", name = other_biomaterial });
            }

            // Логика по Type/ServiceType для localization и transport (биоматериалы уже загружены)
            if (Type == 0)
            {
                // Для Type=0: Проверяем id в Directory и Marketing_complex_composition (для комплексов)
                LoadFromDirectoryAndMarketing(service);
            }
            else
            {
                // Для других Type: Смотрим ServiceType
                if (ServiceType == 0 || ServiceType == 3)
                {
                    // ServiceType=0 или 3: Из Service_parameters
                    LoadFromServiceParameters(service);
                }
                else if (ServiceType == 1 || ServiceType == 2)
                {
                    // ServiceType=1 или 2: Из Marketing_complex_composition
                    LoadFromMarketingComplex(service);
                }
            }

            // Транспорт (всегда из Directory.transport_id, если не переопределено в логике выше)
            if (!string.IsNullOrEmpty(service.transport_id) && !Transports.Any())
            {
                Transports = Dictionaries.Transport.Where(t => t.id == service.transport_id).ToList();
            }
        }

        /// <summary>
        /// Загрузка из Directory и Marketing_complex_composition (для Type=0)
        /// </summary>
        private void LoadFromDirectoryAndMarketing(DictionaryService service)
        {
            // Из Directory (локализация и транспорт по умолчанию)
            if (!string.IsNullOrEmpty(service.localization_id))
            {
                Localization = Dictionaries.Localization.Where(l => l.id == service.localization_id).ToList();
            }

            // Дополнение из Marketing_complex_composition (если услуга часть комплекса)
            var complexItems = Dictionaries.MarketingComplexComposition.Where(m => m.service_id == service.id).ToList();
            if (complexItems.Any())
            {
                // Пример: Если комплекс переопределяет localization/transport — добавить/заменить в списках
                var complex = complexItems.First();
                if (!string.IsNullOrEmpty(complex.localization_id ?? "")) // Предполагаем поле в модели
                {
                    var loc = Dictionaries.Localization.FirstOrDefault(l => l.id == complex.localization_id);
                    if (loc != null && !Localization.Any(l => l.id == loc.id))
                        Localization.Add(loc);
                }
                // Аналогично для transport_id, если есть в модели MarketingComplex (добавь, если нужно)
            }
        }

        /// <summary>
        /// Загрузка из Service_parameters (для ServiceType=0 или 3)
        /// </summary>
        private void LoadFromServiceParameters(DictionaryService service)
        {
            var paramsList = Dictionaries.ServiceParameters.Where(p => p.service_id == service.id).ToList();
            if (paramsList.Any())
            {
                var param = paramsList.First(); // Берем первый релевантный (или фильтр по type)
                if (!string.IsNullOrEmpty(param.localization_id ?? ""))
                {
                    var loc = Dictionaries.Localization.FirstOrDefault(l => l.id == param.localization_id);
                    if (loc != null && !Localization.Any(l => l.id == loc.id))
                        Localization.Add(loc);
                }
                // Biomaterial уже загружен; transport аналогично, если в param.transport_id (добавь поле)
            }
        }

        /// <summary>
        /// Загрузка из Marketing_complex_composition (для ServiceType=1 или 2)
        /// </summary>
        private void LoadFromMarketingComplex(DictionaryService service)
        {
            var complexItems = Dictionaries.MarketingComplexComposition.Where(m => m.complex_id == service.id || m.service_id == service.id).ToList(); // По complex_id или service_id
            if (complexItems.Any())
            {
                var complex = complexItems.First();
                if (!string.IsNullOrEmpty(complex.localization_id ?? "")) // Предполагаем поле в модели
                {
                    var loc = Dictionaries.Localization.FirstOrDefault(l => l.id == complex.localization_id);
                    if (loc != null && !Localization.Any(l => l.id == loc.id))
                        Localization.Add(loc);
                }
                // Аналогично для biomaterial_id/transport_id из комплекса (добавь поля в модель)
            }
        }

        public string DefaultBioMaterialName => BioMaterials.FirstOrDefault()?.name ?? "";

        public void PrintRelatedData()
        {
            Console.WriteLine($"=== Продукт: {Name} (ID: {ID}, Code: {Code}) ===");
            Console.WriteLine($"  Type: {Type}, ServiceType: {ServiceType}, IsBlocked: {IsBlocked}");
            Console.WriteLine($"  Price: {Price}, IncreasePeriod: {IncreasePeriod}");

            // Вывод локализации (из списка)
            Console.WriteLine($"  Локализации ({Localization.Count}):");
            if (Localization.Any())
            {
                var primaryLoc = Localization.FirstOrDefault();
                Console.WriteLine($"    Основная: ID = {primaryLoc?.id ?? "N/A"}, Name = {primaryLoc?.name ?? "N/A"}");
                foreach (var loc in Localization)
                {
                    Console.WriteLine($"    - ID: {loc.id}, Name: {loc.name}, Archive: {loc.archive}");
                }
            }
            else
            {
                Console.WriteLine("    Нет локализаций");
            }

            // Вывод биоматериалов (из списка)
            Console.WriteLine($"  Биоматериалы ({BioMaterials.Count}):");
            if (BioMaterials.Any())
            {
                var primaryBiom = BioMaterials.FirstOrDefault();
                Console.WriteLine($"    Основной: ID = {primaryBiom?.id ?? "N/A"}, Name = {primaryBiom?.name ?? "N/A"}");
                foreach (var biom in BioMaterials)
                {
                    Console.WriteLine($"    - ID: {biom.id}, Name: {biom.name}, Archive: {biom.archive}");
                }
            }
            else
            {
                Console.WriteLine("    Нет биоматериалов");
            }

            // Вывод транспорта (из списка)
            Console.WriteLine($"  Транспорты ({Transports.Count}):");
            if (Transports.Any())
            {
                var primaryTrans = Transports.FirstOrDefault();
                Console.WriteLine($"    Основной: ID = {primaryTrans?.id ?? "N/A"}, Name = {primaryTrans?.name ?? "N/A"}");
                foreach (var trans in Transports)
                {
                    Console.WriteLine($"    - ID: {trans.id}, Name: {trans.name}, Archive: {trans.archive}");
                }
            }
            else
            {
                Console.WriteLine("    Нет транспортов");
            }

            Console.WriteLine("=== Конец продукта ===\n");
        }

        public static void PrintAllProductsRelatedData(List<ProductGemotest> products)
        {
            Console.WriteLine("=== Все продукты и их связанные данные ===");
            for (int index = 300; index < 310; index++) // Уменьшено для примера
            {
                if (index < products.Count)
                    products[index].PrintRelatedData();
            }
            Console.WriteLine("=== Конец списка ===");
        }
    }
}