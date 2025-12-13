using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml;

namespace Gemotest
{
    public sealed class GemotestAnalysisResult
    {
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; } = "";

        public string ExtNum { get; set; } = "";
        public string OrderNum { get; set; } = "";
        public DateTime? DateTime { get; set; }

        public int Status { get; set; } // <status xsi:type="xsd:integer">1</status>
        public string Hash { get; set; } = "";

        public string PdfUrl { get; set; } = ""; // HtmlDecode(&amp;)

        public List<ClResultRow> ClResults { get; set; } = new List<ClResultRow>();
        public List<MbServiceRow> MbServices { get; set; } = new List<MbServiceRow>();
    }

    public sealed class ClResultRow
    {
        public string ServiceId { get; set; } = "";
        public string SectionName { get; set; } = "";

        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string TestRusName { get; set; } = "";

        public string Value { get; set; } = "";
        public string MeasurementUnit { get; set; } = "";

        public string RefMin { get; set; } = "";
        public string RefMax { get; set; } = "";
        public string RefRange { get; set; } = "";
        public string RefText { get; set; } = "";

        public int StatusCl { get; set; }
        public string ResultDate { get; set; } = "";
    }

    public sealed class MbServiceRow
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MatId { get; set; } = "";
        public string MatName { get; set; } = "";
        public string LocId { get; set; } = "";
        public string LocName { get; set; } = "";
        public int StatusMb { get; set; }

        public List<MbMicrobeRow> Microbes { get; set; } = new List<MbMicrobeRow>();
    }

    public sealed class MbMicrobeRow
    {
        public string Id { get; set; } = "";
        public string Value { get; set; } = "";
        public string Norma { get; set; } = "";
    }

    public static class GemotestAnalysisResultParser
    {
        public static GemotestAnalysisResult Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("Пустой XML-ответ.", nameof(xml));

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var res = new GemotestAnalysisResult();

            // return node
            var returnNode = doc.SelectSingleNode("//*[local-name()='get_analysis_resultResponse']/*[local-name()='return']");
            if (returnNode == null)
                throw new InvalidOperationException("Не найден узел get_analysis_resultResponse/return.");

            // error_description
            res.ErrorCode = ReadInt(returnNode.SelectSingleNode("*[local-name()='error_description']/*[local-name()='error_code']"));
            res.ErrorDescription = ReadText(returnNode.SelectSingleNode("*[local-name()='error_description']/*[local-name()='error_description']"));

            // head fields
            res.ExtNum = ReadText(returnNode.SelectSingleNode("*[local-name()='ext_num']"));
            res.OrderNum = ReadText(returnNode.SelectSingleNode("*[local-name()='order_num']"));

            var dtText = ReadText(returnNode.SelectSingleNode("*[local-name()='date']"));
            res.DateTime = ParseDateTimeLoose(dtText);

            res.Status = ReadInt(returnNode.SelectSingleNode("*[local-name()='status']"));
            res.Hash = ReadText(returnNode.SelectSingleNode("*[local-name()='hash']"));

            // attachments/file (HtmlDecode amp;)
            var fileNode = doc.SelectSingleNode("//*[local-name()='attachments']/*[local-name()='item']/*[local-name()='file']");
            var rawUrl = ReadText(fileNode);
            res.PdfUrl = string.IsNullOrWhiteSpace(rawUrl) ? "" : WebUtility.HtmlDecode(rawUrl);

            // results_cl
            var clItems = doc.SelectNodes("//*[local-name()='results_cl']/*[local-name()='item']");
            if (clItems != null)
            {
                foreach (XmlNode item in clItems)
                {
                    var row = new ClResultRow
                    {
                        Id = ReadText(item.SelectSingleNode("*[local-name()='id']")),
                        Name = ReadText(item.SelectSingleNode("*[local-name()='name']")),
                        TestRusName = ReadText(item.SelectSingleNode("*[local-name()='test_rusname']")),
                        SectionName = ReadText(item.SelectSingleNode("*[local-name()='section_name']")),

                        Value = ReadText(item.SelectSingleNode("*[local-name()='value']")),
                        MeasurementUnit = ReadText(item.SelectSingleNode("*[local-name()='measurement_unit']")),

                        RefMin = ReadText(item.SelectSingleNode("*[local-name()='ref_min']")),
                        RefMax = ReadText(item.SelectSingleNode("*[local-name()='ref_max']")),
                        RefRange = ReadText(item.SelectSingleNode("*[local-name()='ref_range']")),
                        RefText = ReadText(item.SelectSingleNode("*[local-name()='ref_text']")),

                        StatusCl = ReadInt(item.SelectSingleNode("*[local-name()='status_cl']")),
                        ResultDate = ReadText(item.SelectSingleNode("*[local-name()='result_date']")),

                        ServiceId = ReadText(item.SelectSingleNode("*[local-name()='service_id']")),
                    };

                    res.ClResults.Add(row);
                }
            }

            // results_mb/service_mb
            var mbServices = doc.SelectNodes("//*[local-name()='results_mb']//*[local-name()='service_mb']/*[local-name()='item']");
            if (mbServices != null)
            {
                foreach (XmlNode svc in mbServices)
                {
                    var row = new MbServiceRow
                    {
                        Id = ReadText(svc.SelectSingleNode("*[local-name()='id']")),
                        Name = ReadText(svc.SelectSingleNode("*[local-name()='name']")),
                        MatId = ReadText(svc.SelectSingleNode("*[local-name()='mat_id']")),
                        MatName = ReadText(svc.SelectSingleNode("*[local-name()='mat_name']")),
                        LocId = ReadText(svc.SelectSingleNode("*[local-name()='loc_id']")),
                        LocName = ReadText(svc.SelectSingleNode("*[local-name()='loc_name']")),
                        StatusMb = ReadInt(svc.SelectSingleNode("*[local-name()='status_mb']"))
                    };

                    var microbes = svc.SelectNodes("*[local-name()='microbe']/*[local-name()='item']");
                    if (microbes != null)
                    {
                        foreach (XmlNode m in microbes)
                        {
                            row.Microbes.Add(new MbMicrobeRow
                            {
                                Id = ReadText(m.SelectSingleNode("*[local-name()='id']")),
                                Value = ReadText(m.SelectSingleNode("*[local-name()='value']")),
                                Norma = ReadText(m.SelectSingleNode("*[local-name()='norma']"))
                            });
                        }
                    }

                    res.MbServices.Add(row);
                }
            }

            return res;
        }

        private static string ReadText(XmlNode node)
        {
            if (node == null) return "";
            if (IsNil(node)) return "";
            return (node.InnerText ?? "").Trim();
        }

        private static int ReadInt(XmlNode node)
        {
            var s = ReadText(node);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static bool IsNil(XmlNode node)
        {
            var nil = node.Attributes?["nil", "http://www.w3.org/2001/XMLSchema-instance"];
            return nil != null && string.Equals(nil.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime? ParseDateTimeLoose(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            // В примере: "2023-03-15 13:35:58.910" (не ISO с 'T') :contentReference[oaicite:1]{index=1}
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                return dt;

            if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.AssumeLocal, out dt))
                return dt;

            return null;
        }
    }
}
