using iTextSharp.text;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Laboratory.Gemotest.GemotestRequests;
using System.Collections.Generic;
using iTextSharp.text.pdf.security;
using System.Linq;
using SiMed.Laboratory;
using System.Diagnostics.Eventing.Reader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Drawing;

namespace Laboratory.Gemotest.Options
{
    public class GemotestService
    {
        private string _login;
        private string _password;
        private string _contractor;
        private string _contractor_code;
        private string _salt;
        private string _url = "https://api.gemotest.ru/odoctor/odoctor/index/ws/1";
        private int chunk;
        private int size;
        public string filePath = $@"C:\Users\Night\AppData\Симплекс\СиМед - Клиника\GemotestDictionaries\10003\";
        
        private static readonly string[] DictionaryFiles = {
            "Biomaterials", "Transport", "Localization", "Service_group", "Service_parameters",
            "Directory", "Tests", "Samples_services", "Samples", "Processing_rules",
            /*"Services_all_interlocks",*/ "Marketing_complex_composition", "Services_group_analogs",
            "Service_auto_insert", "Services_supplementals"
        };
        private string ListFilePath => Path.Combine(filePath, "dictionaries_list.txt");

        public GemotestService(string url, string login, string password, string contractor, string contractor_code, string salt)
        {
            if (url == null) throw new ArgumentNullException("url");
            _url = url;
            _login = login;
            _password = password;
            _contractor = contractor;
            _contractor_code = contractor_code;
            _salt = salt;
            filePath = $@"C:\Users\Night\AppData\Симплекс\СиМед - Клиника\GemotestDictionaries\{contractor_code}\";
        }

