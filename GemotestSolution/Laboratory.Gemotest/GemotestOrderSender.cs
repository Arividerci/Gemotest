using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Laboratory.Gemotest.SourseClass;
using SiMed.Laboratory;

namespace Laboratory.Gemotest.GemotestRequests
{

    internal sealed class ServiceSampleInfo
    {
        public string BiomaterialId { get; set; } = "";
        public string LocalizationId { get; set; } = "";
        public string TransportId { get; set; } = "";
    }

    internal class GemotestOrderSender
    {
        private readonly string _url;
        private readonly string _contractor;
        private readonly string _salt;
        private readonly string _login;
        private readonly string _password;

        public GemotestOrderSender(string url, string contractor, string salt, string login, string password)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _contractor = contractor ?? throw new ArgumentNullException(nameof(contractor));
            _salt = salt ?? throw new ArgumentNullException(nameof(salt));
            _login = login ?? throw new ArgumentNullException(nameof(login));
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        public bool CreateOrder(Order order, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                if (order == null)
                    throw new ArgumentNullException(nameof(order));

                var details = order.OrderDetail as GemotestOrderDetail;
                if (details == null)
                    throw new InvalidOperationException("OrderDetail должен быть GemotestOrderDetail.");

                if (details.Products == null || details.Products.Count == 0)
                    throw new InvalidOperationException("В заказе нет ни одной услуги.");

                var patient = order.Patient ?? new Patient();

                string extNum = string.IsNullOrWhiteSpace(order.Number)
                    ? "SiMed_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                    : order.Number;

                string orderNum = string.Empty; // для нового заказа

                DateTime birthDate = patient.Birthday == default(DateTime)
                    ? DateTime.Today
                    : patient.Birthday;

                string hash = BuildCreateOrderHash(
                    extNum,
                    orderNum,
                    _contractor,
                    patient.Surname ?? string.Empty,
                    birthDate,
                    _salt);

                var serviceItems = BuildServices(details);
                if (serviceItems.Count == 0)
                    throw new InvalidOperationException("Не удалось сформировать список услуг для create_order.");

                string xml = BuildCreateOrderEnvelope(
                    extNum,
                    orderNum,
                    _contractor,
                    hash,
                    order.AuthorInformation ?? string.Empty,
                    patient,
                    serviceItems);

                string responseXml = SendSoapRequest(xml);

                // парсим ответ
                var doc = new XmlDocument();
                doc.LoadXml(responseXml);

                var statusNodes = doc.GetElementsByTagName("status");
                string status = statusNodes.Count > 0 ? statusNodes[0].InnerText : string.Empty;

                if (!string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                {
                    var errorDescrNodes = doc.GetElementsByTagName("error_description");
                    string errorText = errorDescrNodes.Count > 0
                        ? errorDescrNodes[0].InnerText
                        : "Неизвестная ошибка create_order.";

                    throw new Exception("Ошибка create_order: " + errorText);
                }

                // при желании можно вытащить order_num из ответа и сохранить в details
                // var orderNumNodes = doc.GetElementsByTagName("order_num");
                // if (orderNumNodes.Count > 0)
                //     details.OrderNum = orderNumNodes[0].InnerText;

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // ---------- внутренности ----------

        private class SoapServiceItem
        {
            public string Id { get; set; }
            public string BiomaterialId { get; set; }
            public string LocalizationId { get; set; }
            public string TransportId { get; set; }
        }

        /// <summary>
        /// Собираем список услуг для блока &lt;services&gt;.
        /// - Обычные услуги (service_type 0/1) – по Directory.
        /// - Маркетинговый комплекс (service_type 2) – разворачиваем по MarketingComplexComposition.
        /// - Типы 3 и 4 игнорируем.
        /// </summary>
        private List<SoapServiceItem> BuildServices(GemotestOrderDetail details)
        {
            var result = new List<SoapServiceItem>();

            if (details.Products == null || details.Products.Count == 0)
                return result;

            for (int i = 0; i < details.Products.Count; i++)
            {
                var prod = details.Products[i];
                if (string.IsNullOrEmpty(prod.ProductId))
                    continue;

                var svc = Dictionaries.Directory?.FirstOrDefault(s => s.id == prod.ProductId);
                int serviceType = svc?.service_type ?? 0;

                // 3 и 4 — вообще не отправляем
                if (serviceType == 3 || serviceType == 4)
                    continue;

                // ----- маркетинговый комплекс (service_type == 2) -----
                if (serviceType == 2 && Dictionaries.MarketingComplexComposition != null)
                {
                    var complexItems = Dictionaries.MarketingComplexComposition
                        .Where(c => c.complex_id == svc.id)
                        .ToList();

                    foreach (var item in complexItems)
                    {
                        if (string.IsNullOrEmpty(item.service_id))
                            continue;

                        // biomaterial / localization / transport берём через sample_services + samples
                        var sampleInfo = ResolveSampleInfoForService(
                            item.service_id,
                            item.biomaterial_id
                        );

                        result.Add(new SoapServiceItem
                        {
                            Id = item.service_id,
                            BiomaterialId = sampleInfo.BiomaterialId,
                            LocalizationId = sampleInfo.LocalizationId,
                            TransportId = sampleInfo.TransportId
                        });
                    }

                    continue;
                }

                // ----- обычная услуга (service_type 0 или 1) -----
                var sampleInfoForSimple = ResolveSampleInfoForService(
                    prod.ProductId,
                    null   // биоматериал из комплекса тут не нужен
                );

                result.Add(new SoapServiceItem
                {
                    Id = prod.ProductId,
                    BiomaterialId = sampleInfoForSimple.BiomaterialId,
                    LocalizationId = sampleInfoForSimple.LocalizationId,
                    TransportId = sampleInfoForSimple.TransportId
                });
            }

            return result;
        }
        
           
    private ServiceSampleInfo ResolveSampleInfoForService(string serviceId, string biomaterialFromComplex)
            {
                var res = new ServiceSampleInfo();

                if (string.IsNullOrEmpty(serviceId))
                    return res;

                // 1) ServiceParameters (точное совпадение service_id + biomaterial_id, если передали biomaterialFromComplex)
                var param = Dictionaries.ServiceParameters?
                    .FirstOrDefault(p =>
                        p.service_id == serviceId &&
                        (string.IsNullOrEmpty(biomaterialFromComplex) || p.biomaterial_id == biomaterialFromComplex));

                if (param != null)
                {
                    if (!string.IsNullOrEmpty(param.biomaterial_id))
                        res.BiomaterialId = param.biomaterial_id;

                    if (!string.IsNullOrEmpty(param.transport_id))
                        res.TransportId = param.transport_id;
                }

                // 2) Фоллбек: Directory
                var svc = Dictionaries.Directory?.FirstOrDefault(s => s.id == serviceId);
                if (svc != null)
                {
                    if (string.IsNullOrEmpty(res.BiomaterialId) && !string.IsNullOrEmpty(svc.biomaterial_id))
                        res.BiomaterialId = svc.biomaterial_id;

                    if (string.IsNullOrEmpty(res.LocalizationId) && !string.IsNullOrEmpty(svc.localization_id))
                        res.LocalizationId = svc.localization_id;

                    if (string.IsNullOrEmpty(res.TransportId) && !string.IsNullOrEmpty(svc.transport_id))
                        res.TransportId = svc.transport_id;
                }

                return res;
            }


        /// <summary>
        /// hash = sha1(ext_num + order_num + contractor + surname + birthday(yyyy-MM-dd) + salt)
        /// полностью совпадает с примером SiMed1 / Тест / 1998-04-02.
        /// </summary>
        private string BuildCreateOrderHash(
            string extNum,
            string orderNum,
            string contractor,
            string surname,
            DateTime birthday,
            string salt)
        {
            string birthStr = birthday.ToString("yyyy-MM-dd");

            string plain =
                (extNum ?? string.Empty) +
                (orderNum ?? string.Empty) +
                (contractor ?? string.Empty) +
                (surname ?? string.Empty) +
                birthStr +
                (salt ?? string.Empty);

            using (var sha1 = SHA1.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                byte[] hash = sha1.ComputeHash(data);

                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
        private string BuildCreateOrderEnvelope(
    string extNum,
    string orderNum,
    string contractor,
    string hash,
    string comment,
    Patient patient,
    IList<SoapServiceItem> services)
        {
            int count = services?.Count ?? 0;

            string surname = patient?.Surname ?? string.Empty;
            string firstname = patient?.Name ?? string.Empty;
            string middlename = patient?.Patronimic ?? string.Empty;

            DateTime birthDate = (patient != null && patient.Birthday != default(DateTime))
                ? patient.Birthday
                : DateTime.Today;

            // Гемотест: 0 – муж, 1 – жен. Если твой enum другой – явно мапни.
            int gender = patient != null && patient.Sex == Sex.Female ? 1 : 0;

            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope ");
            sb.Append("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ");
            sb.Append("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ");
            sb.Append("xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
            sb.Append("xmlns:urn=\"urn:OdoctorControllerwsdl\" ");
            sb.Append("xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">");

            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<urn:create_order soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            sb.Append("<params xsi:type=\"urn:order\">");

            sb.Append("<ext_num xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(extNum ?? string.Empty))
              .Append("</ext_num>");

            sb.Append("<order_num xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(orderNum ?? string.Empty))
              .Append("</order_num>");

            sb.Append("<comment xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(comment ?? string.Empty))
              .Append("</comment>");

            sb.Append("<contractor xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(contractor ?? string.Empty))
              .Append("</contractor>");

            sb.Append("<hash xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(hash ?? string.Empty))
              .Append("</hash>");

            sb.Append("<registered xsi:type=\"xsd:boolean\">true</registered>");
            sb.Append("<order_status xsi:type=\"xsd:integer\">1</order_status>");

            // -------- patient ----------
            sb.Append("<patient xsi:type=\"urn:patient\">");

            sb.Append("<surname xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(surname))
              .Append("</surname>");

            sb.Append("<firstname xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(firstname))
              .Append("</firstname>");

            sb.Append("<middlename xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(middlename))
              .Append("</middlename>");

            sb.Append("<birthdate xsi:type=\"xsd:date\">")
              .Append(birthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              .Append("</birthdate>");

            sb.Append("<gender xsi:type=\"xsd:int\">")
              .Append(gender)
              .Append("</gender>");

            sb.Append("</patient>");

            // -------- services ----------
            sb.Append("<services xsi:type=\"urn:servicesArray\" soapenc:arrayType=\"urn:services[")
              .Append(count)
              .Append("]\">");

            if (services != null)
            {
                foreach (var s in services)
                {
                    if (string.IsNullOrEmpty(s.Id))
                        continue;

                    sb.Append("<item>");

                    sb.Append("<id>")
                      .Append(SecurityElement.Escape(s.Id))
                      .Append("</id>");

                    sb.Append("<biomaterial_id>")
                      .Append(SecurityElement.Escape(s.BiomaterialId ?? string.Empty))
                      .Append("</biomaterial_id>");

                    sb.Append("<localization_id>")
                      .Append(SecurityElement.Escape(s.LocalizationId ?? string.Empty))
                      .Append("</localization_id>");

                    sb.Append("<transport_id>")
                      .Append(SecurityElement.Escape(s.TransportId ?? string.Empty))
                      .Append("</transport_id>");

                    sb.Append("</item>");
                }
            }

            sb.Append("</services>");

            sb.Append("</params>");
            sb.Append("</urn:create_order>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }
        private string SendSoapRequest(string xmlBody)
        {
            // --- выводим запрос в консоль для анализа ---
            Console.WriteLine("========== Gemotest create_order REQUEST ==========");
            Console.WriteLine(xmlBody);
            Console.WriteLine("==================================================");

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";

            // SOAPAction – как для create_order
            request.Headers["SOAPAction"] = "\"urn:OdoctorControllerwsdl#create_order\"";

            // HTTP Basic Auth: login:password
            string credentials = $"{_login}:{_password}";
            string authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
            request.Headers["Authorization"] = "Basic " + authHeader;
            request.PreAuthenticate = true;

            byte[] buffer = Encoding.UTF8.GetBytes(xmlBody);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(buffer, 0, buffer.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var respStream = response.GetResponseStream())
                using (var reader = new StreamReader(respStream, Encoding.UTF8))
                {
                    string responseText = reader.ReadToEnd();

                    // заодно можно выводить и ответ
                    Console.WriteLine("========== Gemotest create_order RESPONSE ==========");
                    Console.WriteLine(responseText);
                    Console.WriteLine("===================================================");

                    return responseText;
                }
            }
            catch (WebException ex)
            {
                string responseText = "";
                if (ex.Response != null)
                {
                    using (var respStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(respStream, Encoding.UTF8))
                    {
                        responseText = reader.ReadToEnd();
                    }
                }

                Console.WriteLine("========== Gemotest create_order ERROR RESPONSE ==========");
                Console.WriteLine(responseText);
                Console.WriteLine("=========================================================");

                throw new Exception(
                    $"HTTP error: {ex.Message}\r\nResponse: {responseText}",
                    ex);
            }
        }


    }
}
