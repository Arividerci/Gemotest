using System;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Gemotest
{
    public sealed class SampleServiceRow
    {
        public string ServiceId { get; set; }
        public string ComplexId { get; set; } // если услуга входит в маркетинговый комплекс

        public int ExecutionSampleId { get; set; }
        public string ExecutionSampleName { get; set; }
        public string ExecutionTransportId { get; set; }
        public bool ExecutionUtilize { get; set; }

        public int? PrimarySampleId { get; set; } // если есть - значит алиquot
        public string PrimarySampleName { get; set; }
        public string PrimaryTransportId { get; set; }
        public bool PrimaryUtilize { get; set; }

        public string BiomaterialId { get; set; }
        public string MicroBioBiomaterialId { get; set; }
        public string LocalizationId { get; set; }

        public int ServiceCount { get; set; } // доля: 100 / service_count

        public SampleServiceRow()
        {
            ServiceId = "";
            ComplexId = "";

            ExecutionSampleName = "";
            ExecutionTransportId = "";

            PrimarySampleName = "";
            PrimaryTransportId = "";

            BiomaterialId = "";
            MicroBioBiomaterialId = "";
            LocalizationId = "";

            ServiceCount = 1;
        }
    }

    public sealed class TubeServicePlan
    {
        public string ServiceId { get; set; }
        public string ComplexId { get; set; }

        public int UtilizationFlag { get; set; } // 0/1
        public int RefuseFlag { get; set; }      // 0/1

        public int ServiceCount { get; set; }
        public double SharePercent { get; set; }

        public TubeServicePlan()
        {
            ServiceId = "";
            ComplexId = "";
            ServiceCount = 1;
        }
    }

    public sealed class TubePlan
    {
        public int SampleId { get; set; }
        public string SampleName { get; set; }
        public string TransportId { get; set; }
        public bool Utilize { get; set; }

        public string BiomaterialId { get; set; }
        public string MicroBioBiomaterialId { get; set; }
        public string LocalizationId { get; set; }

        // присваивается в sender-е после get_sample_identifiers
        public string SampleIdentifier { get; set; }
        public string PrimarySampleIdentifier { get; set; }

        public TubePlan Parent { get; set; } // null для первичной

        public double UsedPercent { get; set; }
        public List<TubeServicePlan> Services { get; set; }

        public TubePlan()
        {
            SampleName = "";
            TransportId = "";
            BiomaterialId = "";
            MicroBioBiomaterialId = "";
            LocalizationId = "";

            SampleIdentifier = "";
            PrimarySampleIdentifier = "";

            Services = new List<TubeServicePlan>();
        }
    }

    public static class GemotestSamplePacker
    {
        private const double Capacity = 100.0;
        private const double Eps = 1e-9;

        private sealed class WorkItem
        {
            public SampleServiceRow Src;

            public int DrawSampleId;
            public string DrawSampleName;
            public string DrawTransportId;
            public bool DrawUtilize;

            public int UtilizationFlag; // 0/1

            public double Share;
        }

        private struct BioKey : IEquatable<BioKey>
        {
            public readonly string Kind;
            public readonly string Value;

            public BioKey(string kind, string value)
            {
                Kind = kind ?? "";
                Value = value ?? "";
            }

            public bool Equals(BioKey other)
            {
                return Kind == other.Kind && Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return obj is BioKey && Equals((BioKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Kind.GetHashCode() * 397) ^ Value.GetHashCode();
                }
            }

            public override string ToString()
            {
                return Kind + ":" + Value;
            }
        }

        private struct MergeKey : IEquatable<MergeKey>
        {
            public readonly BioKey Bio;
            public readonly string Loc;
            public readonly string Transport;

            public MergeKey(BioKey bio, string loc, string transport)
            {
                Bio = bio;
                Loc = loc ?? "";
                Transport = transport ?? "";
            }

            public bool Equals(MergeKey other)
            {
                return Bio.Equals(other.Bio) && Loc == other.Loc && Transport == other.Transport;
            }

            public override bool Equals(object obj)
            {
                return obj is MergeKey && Equals((MergeKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 31 + Bio.GetHashCode();
                    h = h * 31 + Loc.GetHashCode();
                    h = h * 31 + Transport.GetHashCode();
                    return h;
                }
            }
        }

        private struct PrimaryPackKey : IEquatable<PrimaryPackKey>
        {
            public readonly int SampleId;
            public readonly BioKey Bio;
            public readonly string Loc;
            public readonly string Transport;

            public PrimaryPackKey(int sampleId, BioKey bio, string loc, string transport)
            {
                SampleId = sampleId;
                Bio = bio;
                Loc = loc ?? "";
                Transport = transport ?? "";
            }

            public bool Equals(PrimaryPackKey other)
            {
                return SampleId == other.SampleId && Bio.Equals(other.Bio) && Loc == other.Loc && Transport == other.Transport;
            }

            public override bool Equals(object obj)
            {
                return obj is PrimaryPackKey && Equals((PrimaryPackKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 31 + SampleId.GetHashCode();
                    h = h * 31 + Bio.GetHashCode();
                    h = h * 31 + Loc.GetHashCode();
                    h = h * 31 + Transport.GetHashCode();
                    return h;
                }
            }
        }

        private struct AliquotPackKey : IEquatable<AliquotPackKey>
        {
            public readonly int ExecSampleId;
            public readonly BioKey Bio;
            public readonly string Loc;
            public readonly string Transport;

            public AliquotPackKey(int execSampleId, BioKey bio, string loc, string transport)
            {
                ExecSampleId = execSampleId;
                Bio = bio;
                Loc = loc ?? "";
                Transport = transport ?? "";
            }

            public bool Equals(AliquotPackKey other)
            {
                return ExecSampleId == other.ExecSampleId && Bio.Equals(other.Bio) && Loc == other.Loc && Transport == other.Transport;
            }

            public override bool Equals(object obj)
            {
                return obj is AliquotPackKey && Equals((AliquotPackKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 31 + ExecSampleId.GetHashCode();
                    h = h * 31 + Bio.GetHashCode();
                    h = h * 31 + Loc.GetHashCode();
                    h = h * 31 + Transport.GetHashCode();
                    return h;
                }
            }
        }

        private sealed class Bin
        {
            public double Remaining;
            public double Used;
            public List<WorkItem> Items;

            public Bin()
            {
                Remaining = Capacity;
                Used = 0.0;
                Items = new List<WorkItem>();
            }
        }

        public static List<TubePlan> Pack(List<SampleServiceRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            // 0) подготовка work items
            var items = new List<WorkItem>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                int sc = r.ServiceCount <= 0 ? 1 : r.ServiceCount;
                double share = Capacity / sc;

                int drawId = r.PrimarySampleId.HasValue ? r.PrimarySampleId.Value : r.ExecutionSampleId;
                string drawName = r.PrimarySampleId.HasValue ? (r.PrimarySampleName ?? "") : (r.ExecutionSampleName ?? "");
                string drawTransport = r.PrimarySampleId.HasValue ? (r.PrimaryTransportId ?? "") : (r.ExecutionTransportId ?? "");
                bool drawUtilize = r.PrimarySampleId.HasValue ? r.PrimaryUtilize : r.ExecutionUtilize;

                items.Add(new WorkItem
                {
                    Src = r,
                    DrawSampleId = drawId,
                    DrawSampleName = drawName,
                    DrawTransportId = drawTransport,
                    DrawUtilize = drawUtilize,
                    UtilizationFlag = 0,
                    Share = share
                });
            }

            // 1) merge utilize -> non-utilize (если совпали bio+loc+transport)
            var groupsForMerge = items.GroupBy(x => new MergeKey(GetBioKey(x.Src), x.Src.LocalizationId ?? "", x.DrawTransportId ?? "")).ToList();
            foreach (var g in groupsForMerge)
            {
                var nonUtil = g.FirstOrDefault(x => x.DrawUtilize == false);
                if (nonUtil == null) continue;

                foreach (var it in g)
                {
                    if (it.DrawUtilize == true)
                    {
                        it.DrawSampleId = nonUtil.DrawSampleId;
                        it.DrawSampleName = nonUtil.DrawSampleName;
                        it.DrawTransportId = nonUtil.DrawTransportId;
                        it.DrawUtilize = nonUtil.DrawUtilize;
                        it.UtilizationFlag = 1;
                    }
                }
            }

            // 2) бин-пэкинг первичных пробирок
            var primaryPlans = new List<TubePlan>();
            var itemsByPrimaryKey = items.GroupBy(x => new PrimaryPackKey(x.DrawSampleId, GetBioKey(x.Src), x.Src.LocalizationId ?? "", x.DrawTransportId ?? "")).ToList();

            foreach (var g in itemsByPrimaryKey)
            {
                var binList = BestFitDecreasing(g.ToList());

                foreach (var b in binList)
                {
                    // первичная пробирка
                    var p = new TubePlan
                    {
                        Parent = null,
                        SampleId = g.Key.SampleId,
                        SampleName = b.Items.Count > 0 ? (b.Items[0].DrawSampleName ?? "") : "",
                        TransportId = g.Key.Transport ?? "",
                        Utilize = b.Items.Count > 0 && b.Items[0].DrawUtilize,
                        BiomaterialId = ResolveBiomaterialId(g.Key.Bio),
                        MicroBioBiomaterialId = ResolveMicroBioId(g.Key.Bio),
                        LocalizationId = g.Key.Loc ?? "",
                        UsedPercent = b.Used
                    };

                    // услуги в первичной (aliquot-услуги тут refuse=1)
                    foreach (var it in b.Items)
                    {
                        var r = it.Src;
                        int sc = r.ServiceCount <= 0 ? 1 : r.ServiceCount;
                        double share = Capacity / sc;

                        p.Services.Add(new TubeServicePlan
                        {
                            ServiceId = r.ServiceId ?? "",
                            ComplexId = r.ComplexId ?? "",
                            UtilizationFlag = it.UtilizationFlag,
                            RefuseFlag = r.PrimarySampleId.HasValue ? 1 : 0,
                            ServiceCount = sc,
                            SharePercent = share
                        });
                    }

                    primaryPlans.Add(p);

                    // 3) алиquots внутри этой первичной пробирки
                    var aliquotItems = b.Items.Where(x => x.Src.PrimarySampleId.HasValue).ToList();
                    if (aliquotItems.Count == 0) continue;

                    var aliquotGroups = aliquotItems.GroupBy(x => new AliquotPackKey(
                        x.Src.ExecutionSampleId,
                        GetBioKey(x.Src),
                        x.Src.LocalizationId ?? "",
                        x.Src.ExecutionTransportId ?? ""
                    )).ToList();

                    foreach (var ag in aliquotGroups)
                    {
                        var aliquotBins = BestFitDecreasing(ag.ToList());

                        foreach (var ab in aliquotBins)
                        {
                            var a = new TubePlan
                            {
                                Parent = p,
                                SampleId = ag.Key.ExecSampleId,
                                SampleName = ab.Items.Count > 0 ? (ab.Items[0].Src.ExecutionSampleName ?? "") : "",
                                TransportId = ag.Key.Transport ?? "",
                                Utilize = false, // алиquot не "utilize"
                                BiomaterialId = ResolveBiomaterialId(ag.Key.Bio),
                                MicroBioBiomaterialId = ResolveMicroBioId(ag.Key.Bio),
                                LocalizationId = ag.Key.Loc ?? "",
                                UsedPercent = ab.Used
                            };

                            foreach (var it in ab.Items)
                            {
                                var r = it.Src;
                                int sc = r.ServiceCount <= 0 ? 1 : r.ServiceCount;
                                double share = Capacity / sc;

                                a.Services.Add(new TubeServicePlan
                                {
                                    ServiceId = r.ServiceId ?? "",
                                    ComplexId = r.ComplexId ?? "",
                                    UtilizationFlag = it.UtilizationFlag,
                                    RefuseFlag = 0,
                                    ServiceCount = sc,
                                    SharePercent = share
                                });
                            }

                            primaryPlans.Add(a);
                        }
                    }
                }
            }

            // вернуть: родители первыми, потом алиquots (они уже добавлены после родителя, т.к. мы добавляли по месту)
            return primaryPlans;
        }

        private static List<Bin> BestFitDecreasing(List<WorkItem> workItems)
        {
            workItems.Sort((a, b) => b.Share.CompareTo(a.Share));

            var bins = new List<Bin>();

            for (int i = 0; i < workItems.Count; i++)
            {
                var it = workItems[i];

                Bin best = null;
                double bestRemain = double.MaxValue;

                for (int j = 0; j < bins.Count; j++)
                {
                    var b = bins[j];
                    if (b.Remaining + Eps >= it.Share)
                    {
                        double rem = b.Remaining - it.Share;
                        if (rem < bestRemain)
                        {
                            bestRemain = rem;
                            best = b;
                        }
                    }
                }

                if (best == null)
                {
                    best = new Bin();
                    bins.Add(best);
                }

                best.Items.Add(it);
                best.Remaining -= it.Share;
                best.Used += it.Share;
            }

            return bins;
        }

        private static BioKey GetBioKey(SampleServiceRow r)
        {
            if (!string.IsNullOrWhiteSpace(r.MicroBioBiomaterialId))
                return new BioKey("MB", r.MicroBioBiomaterialId.Trim());

            return new BioKey("BM", (r.BiomaterialId ?? "").Trim());
        }

        private static string ResolveBiomaterialId(BioKey key)
        {
            if (key.Kind == "BM") return key.Value;
            return ""; // для микробиологии biomaterial_id может быть пустым
        }

        private static string ResolveMicroBioId(BioKey key)
        {
            if (key.Kind == "MB") return key.Value;
            return "";
        }
    }
}
