using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Laboratory.Gemotest.GemotestRequests
{
    public static class Dictionaries
    {

        private static string filePath = $@"C:\Users\Night\AppData\Симплекс\СиМед - Клиника\GemotestDictionaries\10003\";

        public static List<DictionaryBiomaterials> Biomaterials { get; private set; } = new List<DictionaryBiomaterials>();
        public static List<DictionaryTransport> Transport { get; private set; } = new List<DictionaryTransport>();
        public static List<DictionaryLocalization> Localization { get; private set; } = new List<DictionaryLocalization>();
        public static List<DictionaryService_group> ServiceGroup { get; private set; } = new List<DictionaryService_group>();
        public static List<DictionaryService_parameters> ServiceParameters { get; private set; } = new List<DictionaryService_parameters>();
        public static List<DictionaryService> Directory { get; private set; } = new List<DictionaryService>();
        public static List<DictionaryTests> Tests { get; private set; } = new List<DictionaryTests>();
        public static List<DictionarySamplesServices> SamplesServices { get; private set; } = new List<DictionarySamplesServices>();
        public static List<DictionarySamples> Samples { get; private set; } = new List<DictionarySamples>();
        public static List<DictionaryProcessingRules> ProcessingRules { get; private set; } = new List<DictionaryProcessingRules>();
        public static List<DictionaryServicesAllInterlocks> ServicesAllInterlocks { get; private set; } = new List<DictionaryServicesAllInterlocks>();
        public static List<DictionaryMarketingComplex> MarketingComplexComposition { get; private set; } = new List<DictionaryMarketingComplex>();
        public static List<DictionaryServicesGroupAnalogs> ServicesGroupAnalogs { get; private set; } = new List<DictionaryServicesGroupAnalogs>();
        public static List<DictionaryServiceAutoInsert> ServiceAutoInsert { get; private set; } = new List<DictionaryServiceAutoInsert>();
        public static List<DictionaryServicesSupplementals> ServicesSupplementals { get; private set; } = new List<DictionaryServicesSupplementals>();

        public static bool Unpack(string path)
        {
            if (!string.IsNullOrEmpty(path))
                filePath = path;

            try
            {
                // Biomaterials
                string biomatContent = File.ReadAllText(filePath + "Biomaterials.xml");
                Biomaterials = DictionaryBiomaterials.Parse(biomatContent);

                // Transport
                string transportContent = File.ReadAllText(Path.Combine(filePath, "Transport.xml"));
                Transport = DictionaryTransport.Parse(transportContent);

                // Localization
                string locContent = File.ReadAllText(Path.Combine(filePath, "Localization.xml"));
                Localization = DictionaryLocalization.Parse(locContent);

                // Service_group
                string sgContent = File.ReadAllText(Path.Combine(filePath, "Service_group.xml"));
                ServiceGroup = DictionaryService_group.Parse(sgContent);

                // Service_parameters
                string spContent = File.ReadAllText(Path.Combine(filePath, "Service_parameters.xml"));
                ServiceParameters = DictionaryService_parameters.Parse(spContent);

                // Directory (услуги)
                string dirContent = File.ReadAllText(Path.Combine(filePath, "Directory.xml"));
                Directory = DictionaryService.Parse(dirContent);

                // Tests
                string testsContent = File.ReadAllText(Path.Combine(filePath, "Tests.xml"));
                Tests = DictionaryTests.Parse(testsContent);

                // Samples_services
                string ssContent = File.ReadAllText(Path.Combine(filePath, "Samples_services.xml"));
                SamplesServices = DictionarySamplesServices.Parse(ssContent);

                // Samples
                string sampContent = File.ReadAllText(Path.Combine(filePath, "Samples.xml"));
                Samples = DictionarySamples.Parse(sampContent);

                // Processing_rules
                string prContent = File.ReadAllText(Path.Combine(filePath, "Processing_rules.xml"));
                ProcessingRules = DictionaryProcessingRules.Parse(prContent);

                // Services_all_interlocks
                string saiContent = File.ReadAllText(Path.Combine(filePath, "Services_all_interlocks.xml"));
                ServicesAllInterlocks = DictionaryServicesAllInterlocks.Parse(saiContent);


                // Marketing_complex_composition (используем статический парсер)
                string mccContent = File.ReadAllText(Path.Combine(filePath, "Marketing_complex_composition.xml"));
                MarketingComplexComposition = DictionaryMarketingComplex.Parse(mccContent);

                // Services_group_analogs
                string sgaContent = File.ReadAllText(Path.Combine(filePath, "Services_group_analogs.xml"));
                ServicesGroupAnalogs = DictionaryServicesGroupAnalogs.Parse(sgaContent);

                // Service_auto_insert
                string sai2Content = File.ReadAllText(Path.Combine(filePath, "Service_auto_insert.xml"));
                ServiceAutoInsert = DictionaryServiceAutoInsert.Parse(sai2Content);

                // Services_supplementals
                string ss2Content = File.ReadAllText(Path.Combine(filePath, "Services_supplementals.xml"));
                ServicesSupplementals = DictionaryServicesSupplementals.Parse(ss2Content);

                Console.WriteLine("Все справочники успешно загружены в память.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке справочников в память: {ex.Message}");
                return false;
            }
        }
    }

    public class BaseDictionary
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int archive { get; set; }
    }

    public class DictionaryBiomaterials : BaseDictionary
    {
        public static void PrintToConsole(List<DictionaryBiomaterials> output, int count)
        {
            Console.WriteLine($"Dictionary Biomaterials");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var biomaterial = output[i];
                Console.WriteLine($"id: {biomaterial.id}, name: {biomaterial.name}, archive: {biomaterial.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryBiomaterials> Parse(string xmlContent)
        {
            var biomaterials = new List<DictionaryBiomaterials>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var biomatNodes = doc.SelectNodes("//*[local-name()='item']");

                if (biomatNodes != null && biomatNodes.Count > 0)
                {
                    foreach (XmlNode node in biomatNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = 0;
                        if (archiveNode != null)
                        {
                            var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                archiveValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                            {
                                int.TryParse(archiveNode.InnerText, out archiveValue);
                            }
                        }

                        var biomaterial = new DictionaryBiomaterials
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(biomaterial.id) && biomaterial.id != "*")
                        {
                            biomaterials.Add(biomaterial);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {biomaterials.Count} биоматериалов.");
                    return biomaterials;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return biomaterials;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryBiomaterials>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryBiomaterials>();
            }
        }
    }

    public class DictionaryTransport : BaseDictionary
    {
        public static void PrintToConsole(List<DictionaryTransport> output, int count)
        {
            Console.WriteLine($"Dictionary Transport");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var transport = output[i];
                Console.WriteLine($"id: {transport.id}, name: {transport.name}, archive: {transport.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryTransport> Parse(string xmlContent)
        {
            var transports = new List<DictionaryTransport>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var transportNodes = doc.SelectNodes("//*[local-name()='item']");

                if (transportNodes != null && transportNodes.Count > 0)
                {
                    foreach (XmlNode node in transportNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = 0;
                        if (archiveNode != null)
                        {
                            var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                archiveValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                            {
                                int.TryParse(archiveNode.InnerText, out archiveValue);
                            }
                        }

                        var transport = new DictionaryTransport
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(transport.id) && transport.id != "*")
                        {
                            transports.Add(transport);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {transports.Count}.");
                    return transports;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return transports;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryTransport>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryTransport>();
            }
        }
    }

    public class DictionaryLocalization : BaseDictionary
    {
        public static void PrintToConsole(List<DictionaryLocalization> output, int count)
        {
            Console.WriteLine($"Dictionary Localization");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var localization = output[i];
                Console.WriteLine($"id: {localization.id}, name: {localization.name}, archive: {localization.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryLocalization> Parse(string xmlContent)
        {
            var localizations = new List<DictionaryLocalization>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var localizationNodes = doc.SelectNodes("//*[local-name()='item']");

                if (localizationNodes != null && localizationNodes.Count > 0)
                {
                    foreach (XmlNode node in localizationNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = 0;
                        if (archiveNode != null)
                        {
                            var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                archiveValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                            {
                                int.TryParse(archiveNode.InnerText, out archiveValue);
                            }
                        }

                        var localization = new DictionaryLocalization
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(localization.id) && localization.id != "*")
                        {
                            localizations.Add(localization);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {localizations.Count}.");
                    return localizations;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return localizations;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryLocalization>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryLocalization>();
            }
        }
    }

    public class DictionaryService_group : BaseDictionary
    {
        public string parent_id { get; set; } = string.Empty;

        public static void PrintToConsole(List<DictionaryService_group> output, int count)
        {
            Console.WriteLine($"Dictionary Service_group");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var service_group = output[i];
                Console.WriteLine($"id: {service_group.id}, parent_id: {service_group.parent_id}, name: {service_group.name}, archive: {service_group.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryService_group> Parse(string xmlContent)
        {
            var service_groups = new List<DictionaryService_group>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var serviceGroupNodes = doc.SelectNodes("//*[local-name()='item']");

                if (serviceGroupNodes != null && serviceGroupNodes.Count > 0)
                {
                    foreach (XmlNode node in serviceGroupNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var parentIdNode = node.SelectSingleNode("*[local-name()='parent_id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = 0;
                        if (archiveNode != null)
                        {
                            var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                archiveValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                            {
                                int.TryParse(archiveNode.InnerText, out archiveValue);
                            }
                        }

                        var serviceGroup = new DictionaryService_group
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            parent_id = parentIdNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(serviceGroup.id) && serviceGroup.id != "*")
                        {
                            service_groups.Add(serviceGroup);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {service_groups.Count}.");
                    return service_groups;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return service_groups;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryService_group>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryService_group>();
            }
        }
    }

    public class DictionaryService_parameters
    {
        public string service_id { get; set; } = string.Empty;
        public string biomaterial_id { get; set; } = string.Empty;
        public string localization_id { get; set; } = string.Empty;
        public string transport_id { get; set; } = string.Empty;
        public int archive { get; set; }

        public static void PrintToConsole(List<DictionaryService_parameters> output, int count)
        {
            Console.WriteLine($"Dictionary Service_parameters");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var service_param = output[i];
                Console.WriteLine($"service_id: {service_param.service_id}, biomaterial_id: {service_param.biomaterial_id}, localization_id: {service_param.localization_id}, transport_id: {service_param.transport_id}, archive: {service_param.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryService_parameters> Parse(string xmlContent)
        {
            var service_parameters = new List<DictionaryService_parameters>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var serviceParamNodes = doc.SelectNodes("//*[local-name()='item']");

                if (serviceParamNodes != null && serviceParamNodes.Count > 0)
                {
                    foreach (XmlNode node in serviceParamNodes)
                    {
                        var serviceIdNode = node.SelectSingleNode("*[local-name()='service_id']");
                        var biomaterialIdNode = node.SelectSingleNode("*[local-name()='biomaterial_id']");
                        var localizationIdNode = node.SelectSingleNode("*[local-name()='localization_id']");
                        var transportIdNode = node.SelectSingleNode("*[local-name()='transport_id']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = 0;
                        if (archiveNode != null)
                        {
                            var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                archiveValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                            {
                                int.TryParse(archiveNode.InnerText, out archiveValue);
                            }
                        }

                        var service_param = new DictionaryService_parameters
                        {
                            service_id = serviceIdNode?.InnerText ?? string.Empty,
                            biomaterial_id = biomaterialIdNode?.InnerText ?? string.Empty,
                            localization_id = localizationIdNode?.InnerText ?? string.Empty,
                            transport_id = transportIdNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(service_param.service_id) && service_param.service_id != "*")
                        {
                            service_parameters.Add(service_param);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {service_parameters.Count}.");
                    return service_parameters;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return service_parameters;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryService_parameters>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryService_parameters>();
            }
        }
    }

    // Класс для справочника услуг (get_directory)
    public class DictionaryService
    {
        public string id { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
        public string health_ministry_code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int type { get; set; }
        public int? service_type { get; set; }
        public string biomaterial_id { get; set; } = string.Empty;
        public string other_biomaterial { get; set; } = string.Empty;
        public string localization_id { get; set; } = string.Empty;
        public string other_localization { get; set; } = string.Empty;
        public string transport_id { get; set; } = string.Empty;
        public bool? probe_in_work { get; set; }
        public List<string> additional_tests { get; set; } = new List<string>();
        public int? age_lock_from { get; set; }
        public int? age_lock_to { get; set; }
        public int? pregnancy_week_lock_from { get; set; }
        public int? pregnancy_week_lock_to { get; set; }
        public int? allowed_for_gender { get; set; }
        public string group_id { get; set; } = string.Empty;
        public float price { get; set; }
        public int execution_period { get; set; }
        public bool is_blocked { get; set; }
        public int increase_period { get; set; }
        public bool is_passport_required { get; set; }
        public bool is_address_required { get; set; }

        public static void PrintToConsole(List<DictionaryService> output, int count)
        {
            Console.WriteLine($"Dictionary Services");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var service = output[i];
                Console.WriteLine($"id: {service.id}, name: {service.name}, code: {service.code}, is_blocked: {service.is_blocked}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryService> Parse(string xmlContent)
        {
            var services = new List<DictionaryService>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var serviceNodes = doc.SelectNodes("//*[local-name()='services']/*[local-name()='item'] | //*[local-name()='item']");

                if (serviceNodes != null && serviceNodes.Count > 0)
                {
                    foreach (XmlNode node in serviceNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var codeNode = node.SelectSingleNode("*[local-name()='code']");
                        var healthCodeNode = node.SelectSingleNode("*[local-name()='health_ministry_code']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var typeNode = node.SelectSingleNode("*[local-name()='type']");
                        var serviceTypeNode = node.SelectSingleNode("*[local-name()='service_type']");
                        var biomaterialIdNode = node.SelectSingleNode("*[local-name()='biomaterial_id']");
                        var otherBiomaterialNode = node.SelectSingleNode("*[local-name()='other_biomaterial']");
                        var localizationIdNode = node.SelectSingleNode("*[local-name()='localization_id']");
                        var otherLocalizationNode = node.SelectSingleNode("*[local-name()='other_localization']");
                        var transportIdNode = node.SelectSingleNode("*[local-name()='transport_id']");
                        var probeInWorkNode = node.SelectSingleNode("*[local-name()='probe_in_work']");
                        var additionalTestsNode = node.SelectSingleNode("*[local-name()='additional_tests']");
                        var ageLockFromNode = node.SelectSingleNode("*[local-name()='age_lock_from']");
                        var ageLockToNode = node.SelectSingleNode("*[local-name()='age_lock_to']");
                        var pregnancyWeekLockFromNode = node.SelectSingleNode("*[local-name()='pregnancy_week_lock_from']");
                        var pregnancyWeekLockToNode = node.SelectSingleNode("*[local-name()='pregnancy_week_lock_to']");
                        var allowedForGenderNode = node.SelectSingleNode("*[local-name()='allowed_for_gender']");
                        var groupIdNode = node.SelectSingleNode("*[local-name()='group_id']");
                        var priceNode = node.SelectSingleNode("*[local-name()='price']");
                        var executionPeriodNode = node.SelectSingleNode("*[local-name()='execution_period']");
                        var isBlockedNode = node.SelectSingleNode("*[local-name()='is_blocked']");
                        var increasePeriodNode = node.SelectSingleNode("*[local-name()='increase_period']");
                        var isPassportRequiredNode = node.SelectSingleNode("*[local-name()='is_passport_required']");
                        var isAddressRequiredNode = node.SelectSingleNode("*[local-name()='is_address_required']");

                        var service = new DictionaryService
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            code = codeNode?.InnerText ?? string.Empty,
                            health_ministry_code = healthCodeNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            type = int.TryParse(typeNode?.InnerText, out int t) ? t : 0,
                            service_type = serviceTypeNode != null && int.TryParse(serviceTypeNode.InnerText, out int st) ? st : (int?)null,
                            biomaterial_id = biomaterialIdNode?.InnerText ?? string.Empty,
                            other_biomaterial = otherBiomaterialNode?.InnerText ?? string.Empty,
                            localization_id = localizationIdNode?.InnerText ?? string.Empty,
                            other_localization = otherLocalizationNode?.InnerText ?? string.Empty,
                            transport_id = transportIdNode?.InnerText ?? string.Empty,
                            probe_in_work = probeInWorkNode != null && bool.TryParse(probeInWorkNode.InnerText, out bool piw) ? piw : (bool?)null,
                            age_lock_from = ageLockFromNode != null && int.TryParse(ageLockFromNode.InnerText, out int alf) ? alf : (int?)null,
                            age_lock_to = ageLockToNode != null && int.TryParse(ageLockToNode.InnerText, out int alt) ? alt : (int?)null,
                            pregnancy_week_lock_from = pregnancyWeekLockFromNode != null && int.TryParse(pregnancyWeekLockFromNode.InnerText, out int pwlf) ? pwlf : (int?)null,
                            pregnancy_week_lock_to = pregnancyWeekLockToNode != null && int.TryParse(pregnancyWeekLockToNode.InnerText, out int pwlt) ? pwlt : (int?)null,
                            allowed_for_gender = allowedForGenderNode != null && int.TryParse(allowedForGenderNode.InnerText, out int afg) ? afg : (int?)null,
                            group_id = groupIdNode?.InnerText ?? string.Empty,
                            price = priceNode != null && float.TryParse(priceNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out float p) ? p : 0f,
                            execution_period = executionPeriodNode != null && int.TryParse(executionPeriodNode.InnerText, out int ep) ? ep : 0,
                            is_blocked = isBlockedNode != null && bool.TryParse(isBlockedNode.InnerText, out bool ib) ? ib : false,
                            increase_period = increasePeriodNode != null && int.TryParse(increasePeriodNode.InnerText, out int ip) ? ip : 0,
                            is_passport_required = isPassportRequiredNode != null && bool.TryParse(isPassportRequiredNode.InnerText, out bool ipr) ? ipr : false,
                            is_address_required = isAddressRequiredNode != null && bool.TryParse(isAddressRequiredNode.InnerText, out bool iar) ? iar : false
                        };

                        // Парсинг additional_tests как массив id
                        if (additionalTestsNode != null)
                        {
                            var addTestNodes = additionalTestsNode.SelectNodes("*[local-name()='item']/*[local-name()='id']");
                            if (addTestNodes != null)
                            {
                                foreach (XmlNode addNode in addTestNodes)
                                {
                                    service.additional_tests.Add(addNode?.InnerText ?? string.Empty);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(service.id) && service.id != "*")
                        {
                            services.Add(service);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {services.Count} услуг.");
                    return services;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return services;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryService>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryService>();
            }
        }
    }

    // Класс для справочника тестов (get_tests)
    public class DictionaryTests 
    {
        public string service_id { get; set; } = string.Empty;
        public string test_id { get; set; } = string.Empty;
        public string test_name { get; set; } = string.Empty;

        public static void PrintToConsole(List<DictionaryTests> output, int count)
        {
            Console.WriteLine($"Dictionary Tests");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var test = output[i];
                Console.WriteLine($"id: {test.test_id}, name: {test.test_name}, service_id: {test.service_id}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryTests> Parse(string xmlContent)
        {
            var tests = new List<DictionaryTests>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var testNodes = doc.SelectNodes("//*[local-name()='tests']/*[local-name()='item'] | //*[local-name()='item']");

                if (testNodes != null && testNodes.Count > 0)
                {
                    foreach (XmlNode node in testNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='test_id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='test_name']");
                        var serviceNode = node.SelectSingleNode("*[local-name()='service_id']");

                        var test = new DictionaryTests
                        {
                            test_id = idNode?.InnerText ?? string.Empty,
                            test_name = nameNode?.InnerText ?? string.Empty,
                            service_id = serviceNode?.InnerText ?? string.Empty,
                        };

                        if (!string.IsNullOrEmpty(test.test_id) && test.test_id != "*")
                        {
                            tests.Add(test);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {tests.Count} тестов.");
                    return tests;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return tests;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryTests>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryTests>();
            }
        }
    }

    public class DictionarySamplesServices
    {
        public string service_id { get; set; } = string.Empty;
        public string localization_id { get; set; } = string.Empty;
        public int sample_id { get; set; }
        public string biomaterial_id { get; set; } = string.Empty;
        public string microbiology_biomaterial_id { get; set; } = string.Empty;
        public string test_ids { get; set; } = string.Empty;
        public int service_count { get; set; }
        public int primary_sample_id { get; set; }

        public static void PrintToConsole(List<DictionarySamplesServices> output, int count)
        {
            Console.WriteLine($"Dictionary Samples_services");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var samples_service = output[i];

                Console.WriteLine($"service_id: {samples_service.service_id}, localization_id: {samples_service.localization_id}, sample_id: {samples_service.sample_id}, biomaterial_id: {samples_service.biomaterial_id}, microbiology_biomaterial_id: {samples_service.microbiology_biomaterial_id}, test_ids: {samples_service.test_ids}, service_count: {samples_service.service_count}, primary_sample_id: {samples_service.primary_sample_id}\n");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionarySamplesServices> Parse(string xmlContent)
        {
            var samples_services = new List<DictionarySamplesServices>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var samplesServicesNodes = doc.SelectNodes("//*[local-name()='item']");

                if (samplesServicesNodes != null && samplesServicesNodes.Count > 0)
                {
                    foreach (XmlNode node in samplesServicesNodes)
                    {
                        var serviceIdNode = node.SelectSingleNode("*[local-name()='service_id']");
                        var localizationIdNode = node.SelectSingleNode("*[local-name()='localization_id']");
                        var sampleIdNode = node.SelectSingleNode("*[local-name()='sample_id']");
                        var biomaterialIdNode = node.SelectSingleNode("*[local-name()='biomaterial_id']");
                        var microbiologyBiomaterialIdNode = node.SelectSingleNode("*[local-name()='microbiology_biomaterial_id']");
                        var testIdsNode = node.SelectSingleNode("*[local-name()='test_ids']");
                        var serviceCountNode = node.SelectSingleNode("*[local-name()='service_count']");
                        var primarySampleIdNode = node.SelectSingleNode("*[local-name()='primary_sample_id']");

                        int sampleIdValue = 0;
                        if (sampleIdNode != null)
                        {
                            var nilAttribute = sampleIdNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                sampleIdValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(sampleIdNode.InnerText))
                            {
                                int.TryParse(sampleIdNode.InnerText, out sampleIdValue);
                            }
                        }

                        int serviceCountValue = 0;
                        if (serviceCountNode != null)
                        {
                            var nilAttribute = serviceCountNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                serviceCountValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(serviceCountNode.InnerText))
                            {
                                int.TryParse(serviceCountNode.InnerText, out serviceCountValue);
                            }
                        }

                        int primarySampleIdValue = 0;
                        if (primarySampleIdNode != null)
                        {
                            var nilAttribute = primarySampleIdNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                primarySampleIdValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(primarySampleIdNode.InnerText))
                            {
                                int.TryParse(primarySampleIdNode.InnerText, out primarySampleIdValue);
                            }
                        }

                        var samples_service = new DictionarySamplesServices
                        {
                            service_id = serviceIdNode?.InnerText ?? string.Empty,
                            localization_id = localizationIdNode?.InnerText ?? string.Empty,
                            sample_id = sampleIdValue,
                            biomaterial_id = biomaterialIdNode?.InnerText ?? string.Empty,
                            microbiology_biomaterial_id = microbiologyBiomaterialIdNode?.InnerText ?? string.Empty,
                            test_ids = testIdsNode?.InnerText ?? string.Empty,
                            service_count = serviceCountValue,
                            primary_sample_id = primarySampleIdValue
                        };

                        if (!string.IsNullOrEmpty(samples_service.service_id) && samples_service.service_id != "*")
                        {
                            samples_services.Add(samples_service);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {samples_services.Count}.");
                    return samples_services;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return samples_services;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionarySamplesServices>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionarySamplesServices>();
            }
        }
    }

    // Класс для справочника проб (get_samples)
    public class DictionarySamples : BaseDictionary
    {
        public static void PrintToConsole(List<DictionarySamples> output, int count)
        {
            Console.WriteLine($"Dictionary Samples");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var sample = output[i];
                Console.WriteLine($"id: {sample.id}, name: {sample.name}, archive: {sample.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionarySamples> Parse(string xmlContent)
        {
            var samples = new List<DictionarySamples>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var sampleNodes = doc.SelectNodes("//*[local-name()='samples']/*[local-name()='item'] | //*[local-name()='item']");

                if (sampleNodes != null && sampleNodes.Count > 0)
                {
                    foreach (XmlNode node in sampleNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = ParseArchive(archiveNode);

                        var sample = new DictionarySamples
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(sample.id) && sample.id != "*")
                        {
                            samples.Add(sample);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {samples.Count} проб.");
                    return samples;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return samples;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionarySamples>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionarySamples>();
            }
        }

        private static int ParseArchive(XmlNode archiveNode)
        {
            if (archiveNode == null) return 0;
            var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            if (nilAttribute != null && nilAttribute.Value == "true") return 0;
            return int.TryParse(archiveNode.InnerText, out int value) ? value : 0;
        }
    }

    // Класс для справочника правил обработки (get_processing_rules)
    public class DictionaryProcessingRules
    {
        public int rule_id { get; set; }
        public string rule_name { get; set; } = string.Empty;
        public string parameter_name { get; set; } = string.Empty;
        public string parameter_description { get; set; } = string.Empty;
        public string parameter_type_name { get; set; } = string.Empty;
        public string parameter_type_title { get; set; } = string.Empty;
        public string section_name { get; set; } = string.Empty;
        public string section_title { get; set; } = string.Empty;

        public static void PrintToConsole(List<DictionaryProcessingRules> output, int count)
        {
            Console.WriteLine($"Dictionary Processing_rules");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var rule = output[i];
                Console.WriteLine($"rule_id: {rule.rule_id}, rule_name: {rule.rule_name}, parameter_name: {rule.parameter_name}, parameter_description: {rule.parameter_description}, parameter_type_name: {rule.parameter_type_name}, parameter_type_title: {rule.parameter_type_title}, section_name: {rule.section_name}, section_title: {rule.section_title}\n");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryProcessingRules> Parse(string xmlContent)
        {
            var rules = new List<DictionaryProcessingRules>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var ruleNodes = doc.SelectNodes("//*[local-name()='item']");

                if (ruleNodes != null && ruleNodes.Count > 0)
                {
                    foreach (XmlNode node in ruleNodes)
                    {
                        var ruleIdNode = node.SelectSingleNode("*[local-name()='rule_id']");
                        var ruleNameNode = node.SelectSingleNode("*[local-name()='rule_name']");
                        var parameterNameNode = node.SelectSingleNode("*[local-name()='parameter_name']");
                        var parameterDescriptionNode = node.SelectSingleNode("*[local-name()='parameter_description']");
                        var parameterTypeNameNode = node.SelectSingleNode("*[local-name()='parameter_type_name']");
                        var parameterTypeTitleNode = node.SelectSingleNode("*[local-name()='parameter_type_title']");
                        var sectionNameNode = node.SelectSingleNode("*[local-name()='section_name']");
                        var sectionTitleNode = node.SelectSingleNode("*[local-name()='section_title']");

                        int ruleIdValue = 0;
                        if (ruleIdNode != null)
                        {
                            var nilAttribute = ruleIdNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                            if (nilAttribute != null && nilAttribute.Value == "true")
                            {
                                ruleIdValue = 0;
                            }
                            else if (!string.IsNullOrEmpty(ruleIdNode.InnerText))
                            {
                                int.TryParse(ruleIdNode.InnerText, out ruleIdValue);
                            }
                        }

                        var rule = new DictionaryProcessingRules
                        {
                            rule_id = ruleIdValue,
                            rule_name = ruleNameNode?.InnerText ?? string.Empty,
                            parameter_name = parameterNameNode?.InnerText ?? string.Empty,
                            parameter_description = parameterDescriptionNode?.InnerText ?? string.Empty,
                            parameter_type_name = parameterTypeNameNode?.InnerText ?? string.Empty,
                            parameter_type_title = parameterTypeTitleNode?.InnerText ?? string.Empty,
                            section_name = sectionNameNode?.InnerText ?? string.Empty,
                            section_title = sectionTitleNode?.InnerText ?? string.Empty
                        };

                        if (!string.IsNullOrEmpty(rule.rule_name) && rule.rule_name != "*")
                        {
                            rules.Add(rule);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {rules.Count} правил обработки.");
                    return rules;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return rules;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryProcessingRules>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryProcessingRules>();
            }
        }
    }

    // Класс для справочника блокирующихся сервисов (get_services_all_interlocks)
    public class DictionaryServicesAllInterlocks
    {
        public string serv_id { get; set; } = string.Empty;
        public string blocked_serv { get; set; } = string.Empty;

        public static void PrintToConsole(List<DictionaryServicesAllInterlocks> output, int count)
        {
            Console.WriteLine($"Dictionary Services All Interlocks");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var interlock = output[i];
                Console.WriteLine($"serv_id: {interlock.serv_id}, blocked_serv: {interlock.blocked_serv}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryServicesAllInterlocks> Parse(string xmlContent)
        {
            var interlocks = new List<DictionaryServicesAllInterlocks>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);
                var interlockNodes = doc.SelectNodes("//*[local-name()='elements']/*[local-name()='item']");
                if (interlockNodes != null && interlockNodes.Count > 0)
                {
                    foreach (XmlNode mapNode in interlockNodes)
                    {
                        var keyValuePairs = mapNode.SelectNodes("./*[local-name()='item']");
                        string serv_id = string.Empty;
                        string blocked_serv = string.Empty;

                        foreach (XmlNode pair in keyValuePairs)
                        {
                            var keyNode = pair.SelectSingleNode("*[local-name()='key']");
                            var valueNode = pair.SelectSingleNode("*[local-name()='value']");
                            var key = keyNode?.InnerText ?? string.Empty;
                            var value = valueNode?.InnerText ?? string.Empty;

                            if (key == "serv_id")
                            {
                                serv_id = value;
                            }
                            else if (key == "blocked_serv")
                            {
                                blocked_serv = value;
                            }
                        }

                        if (!string.IsNullOrEmpty(serv_id) && serv_id != "*")
                        {
                            var interlock = new DictionaryServicesAllInterlocks
                            {
                                serv_id = serv_id,
                                blocked_serv = blocked_serv
                            };
                            interlocks.Add(interlock);
                        }
                    }
                    Console.WriteLine($"Успешно обработано {interlocks.Count} интерлоков.");
                    return interlocks;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return interlocks;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryServicesAllInterlocks>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryServicesAllInterlocks>();
            }
        }
    }

    // Класс для справочника маркетинговых комплексов (get_marketing_complex_composition)
    public class DictionaryMarketingComplex
    {
            public string complex_id { get; set; } = string.Empty;
            public string service_id { get; set; } = string.Empty;
            public float price { get; set; }
            public string localization_id { get; set; } = string.Empty;
            public string biomaterial_id { get; set; } = string.Empty;
            public string transport_id { get; set; } = string.Empty;
            public string main_service { get; set; } = string.Empty;

            public static void PrintToConsole(List<DictionaryMarketingComplex> output, int count)
            {
                Console.WriteLine($"Dictionary Marketing_complex_composition");
                for (int i = 0; i < Math.Min(count, output.Count); i++)
                {
                    var item = output[i];
                    Console.WriteLine($"complex_id: {item.complex_id}, service_id: {item.service_id}, price: {item.price}, localization_id: {item.localization_id}, biomaterial_id: {item.biomaterial_id}, transport_id: {item.transport_id}, main_service: {item.main_service}");
                }
                Console.WriteLine("\n");
            }

            public static List<DictionaryMarketingComplex> Parse(string xmlContent)
            {
                var compositions = new List<DictionaryMarketingComplex>();
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xmlContent);

                    var itemNodes = doc.SelectNodes("//*[local-name()='item']");

                    if (itemNodes != null && itemNodes.Count > 0)
                    {
                        foreach (XmlNode node in itemNodes)
                        {
                            var complexIdNode = node.SelectSingleNode("*[local-name()='complex_id']");
                            var serviceIdNode = node.SelectSingleNode("*[local-name()='service_id']");
                            var priceNode = node.SelectSingleNode("*[local-name()='price']");
                            var localizationIdNode = node.SelectSingleNode("*[local-name()='localization_id']");
                            var biomaterialIdNode = node.SelectSingleNode("*[local-name()='biomaterial_id']");
                            var transportIdNode = node.SelectSingleNode("*[local-name()='transport_id']");
                            var mainServiceNode = node.SelectSingleNode("*[local-name()='main_service']");

                            float priceValue = 0;
                            if (priceNode != null)
                            {
                                var nilAttribute = priceNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                                if (nilAttribute != null && nilAttribute.Value == "true")
                                {
                                    priceValue = 0;
                                }
                                else if (!string.IsNullOrEmpty(priceNode.InnerText))
                                {
                                    float.TryParse(priceNode.InnerText, out priceValue);
                                }
                            }

                            var composition = new DictionaryMarketingComplex
                            {
                                complex_id = complexIdNode?.InnerText ?? string.Empty,
                                service_id = serviceIdNode?.InnerText ?? string.Empty,
                                price = priceValue,
                                localization_id = localizationIdNode?.InnerText ?? string.Empty,
                                biomaterial_id = biomaterialIdNode?.InnerText ?? string.Empty,
                                transport_id = transportIdNode?.InnerText ?? string.Empty,
                                main_service = mainServiceNode?.InnerText ?? string.Empty
                            };

                            if (!string.IsNullOrEmpty(composition.complex_id) && composition.complex_id != "*" &&
                                !string.IsNullOrEmpty(composition.service_id) && composition.service_id != "*")
                            {
                                compositions.Add(composition);
                            }
                        }

                        Console.WriteLine($"Успешно обработано {compositions.Count} элементов состава маркетинговых комплексов.");
                        return compositions;
                    }
                    else
                    {
                        Console.WriteLine("Элементы <item> не найдены.");
                        return compositions;
                    }
                }
                catch (XmlException ex)
                {
                    Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                    return new List<DictionaryMarketingComplex>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Общая ошибка: {ex.Message}");
                    return new List<DictionaryMarketingComplex>();
                }
            }

            private static int ParseArchive(XmlNode archiveNode)
            {
                int archiveValue = 0;
                if (archiveNode != null)
                {
                    var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                    if (nilAttribute != null && nilAttribute.Value == "true")
                    {
                        archiveValue = 0;
                    }
                    else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                    {
                        int.TryParse(archiveNode.InnerText, out archiveValue);
                    }
                }
                return archiveValue;
            }

            private static float ParseFloat(XmlNode floatNode)
            {
                float floatValue = 0;
                if (floatNode != null)
                {
                    var nilAttribute = floatNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                    if (nilAttribute != null && nilAttribute.Value == "true")
                    {
                        floatValue = 0;
                    }
                    else if (!string.IsNullOrEmpty(floatNode.InnerText))
                    {
                        float.TryParse(floatNode.InnerText, out floatValue);
                    }
                }
                return floatValue;
            }
        }

        // Класс для справочника групп услуг-аналогов (get_services_group_analogs)
        public class DictionaryServicesGroupAnalogs
    {
        public string group_id { get; set; } = string.Empty;
        public string analog_group_id { get; set; } = string.Empty;
        public int archive { get; set; }

        public static void PrintToConsole(List<DictionaryServicesGroupAnalogs> output, int count)
        {
            Console.WriteLine($"Dictionary Services_group_analogs");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var analog = output[i];
                Console.WriteLine($"group_id: {analog.group_id}, analog_group_id: {analog.analog_group_id}, archive: {analog.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryServicesGroupAnalogs> Parse(string xmlContent)
        {
            var analogs = new List<DictionaryServicesGroupAnalogs>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var analogNodes = doc.SelectNodes("//*[local-name()='groups']/*[local-name()='item'] | //*[local-name()='item']");

                if (analogNodes != null && analogNodes.Count > 0)
                {
                    foreach (XmlNode node in analogNodes)
                    {
                        var groupIdNode = node.SelectSingleNode("*[local-name()='group_id']");
                        var analogGroupIdNode = node.SelectSingleNode("*[local-name()='analog_group_id']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = ParseArchive(archiveNode);

                        var analog = new DictionaryServicesGroupAnalogs
                        {
                            group_id = groupIdNode?.InnerText ?? string.Empty,
                            analog_group_id = analogGroupIdNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(analog.group_id) && analog.group_id != "*")
                        {
                            analogs.Add(analog);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {analogs.Count} групп аналогов.");
                    return analogs;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return analogs;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryServicesGroupAnalogs>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryServicesGroupAnalogs>();
            }
        }

        private static int ParseArchive(XmlNode archiveNode) => ParseInt(archiveNode);
        private static int ParseInt(XmlNode node)
        {
            if (node == null) return 0;
            var nilAttribute = node.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            if (nilAttribute != null && nilAttribute.Value == "true") return 0;
            return int.TryParse(node.InnerText, out int value) ? value : 0;
        }
    }

    // Класс для справочника автодобавляемых услуг (get_service_auto_insert)
    public class DictionaryServiceAutoInsert
    {
        public string service_id { get; set; } = string.Empty;
        public string auto_service_id { get; set; } = string.Empty;
        public int archive { get; set; }

        public static void PrintToConsole(List<DictionaryServiceAutoInsert> output, int count)
        {
            Console.WriteLine($"Dictionary Service_auto_insert");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var autoInsert = output[i];
                Console.WriteLine($"service_id: {autoInsert.service_id}, auto_service_id: {autoInsert.auto_service_id}, archive: {autoInsert.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryServiceAutoInsert> Parse(string xmlContent)
        {
            var autoInserts = new List<DictionaryServiceAutoInsert>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var autoInsertNodes = doc.SelectNodes("//*[local-name()='auto_inserts']/*[local-name()='item'] | //*[local-name()='item']");

                if (autoInsertNodes != null && autoInsertNodes.Count > 0)
                {
                    foreach (XmlNode node in autoInsertNodes)
                    {
                        var serviceIdNode = node.SelectSingleNode("*[local-name()='service_id']");
                        var autoServiceIdNode = node.SelectSingleNode("*[local-name()='auto_service_id']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = ParseArchive(archiveNode);

                        var autoInsert = new DictionaryServiceAutoInsert
                        {
                            service_id = serviceIdNode?.InnerText ?? string.Empty,
                            auto_service_id = autoServiceIdNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(autoInsert.service_id) && autoInsert.service_id != "*")
                        {
                            autoInserts.Add(autoInsert);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {autoInserts.Count} автодобавляемых услуг.");
                    return autoInserts;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return autoInserts;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryServiceAutoInsert>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryServiceAutoInsert>();
            }
        }

        private static int ParseArchive(XmlNode archiveNode) => ParseInt(archiveNode);
        private static int ParseInt(XmlNode node)
        {
            if (node == null) return 0;
            var nilAttribute = node.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            if (nilAttribute != null && nilAttribute.Value == "true") return 0;
            return int.TryParse(node.InnerText, out int value) ? value : 0;
        }
    }

    // Класс для справочника дополнительных тестов для услуг (get_services_supplementals)
    public class DictionaryServicesSupplementals
    {
        
            public string complex_id { get; set; } = string.Empty;
            public string service_id { get; set; } = string.Empty;
            public float price { get; set; }
            public string localization_id { get; set; } = string.Empty;
            public string biomaterial_id { get; set; } = string.Empty;
            public string transport_id { get; set; } = string.Empty;
            public string main_service { get; set; } = string.Empty;

            public static void PrintToConsole(List<DictionaryServicesSupplementals> output, int count)
            {
                Console.WriteLine($"Dictionary Marketing_complex_composition");
                for (int i = 0; i < Math.Min(count, output.Count); i++)
                {
                    var item = output[i];
                    Console.WriteLine($"complex_id: {item.complex_id}, service_id: {item.service_id}, price: {item.price}, localization_id: {item.localization_id}, biomaterial_id: {item.biomaterial_id}, transport_id: {item.transport_id}, main_service: {item.main_service}");
                }
                Console.WriteLine("\n");
            }

            public static List<DictionaryServicesSupplementals> Parse(string xmlContent)
            {
                var compositions = new List<DictionaryServicesSupplementals>();
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xmlContent);

                    var itemNodes = doc.SelectNodes("//*[local-name()='item']");

                    if (itemNodes != null && itemNodes.Count > 0)
                    {
                        foreach (XmlNode node in itemNodes)
                        {
                            var complexIdNode = node.SelectSingleNode("*[local-name()='complex_id']");
                            var serviceIdNode = node.SelectSingleNode("*[local-name()='service_id']");
                            var priceNode = node.SelectSingleNode("*[local-name()='price']");
                            var localizationIdNode = node.SelectSingleNode("*[local-name()='localization_id']");
                            var biomaterialIdNode = node.SelectSingleNode("*[local-name()='biomaterial_id']");
                            var transportIdNode = node.SelectSingleNode("*[local-name()='transport_id']");
                            var mainServiceNode = node.SelectSingleNode("*[local-name()='main_service']");

                            float priceValue = 0;
                            if (priceNode != null)
                            {
                                var nilAttribute = priceNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                                if (nilAttribute != null && nilAttribute.Value == "true")
                                {
                                    priceValue = 0;
                                }
                                else if (!string.IsNullOrEmpty(priceNode.InnerText))
                                {
                                    float.TryParse(priceNode.InnerText, out priceValue);
                                }
                            }

                            var composition = new DictionaryServicesSupplementals
                            {
                                complex_id = complexIdNode?.InnerText ?? string.Empty,
                                service_id = serviceIdNode?.InnerText ?? string.Empty,
                                price = priceValue,
                                localization_id = localizationIdNode?.InnerText ?? string.Empty,
                                biomaterial_id = biomaterialIdNode?.InnerText ?? string.Empty,
                                transport_id = transportIdNode?.InnerText ?? string.Empty,
                                main_service = mainServiceNode?.InnerText ?? string.Empty
                            };

                            if (!string.IsNullOrEmpty(composition.complex_id) && composition.complex_id != "*" &&
                                !string.IsNullOrEmpty(composition.service_id) && composition.service_id != "*")
                            {
                                compositions.Add(composition);
                            }
                        }

                        Console.WriteLine($"Успешно обработано {compositions.Count} элементов состава маркетинговых комплексов.");
                        return compositions;
                    }
                    else
                    {
                        Console.WriteLine("Элементы <item> не найдены.");
                        return compositions;
                    }
                }
                catch (XmlException ex)
                {
                    Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                    return new List<DictionaryServicesSupplementals>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Общая ошибка: {ex.Message}");
                    return new List<DictionaryServicesSupplementals>();
                }
            }

            private static int ParseArchive(XmlNode archiveNode)
            {
                int archiveValue = 0;
                if (archiveNode != null)
                {
                    var nilAttribute = archiveNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                    if (nilAttribute != null && nilAttribute.Value == "true")
                    {
                        archiveValue = 0;
                    }
                    else if (!string.IsNullOrEmpty(archiveNode.InnerText))
                    {
                        int.TryParse(archiveNode.InnerText, out archiveValue);
                    }
                }
                return archiveValue;
            }

            private static float ParseFloat(XmlNode floatNode)
            {
                float floatValue = 0;
                if (floatNode != null)
                {
                    var nilAttribute = floatNode.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
                    if (nilAttribute != null && nilAttribute.Value == "true")
                    {
                        floatValue = 0;
                    }
                    else if (!string.IsNullOrEmpty(floatNode.InnerText))
                    {
                        float.TryParse(floatNode.InnerText, out floatValue);
                    }
                }
                return floatValue;
            }
        }
    

    // Класс для справочника лабораторных отделений (get_branches)
    public class DictionaryBranches
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public List<string> phones { get; set; } = new List<string>();
        public string work_time { get; set; } = string.Empty;
        public List<Day> days { get; set; } = new List<Day>();
        public int archive { get; set; }
    }

    public class Day
    {
        public int day { get; set; }
        public List<TimePeriod> time_periods { get; set; } = new List<TimePeriod>();
    }

    public class TimePeriod
    {
        public string start_time { get; set; } = string.Empty;
        public string end_time { get; set; } = string.Empty;
    }

    public static class DictionaryBranchesParser
    {
        public static void PrintToConsole(List<DictionaryBranches> output, int count)
        {
            Console.WriteLine($"Dictionary Branches");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var branch = output[i];
                Console.WriteLine($"id: {branch.id}, name: {branch.name}, address: {branch.address}, archive: {branch.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryBranches> Parse(string xmlContent)
        {
            var branches = new List<DictionaryBranches>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var branchNodes = doc.SelectNodes("//*[local-name()='branches']/*[local-name()='item'] | //*[local-name()='item']");

                if (branchNodes != null && branchNodes.Count > 0)
                {
                    foreach (XmlNode node in branchNodes)
                    {
                        var idNode = node.SelectSingleNode("*[local-name()='id']");
                        var nameNode = node.SelectSingleNode("*[local-name()='name']");
                        var addressNode = node.SelectSingleNode("*[local-name()='address']");
                        var phonesNode = node.SelectSingleNode("*[local-name()='phones']");
                        var workTimeNode = node.SelectSingleNode("*[local-name()='work_time']");
                        var daysNode = node.SelectSingleNode("*[local-name()='days']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = ParseArchive(archiveNode);

                        var branch = new DictionaryBranches
                        {
                            id = idNode?.InnerText ?? string.Empty,
                            name = nameNode?.InnerText ?? string.Empty,
                            address = addressNode?.InnerText ?? string.Empty,
                            work_time = workTimeNode?.InnerText ?? string.Empty,
                            archive = archiveValue
                        };

                        // Парсинг phones
                        if (phonesNode != null)
                        {
                            var phoneNodes = phonesNode.SelectNodes("*[local-name()='item']");
                            if (phoneNodes != null)
                            {
                                foreach (XmlNode pNode in phoneNodes)
                                {
                                    branch.phones.Add(pNode.InnerText ?? string.Empty);
                                }
                            }
                        }

                        // Парсинг days
                        if (daysNode != null)
                        {
                            var dayNodes = daysNode.SelectNodes("*[local-name()='item']");
                            if (dayNodes != null)
                            {
                                foreach (XmlNode dNode in dayNodes)
                                {
                                    var day = new Day { day = ParseInt(dNode.SelectSingleNode("*[local-name()='day']")) };
                                    var timePeriodsNode = dNode.SelectSingleNode("*[local-name()='time_periods']");
                                    if (timePeriodsNode != null)
                                    {
                                        var tpNodes = timePeriodsNode.SelectNodes("*[local-name()='item']");
                                        if (tpNodes != null)
                                        {
                                            foreach (XmlNode tpNode in tpNodes)
                                            {
                                                var startTimeNode = tpNode.SelectSingleNode("*[local-name()='start_time']");
                                                var endTimeNode = tpNode.SelectSingleNode("*[local-name()='end_time']");
                                                day.time_periods.Add(new TimePeriod
                                                {
                                                    start_time = startTimeNode?.InnerText ?? string.Empty,
                                                    end_time = endTimeNode?.InnerText ?? string.Empty
                                                });
                                            }
                                        }
                                    }
                                    branch.days.Add(day);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(branch.id) && branch.id != "*")
                        {
                            branches.Add(branch);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {branches.Count} отделений.");
                    return branches;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return branches;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryBranches>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryBranches>();
            }
        }

        private static int ParseArchive(XmlNode archiveNode) => ParseInt(archiveNode);
        private static int ParseInt(XmlNode node)
        {
            if (node == null) return 0;
            var nilAttribute = node.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            if (nilAttribute != null && nilAttribute.Value == "true") return 0;
            return int.TryParse(node.InnerText, out int value) ? value : 0;
        }
    }

    // Класс для справочника цен (get_prices)
    public class DictionaryPrices
    {
        public string service_id { get; set; } = string.Empty;
        public float price { get; set; }
        public int archive { get; set; }

        public static void PrintToConsole(List<DictionaryPrices> output, int count)
        {
            Console.WriteLine($"Dictionary Prices");
            for (int i = 0; i < Math.Min(count, output.Count); i++)
            {
                var priceItem = output[i];
                Console.WriteLine($"service_id: {priceItem.service_id}, price: {priceItem.price}, archive: {priceItem.archive}");
            }
            Console.WriteLine("\n");
        }

        public static List<DictionaryPrices> Parse(string xmlContent)
        {
            var prices = new List<DictionaryPrices>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                var priceNodes = doc.SelectNodes("//*[local-name()='prices']/*[local-name()='item'] | //*[local-name()='item']");

                if (priceNodes != null && priceNodes.Count > 0)
                {
                    foreach (XmlNode node in priceNodes)
                    {
                        var serviceIdNode = node.SelectSingleNode("*[local-name()='service_id']");
                        var priceNode = node.SelectSingleNode("*[local-name()='price']");
                        var archiveNode = node.SelectSingleNode("*[local-name()='archive']");

                        int archiveValue = ParseArchive(archiveNode);
                        float priceValue = ParseFloat(priceNode);

                        var priceItem = new DictionaryPrices
                        {
                            service_id = serviceIdNode?.InnerText ?? string.Empty,
                            price = priceValue,
                            archive = archiveValue
                        };

                        if (!string.IsNullOrEmpty(priceItem.service_id) && priceItem.service_id != "*")
                        {
                            prices.Add(priceItem);
                        }
                    }

                    Console.WriteLine($"Успешно обработано {prices.Count} цен.");
                    return prices;
                }
                else
                {
                    Console.WriteLine("Элементы <item> не найдены.");
                    return prices;
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
                return new List<DictionaryPrices>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка: {ex.Message}");
                return new List<DictionaryPrices>();
            }
        }

        private static int ParseArchive(XmlNode archiveNode) => ParseInt(archiveNode);
        private static int ParseInt(XmlNode node)
        {
            if (node == null) return 0;
            var nilAttribute = node.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            if (nilAttribute != null && nilAttribute.Value == "true") return 0;
            return int.TryParse(node.InnerText, out int value) ? value : 0;
        }

        private static float ParseFloat(XmlNode node)
        {
            if (node == null) return 0f;
            var nilAttribute = node.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            if (nilAttribute != null && nilAttribute.Value == "true") return 0f;
            return float.TryParse(node.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }
    }
}