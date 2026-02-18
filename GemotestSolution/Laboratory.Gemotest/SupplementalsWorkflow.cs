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
            if (serviceIds == null) serviceIds = new List<string>();

            // обязательные доп.поля для выбранных услуг
            var required = Dictionaries.ServicesSupplementals
                .Where(x => x != null && x.required && serviceIds.Contains(x.parent_id))
                .GroupBy(x => x.test_id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (required.Count == 0)
                return true;

            using (var f = new SupplementalsForm(required))
            {
                if (f.ShowDialog(owner) != DialogResult.OK)
                    return false;

                // Сохраняем в details.Details (у тебя это уже используется для доп.инфо)
                if (details.Details == null)
                    details.Details = new List<GemotestDetail>();

                foreach (var r in required)
                {
                    string val = f.Values.ContainsKey(r.test_id) ? f.Values[r.test_id] : "";
                    var ex = details.Details.FirstOrDefault(d => d != null && string.Equals(d.Code, r.test_id, StringComparison.OrdinalIgnoreCase));
                    if (ex == null)
                    {
                        details.Details.Add(new GemotestDetail
                        {
                            Code = r.test_id,
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