        private void CreateNewDictionaries(string response, string fileName)
        {
            string dictionaries_path = Path.Combine(filePath, $"{fileName}_new.xml");
            string directory = Path.GetDirectoryName(dictionaries_path);

            try
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(dictionaries_path, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public bool all_dictionaries_is_valid()
        {
            string lastUpdatePath = Path.Combine(filePath, "last_update.txt");

            if (!File.Exists(lastUpdatePath))
            {
                return get_all_dictionary();
            }

            try
            {
                string lastUpdateStr = File.ReadAllText(lastUpdatePath).Trim();
                if (DateTime.TryParseExact(lastUpdateStr, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime lastUpdate))
                {
                    TimeSpan age = DateTime.Now - lastUpdate;
                    if (age > TimeSpan.FromHours(24))
                    {
                        return get_all_dictionary();
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return get_all_dictionary();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки last_update.txt: {ex.Message} — обновляем справочники.");
                return get_all_dictionary();
            }
        }

        public bool get_all_dictionary()
        {
            try
            {
                get_biomaterials();
                get_transport();
                get_localization();
                get_service_group();
                get_service_parameters();
                get_directory();
                get_tests();
                get_samples_services();
                get_samples();
                get_processing_rules();
                // get_services_all_interlocks();
                get_marketing_complex_composition();
                get_services_group_analogs();
                get_service_auto_insert();
                get_services_supplementals();

                return VerifyAndReplaceDictionaries();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке справочников: {ex.Message}");
                CleanupNewFiles();
                return false;
            }
        }

        public void PrintS()
        {
            DictionarySamplesServices.PrintToConsole(Dictionaries.SamplesServices, 100);
        }
        public void PrintDictionaries()
        {
            if (get_all_dictionary())
            {
                Dictionaries.Unpack(filePath);

                // Biomaterials
                DictionaryBiomaterials.PrintToConsole(Dictionaries.Biomaterials, 10);

                // Transport
                DictionaryTransport.PrintToConsole(Dictionaries.Transport, 10);

                // Localization
                DictionaryLocalization.PrintToConsole(Dictionaries.Localization, 10);

                // Service_group
                DictionaryService_group.PrintToConsole(Dictionaries.ServiceGroup, 10);

                // Service_parameters
                DictionaryService_parameters.PrintToConsole(Dictionaries.ServiceParameters, 10);

                // Directory (услуги)
                DictionaryService.PrintToConsole(Dictionaries.Directory, 10);

                // Tests
                DictionaryTests.PrintToConsole(Dictionaries.Tests, 10);

                // Samples_services
                DictionarySamplesServices.PrintToConsole(Dictionaries.SamplesServices, 10);

                // Samples
                DictionarySamples.PrintToConsole(Dictionaries.Samples, 10);

                // Processing_rules
                DictionaryProcessingRules.PrintToConsole(Dictionaries.ProcessingRules, 10);

                // Services_all_interlocks (если включен)
                DictionaryServicesAllInterlocks.PrintToConsole(Dictionaries.ServicesAllInterlocks, 10);

                // Marketing_complex_composition
                DictionaryMarketingComplex.PrintToConsole(Dictionaries.MarketingComplexComposition, 10);

                // Services_group_analogs
                DictionaryServicesGroupAnalogs.PrintToConsole(Dictionaries.ServicesGroupAnalogs, 10);

                // Service_auto_insert
                DictionaryServiceAutoInsert.PrintToConsole(Dictionaries.ServiceAutoInsert, 10);

                // Services_supplementals
                DictionaryServicesSupplementals.PrintToConsole(Dictionaries.ServicesSupplementals, 10);

                Console.WriteLine("Вывод справочников завершён.");
            }
            else
            {
                Console.WriteLine("Ошибка загрузки справочников. Вывод невозможен.");
            }
        }

        public void get_biomaterials()
        {
            string response = RequestToGemotest("get_biomaterials");
            CreateNewDictionaries(response, "Biomaterials");
            Console.WriteLine("Справочник биоматериалов загружен (Ok 1)");
        }

        public void get_transport()
        {
            string response = RequestToGemotest("get_transport");
            CreateNewDictionaries(response, "Transport");
            Console.WriteLine("Справочник транспортных сред загружен (Ok 2)");
        }

        public void get_localization()
        {
            string response = RequestToGemotest("get_localization");
            CreateNewDictionaries(response, "Localization");
            Console.WriteLine("Справочник локализаций загружен (Ok 3)");
        }

        public void get_service_group()
        {
            string response = RequestToGemotest("get_service_group");
            CreateNewDictionaries(response, "Service_group");
            Console.WriteLine("Справочник групп услуг загружен (Ok 4)");
        }

        public void get_service_parameters()
        {
            string response = RequestToGemotest("get_service_parameters");
            CreateNewDictionaries(response, "Service_parameters");
            Console.WriteLine("Справочник параметров услуг загружен (Ok 5)");
        }

        public void get_directory()
        {
            string response = RequestToGemotest("get_directory");
            CreateNewDictionaries(response, "Directory");
            Console.WriteLine("Справочник услуг загружен (Ok 6)");
        }

        public void get_tests()
        {
            string response = RequestToGemotest("get_tests");
            CreateNewDictionaries(response, "Tests");
            Console.WriteLine("Справочник тестов загружен (Ok 7)");
        }

        public void get_samples_services()
        {
            string response = RequestToGemotest("get_samples_services");
            CreateNewDictionaries(response, "Samples_services");
            Console.WriteLine("Справочник проб-услуг загружен (Ok 8)");
        }

        public void get_samples()
        {
            string response = RequestToGemotest("get_samples");
            CreateNewDictionaries(response, "Samples");
            Console.WriteLine("Справочник проб загружен (Ok 9)");
        }

        public void get_processing_rules()
        {
            string response = RequestToGemotest("get_processing_rules");
            CreateNewDictionaries(response, "Processing_rules");
            Console.WriteLine("Справочник правил обработки загружен (Ok 10)");
        }

        /*public void get_services_all_interlocks()
        {
            string response = RequestToGemotest("get_services_all_interlocks");
            CreateNewDictionaries(response, "Services_all_interlocks");
            Console.WriteLine("Справочник правил взаимоблокирующихся услуг загружен (Ok 11)");
        }*/
        public void get_services_all_interlocks()
        {
            const int chunkSize = 20001;
            int currentChunk = 1;
            List<string> allItemXmls = new List<string>();
            string firstResponse = null;
            int totalChunks = 0;

            try
            {
                do
                {
                    // Устанавливаем поля перед запросом
                    this.chunk = currentChunk;
                    this.size = chunkSize;

                    string response = RequestToGemotest("get_services_all_interlocks");

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(response);

                    // Проверка статуса
                    XmlNode statusNode = doc.SelectSingleNode("//status");
                    if (statusNode == null || statusNode.InnerText != "accepted")
                    {
                        throw new Exception($"Неверный статус ответа: {statusNode?.InnerText ?? "null"}");
                    }

                    // Проверка ошибки
                    XmlNode errorCodeNode = doc.SelectSingleNode("//error_code");
                    if (errorCodeNode != null && int.Parse(errorCodeNode.InnerText) != 0)
                    {
                        XmlNode errorDescNode = doc.SelectSingleNode("//error_description");
                        throw new Exception($"Ошибка в ответе (код {errorCodeNode.InnerText}): {errorDescNode?.InnerText ?? "нет описания"}");
                    }

                    // Получение информации о чанках
                    XmlNode chunkNode = doc.SelectSingleNode("//chunk");
                    if (chunkNode == null)
                    {
                        throw new Exception("Отсутствует информация о чанке в ответе");
                    }
                    XmlNode currentNode = chunkNode.SelectSingleNode("current");
                    XmlNode countNode = chunkNode.SelectSingleNode("count");
                    if (currentNode == null || countNode == null)
                    {
                        throw new Exception("Отсутствуют current или count в чанке");
                    }

                    int thisCurrent = int.Parse(currentNode.InnerText);
                    totalChunks = int.Parse(countNode.InnerText);

                    if (thisCurrent != currentChunk)
                    {
                        throw new Exception($"Несоответствие текущего чанка: ожидался {currentChunk}, получен {thisCurrent}");
                    }

                    // Сбор элементов (item)
                    XmlNode elementsNode = doc.SelectSingleNode("//elements");
                    if (elementsNode != null)
                    {
                        XmlNodeList items = elementsNode.SelectNodes("item");
                        foreach (XmlNode item in items)
                        {
                            allItemXmls.Add(item.OuterXml);
                        }
                    }

                    if (currentChunk == 1)
                    {
                        firstResponse = response;
                    }
                    Console.WriteLine($"Текущий чанк: {currentChunk} из {totalChunks}");
                    currentChunk++;
                } while (currentChunk <= totalChunks);

                if (string.IsNullOrEmpty(firstResponse))
                {
                    throw new Exception("Не удалось получить первый ответ");
                }

                // Построение полного ответа
                XmlDocument fullDoc = new XmlDocument();
                fullDoc.LoadXml(firstResponse);

                XmlNode fullElementsNode = fullDoc.SelectSingleNode("//elements");
                if (fullElementsNode == null)
                {
                    throw new Exception("Отсутствует узел elements в первом ответе");
                }

                // Обновление arrayType
                XmlAttribute arrayTypeAttr = fullElementsNode.Attributes["SOAP-ENC:arrayType"];
                if (arrayTypeAttr != null)
                {
                    arrayTypeAttr.Value = $"ns2:Map[{allItemXmls.Count}]";
                }

                // Очистка старых item
                fullElementsNode.RemoveAll();

                // Добавление всех item
                foreach (string itemXml in allItemXmls)
                {
                    XmlDocument tempDoc = new XmlDocument();
                    tempDoc.LoadXml($"<root>{itemXml}</root>"); // Обертка для LoadXml
                    XmlNode itemNode = tempDoc.DocumentElement?.FirstChild;
                    if (itemNode != null)
                    {
                        XmlNode importedItem = fullDoc.ImportNode(itemNode, true);
                        fullElementsNode.AppendChild(importedItem);
                    }
                }

                // Обновление chunk: current=1, count=1
                XmlNode fullChunkNode = fullDoc.SelectSingleNode("//chunk");
                if (fullChunkNode != null)
                {
                    XmlNode fullCurrentNode = fullChunkNode.SelectSingleNode("current");
                    XmlNode fullCountNode = fullChunkNode.SelectSingleNode("count");
                    if (fullCurrentNode != null) fullCurrentNode.InnerText = "1";
                    if (fullCountNode != null) fullCountNode.InnerText = "1";
                }

                string fullResponse = fullDoc.OuterXml;
                CreateNewDictionaries(fullResponse, "Services_all_interlocks");
                Console.WriteLine("Справочник правил взаимоблокирующихся услуг загружен (Ok 11)");

                this.chunk = 0;
                this.size = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке справочника Services_all_interlocks: {ex.Message}");
                this.chunk = 0;
                this.size = 0;
                throw; 
            }
        }
        public void get_marketing_complex_composition()
        {
            string response = RequestToGemotest("get_marketing_complex_composition");
            CreateNewDictionaries(response, "Marketing_complex_composition");
            Console.WriteLine("Справочник маркетинговых комплексов загружен (Ok 12)");
        }

        public void get_services_group_analogs()
        {
            string response = RequestToGemotest("get_services_group_analogs");
            CreateNewDictionaries(response, "Services_group_analogs");
            Console.WriteLine("Справочник групп услуг-аналогов загружен (Ok 13)");
        }

        public void get_service_auto_insert()
        {
            string response = RequestToGemotest("get_service_auto_insert");
            CreateNewDictionaries(response, "Service_auto_insert");
            Console.WriteLine("Справочник автодобавляемых услуг загружен (Ok 14)");
        }

        public void get_services_supplementals()
        {
            string response = RequestToGemotest("get_services_supplementals");
            CreateNewDictionaries(response, "Services_supplementals");
            Console.WriteLine("Справочник дополнительных тестов для услуг загружен (Ok 15)");
        }

        private bool VerifyAndReplaceDictionaries()
        {
            var oldFileNames = new HashSet<string>();
            var missingNewForOld = new List<string>();

            try
            {
                foreach (var fileName in DictionaryFiles)
                {
                    string newPath = Path.Combine(filePath, $"{fileName}_new.xml");
                    if (!File.Exists(newPath) || new FileInfo(newPath).Length == 0)
                    {
                        throw new FileNotFoundException($"Новый файл {fileName}_new.xml не найден или пустой.");
                    }
                }

                if (!File.Exists(ListFilePath) || new FileInfo(ListFilePath).Length == 0)
                {
                    File.WriteAllLines(ListFilePath, DictionaryFiles);
                }

                oldFileNames = new HashSet<string>(File.ReadAllLines(ListFilePath).Where(line => !string.IsNullOrWhiteSpace(line)));

                if (oldFileNames.Count > 0)
                {
                    foreach (var fileName in oldFileNames)
                    {
                        string newPath = Path.Combine(filePath, $"{fileName}_new.xml");
                        if (!File.Exists(newPath) || new FileInfo(newPath).Length == 0)
                        {
                            missingNewForOld.Add(fileName);
                        }
                    }

                    if (missingNewForOld.Count > 0)
                    {
                        throw new InvalidOperationException($"Для старых файлов {string.Join(", ", missingNewForOld)} отсутствуют новые версии.");
                    }
                }

                foreach (var fileName in oldFileNames)
                {
                    string oldPath = Path.Combine(filePath, $"{fileName}.xml");
                    if (File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                }

                foreach (var fileName in DictionaryFiles)
                {
                    string newPath = Path.Combine(filePath, $"{fileName}_new.xml");
                    string finalPath = Path.Combine(filePath, $"{fileName}.xml");
                    File.Move(newPath, finalPath);
                }
                UpdateDictionariesListFile();

                File.WriteAllText(Path.Combine(filePath, "last_update.txt"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                Console.WriteLine("Все справочники успешно обновлены.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке/замене справочников: {ex.Message}");
                CleanupNewFiles();
                return false;
            }
        }

        private void UpdateDictionariesListFile()
        {
            var currentDictionaries = new List<string>();
            foreach (var file in Directory.GetFiles(filePath, "*.xml"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (Array.Exists(DictionaryFiles, element => element == fileName))
                {
                    currentDictionaries.Add(fileName);
                }
            }
            currentDictionaries.Sort();

            File.WriteAllLines(ListFilePath, currentDictionaries);
        }

        private void CleanupNewFiles()
        {
            foreach (var fileName in DictionaryFiles)
            {
                string newPath = Path.Combine(filePath, $"{fileName}_new.xml");
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }
            }
        }

        public string RequestToGemotest(string methodName)
        {
            string soapRequest;
            string hash = GenerateHash($"{_contractor_code}{_salt}");
            if (methodName == "get_directory")
            {
                soapRequest = CreateSoapRequest(methodName, _contractor_code, 1, hash);
            }
            else if (methodName == "get_services_supplementals" || methodName == "get_marketing_complex_composition")
            {
                soapRequest = CreateSoapRequest(methodName, _contractor_code, hash);
            }
            else if (methodName == "get_services_all_interlocks")
            {
                int c = chunk > 0 ? chunk : 1;
                int s = size > 0 ? size : 10001;
                soapRequest = CreateSoapRequest(methodName, c, s);
            }
            else
            {
                soapRequest = CreateSoapRequest(methodName);
            }

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers.Add("SOAPAction", $"\"urn:OdoctorControllerwsdl#{methodName}\"");

            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_login}:{_password}"));

            request.Headers["Authorization"] = "Basic " + auth;

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            byte[] data = Encoding.UTF8.GetBytes(soapRequest);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        string result = reader.ReadToEnd();
                        return result;
                    }
                }
            }
            catch (WebException ex)
            {
                var errorResponse = (HttpWebResponse)ex.Response;
                string errorBody = "Неизвестная ошибка";
                if (errorResponse != null)
                {
                    using (var stream = errorResponse.GetResponseStream())
                    using (var reader = new StreamReader(stream ?? Stream.Null))
                    {
                        errorBody = reader.ReadToEnd();
                    }
                }
                throw new Exception($"HTTP {errorResponse?.StatusCode}: {errorBody}.", ex);
            }
        }


        private string CreateSoapRequest(string methodName, int Chunk, int Size)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
xmlns:urn=""urn:OdoctorControllerwsdl""> 
   <soapenv:Header/> 
   <soapenv:Body> 
      <urn:{methodName} soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""> 
         <params xsi:type=""urn:request_{methodName}""> 
            <currentChunk xsi:type=""xsd:integer"">{Chunk}</currentChunk> 
            <chunkSize xsi:type=""xsd:integer"">{Size}</chunkSize> 
           
         </params> 
      </urn:{methodName}> 
   </soapenv:Body> 
</soapenv:Envelope>";
        }

        private string CreateSoapRequest(string methodName, string contractor, int directory, string hash)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
xmlns:urn=""urn:OdoctorControllerwsdl""> 
   <soapenv:Header/> 
   <soapenv:Body> 
      <urn:{methodName} soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""> 
         <params xsi:type=""urn:service""> 
            <contractor xsi:type=""xsd:string"">{contractor}</contractor> 
            <directory xsi:type=""xsd:integer"">{directory}</directory> 
            <hash xsi:type=""xsd:string"">{hash}</hash> 
         </params> 
      </urn:{methodName}> 
   </soapenv:Body> 
</soapenv:Envelope>";
        }

        private string CreateSoapRequest(string methodName, string contractor, string hash)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
xmlns:urn=""urn:OdoctorControllerwsdl""> 
<soapenv:Header/> 
<soapenv:Body> 
<urn:{methodName} soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""> 
<params xsi:type=""urn:request_{methodName}""> 
<contractor xsi:type=""xsd:string"">{contractor}</contractor> 
<hash xsi:type=""xsd:string"">{hash}</hash> 
</params> 
</urn:{methodName}> 
</soapenv:Body> 
</soapenv:Envelope>";
        }

        private string CreateSoapRequest(string methodName)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
xmlns:urn=""urn:OdoctorControllerwsdl""> 
<soapenv:Header/> 
    <soapenv:Body> 
        <urn:{methodName} soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""/> 
    </soapenv:Body> 
</soapenv:Envelope>";
        }

        private string GenerateHash(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}