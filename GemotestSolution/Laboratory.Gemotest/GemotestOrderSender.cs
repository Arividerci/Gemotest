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
using Laboratory.Gemotest;

namespace Laboratory.Gemotest.GemotestRequests
{
    internal sealed class GemotestOrderSender
    {
        private readonly string _url;
        private readonly string _contractor;
        private readonly string _salt;
        private readonly string _login;
        private readonly string _password;

        private Dictionaries _dictionaries;

        public GemotestOrderSender(string url, string contractor, string salt, string login, string password)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _contractor = contractor ?? throw new ArgumentNullException(nameof(contractor));
            _salt = salt ?? throw new ArgumentNullException(nameof(salt));
            _login = login ?? throw new ArgumentNullException(nameof(login));
            _password = password ?? throw new ArgumentNullException(nameof(password));
        }

        private sealed class SoapTopServiceItem
        {
            public string Id;
            public string BiomaterialId;
            public string LocalizationId;
            public string TransportId;
        }

        public bool CreateOrder(Order order, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                if (order == null) throw new ArgumentNullException(nameof(order));

                var details = order.OrderDetail as GemotestOrderDetail;
                if (details == null) throw new InvalidOperationException("OrderDetail должен быть GemotestOrderDetail.");

                _dictionaries = details.Dicts ?? throw new InvalidOperationException("Dictionaries не назначены: перед отправкой заказа нужно установить details.Dicts (из LaboratoryGemotest.Dicts).");
                if (details.Products == null || details.Products.Count == 0) throw new InvalidOperationException("В заказе нет ни одной услуги.");

                var patient = order.Patient ?? new Patient();

                string extNum = string.IsNullOrWhiteSpace(order.Number)
                    ? "SiMed_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                    : order.Number;

                string orderNum = "";

                DateTime birthDate = patient.Birthday == default(DateTime) ? DateTime.Today : patient.Birthday;

                string createHash = BuildCreateOrderHash(
                    extNum,
                    orderNum,
                    _contractor,
                    patient.Surname ?? "",
                    birthDate,
                    _salt);


                DumpUserSelection(details);


                var rows = BuildSampleServiceRows(details);

                if (rows.Count == 0)
                    throw new InvalidOperationException("Не удалось определить пробы для выбранных услуг (rows=0).");

                DumpRows(rows);


                var tubes = GemotestSamplePacker.Pack(rows);

                if (tubes == null || tubes.Count == 0)
                    throw new InvalidOperationException("Упаковка не дала ни одной пробирки (tubes=0).");

                DumpPacking(tubes);


                long rangeStart, rangeEnd;
                GetSampleIdentifiersRange(tubes.Count, out rangeStart, out rangeEnd);

                long available = (rangeEnd - rangeStart) + 1;
                if (available < tubes.Count)
                    throw new InvalidOperationException("get_sample_identifiers вернул недостаточно идентификаторов.");


                AssignIdentifiers(tubes, rangeStart);


                var topServices = BuildTopLevelServices(details);


                string xml = BuildCreateOrderEnvelopeVariantA(
                    extNum,
                    orderNum,
                    _contractor,
                    createHash,
                    order.AuthorInformation ?? "",
                    patient,
                    topServices,
                    tubes
                );

                string responseXml = SendSoapRequest("create_order", xml);

                var doc = new XmlDocument();
                doc.LoadXml(responseXml);

                var statusNodes = doc.GetElementsByTagName("status");
                string status = statusNodes.Count > 0 ? statusNodes[0].InnerText : "";

                if (!string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                {
                    var errorDescrNodes = doc.GetElementsByTagName("error_description");
                    string errorText = errorDescrNodes.Count > 0 ? errorDescrNodes[0].InnerText : "Неизвестная ошибка create_order.";
                    throw new Exception("Ошибка create_order: " + errorText);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }


        private static int ToInt(object value, int defaultValue)
        {
            if (value == null) return defaultValue;

            if (value is int) return (int)value;
            if (value is long) return (int)(long)value;
            if (value is short) return (short)value;
            if (value is byte) return (byte)value;

            var s = value as string;
            if (s != null)
            {
                int r;
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r))
                    return r;
            }

            return defaultValue;
        }

        private static int ToInt(object value)
        {
            return ToInt(value, 0);
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null) return null;

            if (value is int) return (int)value;
            if (value is long) return (int)(long)value;

