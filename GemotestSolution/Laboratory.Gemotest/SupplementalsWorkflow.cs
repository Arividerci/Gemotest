using Laboratory.Gemotest.GemotestRequests;
using Laboratory.Gemotest.SourseClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Laboratory.Gemotest
{
    internal static class SupplementalsWorkflow
    {
        private static string NormalizeSupplementalValue(string testId, string raw)
        {
            var v = (raw ?? "").Trim();
            if (v.Length == 0) return "";

            // Для test_id=Contingent в create_order нужно отправлять code, а не отображаемое значение.
            if (string.Equals(testId, "Contingent", StringComparison.OrdinalIgnoreCase))
            {
                var cut = v;
                int dash = cut.IndexOf('-');
                if (dash > 0) cut = cut.Substring(0, dash);
                int sp = cut.IndexOf(' ');
                if (sp > 0) cut = cut.Substring(0, sp);
                return cut.Trim();
            }

            // Формат даты в supplementals: DD/MM/YYYY
            if (DateTime.TryParse(v, out var dt))
                return dt.ToString("dd/MM/yyyy");

            if (string.Equals(v, "да", StringComparison.OrdinalIgnoreCase)) return "true";
            if (string.Equals(v, "нет", StringComparison.OrdinalIgnoreCase)) return "false";

            return v;
        }

        private static List<int> ResolveMandatoryProductIndexes(GemotestOrderDetail details, DictionaryServicesSupplementals rule)
        {
            var idxs = new List<int>();
            if (details?.Products == null || details.Products.Count == 0)
                return idxs;

            var dicts = details.Dicts;
            var parentId = (rule?.parent_id ?? "").Trim();

            for (int i = 0; i < details.Products.Count; i++)
            {
                var pid = (details.Products[i]?.ProductId ?? "").Trim();
                if (pid.Length == 0) continue;

                if (parentId.Length > 0 && string.Equals(pid, parentId, StringComparison.OrdinalIgnoreCase))
                {
                    idxs.Add(i);
                    continue;
                }

                // Если выбран маркетинговый комплекс, а supplemental относится к услуге внутри комплекса —
                // привязываем supplemental к самому комплексу, чтобы поле не удалялось как «осиротевшее».
                if (dicts?.MarketingComplexByComplexId != null &&
                    dicts.MarketingComplexByComplexId.TryGetValue(pid, out var items) &&
                    items != null && parentId.Length > 0)
                {
                    if (items.Any(x => x != null && string.Equals(x.service_id ?? "", parentId, StringComparison.OrdinalIgnoreCase)))
                        idxs.Add(i);
                }
            }

            // Фолбек: иначе DeleteObsoleteDetails() может удалить поле и оно не уйдёт в create_order.
            if (idxs.Count == 0)
            {
                for (int i = 0; i < details.Products.Count; i++)
                    idxs.Add(i);
            }

            return idxs;
        }

        public static bool EnsureSupplementals(GemotestOrderDetail details, IWin32Window owner, List<string> serviceIds)
        {
            if (details == null) return true;

            var dicts = details.Dicts;
            if (dicts == null) return true;

            var serviceSet = new HashSet<string>(serviceIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var rawRequired = new List<DictionaryServicesSupplementals>();

            foreach (var sid in serviceSet)
            {
                if (string.IsNullOrWhiteSpace(sid)) continue;

                if (!dicts.ServicesSupplementals.TryGetValue(sid, out var list) || list == null || list.Count == 0)
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item != null && item.required)
                        rawRequired.Add(item);
                }
            }

            var required = rawRequired
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.test_id))
                .GroupBy(x => x.test_id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (required.Count == 0)
                return true;

            using (var f = new SupplementalsForm(required))
            {
                if (f.ShowDialog(owner) != DialogResult.OK)
                    return false;

                if (details.Details == null)
                    details.Details = new List<GemotestDetail>();

                var values = f.Values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var r in required)
                {
                    var key = r.test_id ?? string.Empty;
                    values.TryGetValue(key, out var val);
                    if (val == null) val = string.Empty;

                    val = NormalizeSupplementalValue(key, val);

                    var mandatoryIdxs = ResolveMandatoryProductIndexes(details, r);

                    var ex = details.Details.FirstOrDefault(d =>
                        d != null && string.Equals(d.Code, key, StringComparison.OrdinalIgnoreCase));

                    if (ex == null)
                    {
                        details.Details.Add(new GemotestDetail
                        {
                            Code = key,
                            Name = r.name,
                            Value = val
                        });

                        var added = details.Details.Last();
                        foreach (var mi in mandatoryIdxs)
                            if (!added.MandatoryProducts.Contains(mi))
                                added.MandatoryProducts.Add(mi);
                    }
                    else
                    {
                        ex.Name = r.name;
                        ex.Value = val;

                        foreach (var mi in mandatoryIdxs)
                            if (!ex.MandatoryProducts.Contains(mi))
                                ex.MandatoryProducts.Add(mi);
                    }
                }
            }

            return true;
        }
    }
}
