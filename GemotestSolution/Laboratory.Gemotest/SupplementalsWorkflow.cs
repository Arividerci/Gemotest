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
                    }
                    else
                    {
                        ex.Name = r.name;
                        ex.Value = val;
                    }
                }
            }

            return true;
        }
    }
}
