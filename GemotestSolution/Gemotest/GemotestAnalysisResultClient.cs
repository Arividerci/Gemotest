using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace Laboratory.Gemotest
{
    public sealed class GemotestAnalysisResultClient
    {
        private readonly string _url;
        private readonly string _contractor;
        private readonly string _salt;
        private readonly string _login;
        private readonly string _password;

        public GemotestAnalysisResultClient(string url, string contractor, string salt, string login, string password)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _contractor = contractor ?? throw new ArgumentNullException(nameof(contractor));
            _salt = salt ?? throw new ArgumentNullException(nameof(salt));
            _login = login ?? "";
            _password = password ?? "";
        }

        public string GetAnalysisResultRaw(string orderNum)
        {
            if (string.IsNullOrWhiteSpace(orderNum))
                throw new ArgumentException("orderNum пуст.", nameof(orderNum));

            orderNum = orderNum.Trim();

            // По твоему требованию: hash = contractor + salt (через SHA1)
            string hash = Sha1Hex(_contractor + _salt);

            string requestXml = BuildEnvelope(orderNum, _contractor, hash);
            return PostSoapWithBasicAuth(_url, requestXml, _login, _password);
        }

        private static string BuildEnvelope(string orderNum, string contractor, string hash)
        {
            return
$@"<soapenv:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                  xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                  xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:urn=""urn:OdoctorControllerwsdl"">
   <soapenv:Header/>
   <soapenv:Body>
      <urn:get_analysis_result soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
         <params xsi:type=""urn:request_get_analysis_result"">
            <order_num xsi:type=""xsd:string"">{XmlEscape(orderNum)}</order_num>
            <contractor xsi:type=""xsd:string"">{XmlEscape(contractor)}</contractor>
            <hash xsi:type=""xsd:string"">{XmlEscape(hash)}</hash>
         </params>
      </urn:get_analysis_result>
   </soapenv:Body>
</soapenv:Envelope>";
        }

        private static string PostSoapWithBasicAuth(string url, string bodyXml, string login, string password)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=utf-8";
            req.Accept = "text/xml";
            req.Timeout = 60000;

            // Basic Auth
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{password}"));
            req.Headers["Authorization"] = "Basic " + token;

            byte[] data = Encoding.UTF8.GetBytes(bodyXml);
            using (var rs = req.GetRequestStream())
                rs.Write(data, 0, data.Length);

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var stream = resp.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private static string Sha1Hex(string s)
        {
            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(s ?? "");
                var hash = sha1.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string XmlEscape(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                    .Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