            var s = value as string;
            if (s != null)
            {
                int r;
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out r))
                    return r;
            }

            return null;
        }

        private static string ToStr(object value)
        {
            return value == null ? "" : value.ToString();
        }

        private static int MapGender(object sexEnum)
        {


            string s = sexEnum == null ? "" : sexEnum.ToString();
            s = (s ?? "").ToLowerInvariant();

            if (s.Contains("female") || s.Contains("жен")) return 2;
            if (s.Contains("male") || s.Contains("муж")) return 1;
            return 0;
        }

        private void AssignIdentifiers(List<TubePlan> tubes, long rangeStart)
        {
            long cur = rangeStart;

            for (int i = 0; i < tubes.Count; i++)
            {
                tubes[i].SampleIdentifier = cur.ToString(CultureInfo.InvariantCulture);
                cur++;
            }

            for (int i = 0; i < tubes.Count; i++)
            {
                if (tubes[i].Parent != null)
                    tubes[i].PrimarySampleIdentifier = tubes[i].Parent.SampleIdentifier ?? "";
                else
                    tubes[i].PrimarySampleIdentifier = "";
            }
        }

        private List<SoapTopServiceItem> BuildTopLevelServices(GemotestOrderDetail details)
        {
            var res = new List<SoapTopServiceItem>();

            for (int i = 0; i < details.Products.Count; i++)
            {
                var prod = details.Products[i];
                if (prod == null || string.IsNullOrEmpty(prod.ProductId)) continue;

                if (!_dictionaries.Directory.TryGetValue(prod.ProductId, out var svc) || svc == null)
                    continue;

                int? serviceType = svc.service_type;
                if (serviceType == 3 || serviceType == 4)
                    continue;

                res.Add(new SoapTopServiceItem
                {
                    Id = prod.ProductId,
                    BiomaterialId = "",
                    LocalizationId = "",
                    TransportId = ""
                });
            }

            return res;
        }

        private Dictionary<int, string> BuildChosenBiomaterialByProductIndex(GemotestOrderDetail details)
        {
            var map = new Dictionary<int, string>();


            for (int b = 0; b < details.BioMaterials.Count; b++)
            {
                var bio = details.BioMaterials[b];
                if (bio == null) continue;

                for (int i = 0; i < bio.Mandatory.Count; i++)
                {
                    int idx = ToInt(bio.Mandatory[i], -1);
                    if (idx < 0) continue;
                    map[idx] = bio.Id ?? "";
                }
            }

            for (int b = 0; b < details.BioMaterials.Count; b++)
            {
                var bio = details.BioMaterials[b];
                if (bio == null) continue;

                for (int i = 0; i < bio.Chosen.Count; i++)
                {
                    int idx = ToInt(bio.Chosen[i], -1);
                    if (idx < 0) continue;

                    if (!map.ContainsKey(idx))
                        map[idx] = bio.Id ?? "";
                }
            }

            return map;
        }

        private List<SampleServiceRow> BuildSampleServiceRows(GemotestOrderDetail details)
        {
            if (_dictionaries == null)
                throw new InvalidOperationException("Dictionaries не инициализированы в GemotestOrderSender.");

            var rows = new List<SampleServiceRow>();
            var chosenBio = BuildChosenBiomaterialByProductIndex(details);

            for (int i = 0; i < details.Products.Count; i++)
            {
                var prod = details.Products[i];
                if (prod == null || string.IsNullOrEmpty(prod.ProductId)) continue;

                if (!_dictionaries.Directory.TryGetValue(prod.ProductId, out var svc) || svc == null)
                    continue;

                int? serviceType = svc.service_type;
                if (serviceType == 3 || serviceType == 4)
                    continue;


                if (serviceType == 2)
                {
                    AddRowsForMarketingComplex(prod.ProductId, rows);
                    continue;
                }


                string biomaterialId = chosenBio.ContainsKey(i) ? chosenBio[i] : "";
                AddRowsForSimpleService(prod.ProductId, biomaterialId, rows, "", "");
            }

            return rows;
        }

                private void AddRowsForMarketingComplex(string complexId, List<SampleServiceRow> rows)
        {
            if (string.IsNullOrEmpty(complexId))
                return;

            if (_dictionaries.MarketingComplexByComplexId == null ||
                !_dictionaries.MarketingComplexByComplexId.TryGetValue(complexId, out var comp) ||
                comp == null || comp.Count == 0)
            {
                return;
            }

            for (int i = 0; i < comp.Count; i++)
            {
                var c = comp[i];
                if (c == null) continue;
                if (string.IsNullOrEmpty(c.service_id)) continue;

                string bio = c.biomaterial_id ?? "";
                string loc = c.localization_id ?? "";

                AddRowsForSimpleService(c.service_id, bio, rows, complexId, loc);
            }
        }

                private void AddRowsForSimpleService(
            string serviceId,
            string biomaterialId,
            List<SampleServiceRow> rows,
            string complexId,
            string forcedLocalizationId)
        {
            if (string.IsNullOrEmpty(serviceId))
                return;

            if (_dictionaries.SamplesServices == null ||
                !_dictionaries.SamplesServices.TryGetValue(serviceId, out var baseList) ||
                baseList == null)
            {
                baseList = new List<DictionarySamplesServices>();
            }


            var list = baseList;

            if (!string.IsNullOrEmpty(biomaterialId))
                list = list.Where(p => string.Equals(p.biomaterial_id ?? "", biomaterialId, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(forcedLocalizationId))
                list = list.Where(p => string.Equals(p.localization_id ?? "", forcedLocalizationId, StringComparison.OrdinalIgnoreCase)).ToList();


            if (list.Count == 0)
                list = baseList;

            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];

                int execSampleId = ToInt(p.sample_id, 0);
                int serviceCount = ToInt(p.service_count, 1);

                int primaryRaw = ToInt(p.primary_sample_id, 0);
                int? primarySampleId = primaryRaw > 0 ? (int?)primaryRaw : null;

                _dictionaries.Samples.TryGetValue(execSampleId.ToString(CultureInfo.InvariantCulture), out var execSample);

                string execName = execSample != null ? (execSample.name ?? "") : "";
                string execTransport = execSample != null ? (execSample.transport_id ?? "") : "";
                bool execUtilize = execSample != null && execSample.utilize;

                string primName = "";
                string primTransport = "";
                bool primUtilize = false;

                if (primarySampleId.HasValue)
                {
                    _dictionaries.Samples.TryGetValue(primarySampleId.Value.ToString(CultureInfo.InvariantCulture), out var primSample);
                    primName = primSample != null ? (primSample.name ?? "") : "";
                    primTransport = primSample != null ? (primSample.transport_id ?? "") : "";
                    primUtilize = primSample != null && primSample.utilize;
                }

                var row = new SampleServiceRow
                {
                    ServiceId = serviceId ?? "",
                    ComplexId = complexId ?? "",

                    ExecutionSampleId = execSampleId,
                    ExecutionSampleName = execName,
                    ExecutionTransportId = execTransport,
                    ExecutionUtilize = execUtilize,

                    PrimarySampleId = primarySampleId,
                    PrimarySampleName = primName,
                    PrimaryTransportId = primTransport,
                    PrimaryUtilize = primUtilize,

                    BiomaterialId = p.biomaterial_id ?? "",
                    MicroBioBiomaterialId = p.microbiology_biomaterial_id ?? "",
                    LocalizationId = p.localization_id ?? "",

                    ServiceCount = serviceCount <= 0 ? 1 : serviceCount
                };

                rows.Add(row);
            }
        }

        private void GetSampleIdentifiersRange(int count, out long rangeStart, out long rangeEnd)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));


            string hash = BuildContractorHash(_contractor, _salt);

            string xml = BuildGetSampleIdentifiersEnvelope(count, _contractor, hash);
            string resp = SendSoapRequest("get_sample_identifiers", xml);

            bool accepted;
            string errorText;
            ParseGetSampleIdentifiersResponse(resp, out accepted, out rangeStart, out rangeEnd, out errorText);

            if (!accepted)
                throw new Exception("get_sample_identifiers отклонён: " + (errorText ?? ""));
        }

        private string BuildGetSampleIdentifiersEnvelope(int count, string contractor, string hash)
        {
            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ");
            sb.Append("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ");
            sb.Append("xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
            sb.Append("xmlns:urn=\"urn:OdoctorControllerwsdl\" ");
            sb.Append("xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">");

            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");

            sb.Append("<urn:get_sample_identifiers soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            sb.Append("<params xsi:type=\"urn:request_get_sample_identifiers\">");

            sb.Append("<contractor xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(contractor ?? ""))
              .Append("</contractor>");

            sb.Append("<hash xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(hash ?? ""))
              .Append("</hash>");

            sb.Append("<identifiers_count xsi:type=\"xsd:int\">")
              .Append(count.ToString(CultureInfo.InvariantCulture))
              .Append("</identifiers_count>");

            sb.Append("</params>");
            sb.Append("</urn:get_sample_identifiers>");

            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }

        private void ParseGetSampleIdentifiersResponse(
            string responseXml,
            out bool accepted,
            out long rangeStart,
            out long rangeEnd,
            out string errorDesc)
        {
            accepted = false;
            rangeStart = 0;
            rangeEnd = 0;
            errorDesc = "";

            var doc = new XmlDocument();
            doc.LoadXml(responseXml);

            var statusNodes = doc.GetElementsByTagName("status");
            string status = statusNodes.Count > 0 ? (statusNodes[0].InnerText ?? "") : "";

            accepted = string.Equals(status.Trim(), "accepted", StringComparison.OrdinalIgnoreCase);

            var errNodes = doc.GetElementsByTagName("error_description");
            if (errNodes.Count > 0)
                errorDesc = errNodes[0].InnerText ?? "";

            var rsNode = doc.GetElementsByTagName("range_start");
            var reNode = doc.GetElementsByTagName("range_end");

            if (rsNode.Count > 0) long.TryParse(rsNode[0].InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeStart);
            if (reNode.Count > 0) long.TryParse(reNode[0].InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out rangeEnd);
        }

        private string BuildCreateOrderEnvelopeVariantA(
            string extNum,
            string orderNum,
            string contractor,
            string hash,
            string comment,
            Patient patient,
            IList<SoapTopServiceItem> services,
            IList<TubePlan> tubes)
        {
            int svcCount = services != null ? services.Count : 0;
            int tubesCount = tubes != null ? tubes.Count : 0;

            string surname = patient != null ? (patient.Surname ?? "") : "";
            string firstname = patient != null ? (patient.Name ?? "") : "";
            string middlename = patient != null ? (patient.Patronimic ?? "") : "";

            DateTime birthDate = (patient != null && patient.Birthday != default(DateTime)) ? patient.Birthday : DateTime.Today;
            int gender = MapGender(patient != null ? (object)patient.Sex : null);

            var sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ");
            sb.Append("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ");
            sb.Append("xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
            sb.Append("xmlns:urn=\"urn:OdoctorControllerwsdl\" ");
            sb.Append("xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">");

            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<urn:create_order soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            sb.Append("<params xsi:type=\"urn:order\">");

            sb.Append("<ext_num xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(extNum ?? ""))
              .Append("</ext_num>");

            sb.Append("<order_num xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(orderNum ?? ""))
              .Append("</order_num>");

            sb.Append("<contractor xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(contractor ?? ""))
              .Append("</contractor>");

            sb.Append("<hash xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(hash ?? ""))
              .Append("</hash>");


            sb.Append("<doctor xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(comment ?? ""))
              .Append("</doctor>");

            sb.Append("<order_status xsi:type=\"xsd:integer\">0</order_status>");
            sb.Append("<registered xsi:type=\"xsd:integer\">1</registered>");

            sb.Append("<comment xsi:type=\"xsd:string\">")
              .Append(SecurityElement.Escape(comment ?? ""))
              .Append("</comment>");

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
              .Append(gender.ToString(CultureInfo.InvariantCulture))
              .Append("</gender>");

            sb.Append("</patient>");


            sb.Append("<services xsi:type=\"urn:servicesArray\" soapenc:arrayType=\"urn:services[")
              .Append(svcCount.ToString(CultureInfo.InvariantCulture))
              .Append("]\">");

            if (services != null)
            {
                for (int i = 0; i < services.Count; i++)
                {
                    var s = services[i];
                    if (s == null || string.IsNullOrEmpty(s.Id)) continue;

                    sb.Append("<item>");
                    sb.Append("<id xsi:type=\"xsd:string\">")
                      .Append(SecurityElement.Escape(s.Id))
                      .Append("</id>");
                    sb.Append("<biomaterial_id xsi:nil=\"true\"/>");
                    sb.Append("<localization_id xsi:nil=\"true\"/>");
                    sb.Append("<transport_id xsi:nil=\"true\"/>");
                    sb.Append("</item>");
                }
            }

            sb.Append("</services>");


            sb.Append("<order_samples xsi:type=\"urn:order_sampleArray\" soapenc:arrayType=\"urn:order_sample[")
              .Append(tubesCount.ToString(CultureInfo.InvariantCulture))
              .Append("]\">");

            if (tubes != null)
            {
                for (int i = 0; i < tubes.Count; i++)
                {
                    var t = tubes[i];
                    if (t == null) continue;

                    sb.Append("<item>");

                    sb.Append("<sample_id xsi:type=\"xsd:int\">")
                      .Append(t.SampleId.ToString(CultureInfo.InvariantCulture))
                      .Append("</sample_id>");

                    sb.Append("<sample_identifier xsi:type=\"xsd:string\">")
                      .Append(SecurityElement.Escape(t.SampleIdentifier ?? ""))
                      .Append("</sample_identifier>");

                    if (string.IsNullOrEmpty(t.PrimarySampleIdentifier))
                    {
                        sb.Append("<primary_sample_identifier/>");
                    }
                    else
                    {
                        sb.Append("<primary_sample_identifier xsi:type=\"xsd:string\">")
                          .Append(SecurityElement.Escape(t.PrimarySampleIdentifier))
                          .Append("</primary_sample_identifier>");
                    }


                    sb.Append("<microbiology_biomaterial_id>")
                      .Append(SecurityElement.Escape(t.MicroBioBiomaterialId ?? ""))
                      .Append("</microbiology_biomaterial_id>");

                    sb.Append("<localization_id>")
                      .Append(SecurityElement.Escape(t.LocalizationId ?? ""))
                      .Append("</localization_id>");

                    sb.Append("<biomaterial_id>")
                      .Append(SecurityElement.Escape(t.BiomaterialId ?? ""))
                      .Append("</biomaterial_id>");

                    sb.Append("<transport_id>")
                      .Append(SecurityElement.Escape(t.TransportId ?? ""))
                      .Append("</transport_id>");

                    int osCount = t.Services != null ? t.Services.Count : 0;
                    sb.Append("<services xsi:type=\"urn:order_sample_serviceArray\" soapenc:arrayType=\"urn:order_sample_service[")
                      .Append(osCount.ToString(CultureInfo.InvariantCulture))
                      .Append("]\">");

                    if (t.Services != null)
                    {
                        for (int k = 0; k < t.Services.Count; k++)
                        {
                            var ss = t.Services[k];
                            if (ss == null) continue;

                            sb.Append("<item>");
                            sb.Append("<service_id xsi:type=\"xsd:string\">")
                              .Append(SecurityElement.Escape(ss.ServiceId ?? ""))
                              .Append("</service_id>");
                            sb.Append("<complex_id xsi:type=\"xsd:string\">")
                              .Append(SecurityElement.Escape(ss.ComplexId ?? ""))
                              .Append("</complex_id>");
                            sb.Append("<utilization_flag xsi:type=\"xsd:int\">")
                              .Append(ss.UtilizationFlag.ToString(CultureInfo.InvariantCulture))
                              .Append("</utilization_flag>");
                            sb.Append("<refuse_flag xsi:type=\"xsd:int\">")
                              .Append(ss.RefuseFlag.ToString(CultureInfo.InvariantCulture))
                              .Append("</refuse_flag>");
                            sb.Append("</item>");
                        }
                    }

                    sb.Append("</services>");
                    sb.Append("</item>");
                }
            }

            sb.Append("</order_samples>");

            sb.Append("</params>");
            sb.Append("</urn:create_order>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }

        private static string BuildCreateOrderHash(
            string extNum,
            string orderNum,
            string contractor,
            string surname,
            DateTime birthday,
            string salt)
        {
            string birthStr = birthday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            string plain =
                (extNum ?? "") +
                (orderNum ?? "") +
                (contractor ?? "") +
                (surname ?? "") +
                birthStr +
                (salt ?? "");

            using (var sha1 = SHA1.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                byte[] hash = sha1.ComputeHash(data);

                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));

                return sb.ToString();
            }
        }

        private static string BuildContractorHash(string contractor, string salt)
        {
            string plain = (contractor ?? "") + (salt ?? "");

            using (var sha1 = SHA1.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                byte[] hash = sha1.ComputeHash(data);

                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));

                return sb.ToString();
            }
        }

        private string SendSoapRequest(string method, string xmlBody)
        {
            string soapAction = "\"urn:OdoctorControllerwsdl#" + method + "\"";

            Console.WriteLine("========== Gemotest SOAP REQUEST (" + method + ") ==========");
            Console.WriteLine(xmlBody);
            Console.WriteLine("============================================================");

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers["SOAPAction"] = soapAction;

            string credentials = _login + ":" + _password;
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

                    Console.WriteLine("========== Gemotest SOAP RESPONSE (" + method + ") ==========");
                    Console.WriteLine(responseText);
                    Console.WriteLine("=============================================================");

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

                Console.WriteLine("========== Gemotest SOAP ERROR (" + method + ") ==========");
                Console.WriteLine(responseText);
                Console.WriteLine("=========================================================");

                throw new Exception("HTTP error: " + ex.Message + "\r\nResponse: " + responseText, ex);
            }
        }

        private void DumpUserSelection(GemotestOrderDetail details)
        {
            Console.WriteLine();
            Console.WriteLine("=== Выбор пользователя (услуги) ===");
            for (int i = 0; i < details.Products.Count; i++)
            {
                var p = details.Products[i];

                _dictionaries.Directory.TryGetValue(p.ProductId ?? "", out var svc);
                if (svc != null)
                    Console.WriteLine($"    -> type={svc.type}, service_type={(svc.service_type.HasValue ? svc.service_type.Value.ToString() : "null")}");

                Console.WriteLine("[" + i + "] " + (p.ProductId ?? "") + " | " + (p.ProductName ?? ""));
            }


            Console.WriteLine();
            Console.WriteLine("=== Выбор пользователя (биоматериал по услугам) ===");
            var chosen = BuildChosenBiomaterialByProductIndex(details);

            for (int i = 0; i < details.Products.Count; i++)
            {
                string bio = chosen.ContainsKey(i) ? chosen[i] : "";
                string bioName = "";
                if (!string.IsNullOrEmpty(bio))
                {
                    var b = details.BioMaterials.FirstOrDefault(x => x.Id == bio);
                    bioName = b != null ? (b.Name ?? "") : "";
                }

                Console.WriteLine("[" + i + "] bio=" + bio + " " + bioName);
            }
        }

        private void DumpRows(List<SampleServiceRow> rows)
        {
            Console.WriteLine();
            Console.WriteLine("=== Требования проб (samples_services / marketing composition) ===");

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];

                string bioKey = !string.IsNullOrWhiteSpace(r.MicroBioBiomaterialId)
                    ? ("MB:" + r.MicroBioBiomaterialId)
                    : ("BM:" + r.BiomaterialId);

                Console.WriteLine(
                    "[" + i + "] svc=" + r.ServiceId +
                    (string.IsNullOrEmpty(r.ComplexId) ? "" : (" complex=" + r.ComplexId)) +
                    " sample=" + r.ExecutionSampleId +
                    (r.PrimarySampleId.HasValue ? (" primary=" + r.PrimarySampleId.Value) : "") +
                    " loc=" + (r.LocalizationId ?? "") +
                    " " + bioKey +
                    " sc=" + r.ServiceCount +
                    " execTr=" + (r.ExecutionTransportId ?? "")
                );
            }
        }

        private void DumpPacking(List<TubePlan> tubes)
        {
            Console.WriteLine();
            Console.WriteLine("=== Итоговая упаковка (пробирки) ===");

            for (int i = 0; i < tubes.Count; i++)
            {
                var t = tubes[i];
                string parent = t.Parent != null ? (" parentSample=" + t.Parent.SampleId) : "";
                Console.WriteLine(
                    "[" + i + "] sample=" + t.SampleId +
                    parent +
                    " tr=" + (t.TransportId ?? "") +
                    " loc=" + (t.LocalizationId ?? "") +
                    " bio=" + (string.IsNullOrEmpty(t.MicroBioBiomaterialId) ? (t.BiomaterialId ?? "") : ("MB:" + t.MicroBioBiomaterialId)) +
                    " used=" + t.UsedPercent.ToString("0.##", CultureInfo.InvariantCulture) + "%"
                );

                if (t.Services != null)
                {
                    for (int k = 0; k < t.Services.Count; k++)
                    {
                        var s = t.Services[k];
                        Console.WriteLine("    - " + (s.ServiceId ?? "") +
                                          (string.IsNullOrEmpty(s.ComplexId) ? "" : (" (complex " + s.ComplexId + ")")) +
                                          " share=" + s.SharePercent.ToString("0.##", CultureInfo.InvariantCulture) +
                                          "% util=" + s.UtilizationFlag +
                                          " refuse=" + s.RefuseFlag);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Всего контейнеров для отправки: " + tubes.Count);
        }
    }
}
