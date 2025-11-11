using System;
using System.Collections.Generic;
using System.Linq;

namespace baseis.ViewModels
{
    /// <summary>
    /// Набор параметров перебора PZ6.
    /// </summary>
    public sealed class Pz6OptimizationSettings
    {
        public Pz6OptimizationSettings(IReadOnlyList<int> deltaCandidates, IReadOnlyList<double> selecCandidates, double scoreTolerance = 1e-6)
        {
            DeltaCandidates = deltaCandidates ?? throw new ArgumentNullException(nameof(deltaCandidates));
            SelecCandidates = selecCandidates ?? throw new ArgumentNullException(nameof(selecCandidates));
            ScoreTolerance = scoreTolerance;
        }

        public IReadOnlyList<int> DeltaCandidates { get; }
        public IReadOnlyList<double> SelecCandidates { get; }
        public double ScoreTolerance { get; }

        public static Pz6OptimizationSettings CreateDefault(int currentDelta, double currentSelec)
        {
            var deltaValues = new SortedSet<int>();
            deltaValues.Add(0);
            for (int value = 25; value <= 75; value += 1)
            {
                deltaValues.Add(value);
            }

            deltaValues.Add(Math.Clamp(currentDelta, 0, 255));

            var selecValues = new SortedSet<double>();
            for (int pct = 25; pct <= 75; pct += 1)
            {
                selecValues.Add(pct / 100.0);
            }

            selecValues.Add(Math.Round(Math.Clamp(currentSelec, 0.0, 1.0), 2));

            return new Pz6OptimizationSettings(deltaValues.ToList(), selecValues.ToList());
        }
    }

    /// <summary>
    /// Снимок рассчитанных метрик для фиксированных параметров.
    /// </summary>
    public sealed class Pz6OptimizationSnapshot
    {
        public Pz6OptimizationSnapshot(int delta, double selec, IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> metricsByClass)
        {
            Delta = delta;
            Selec = selec;
            MetricsByClass = CloneMetrics(metricsByClass);
            BestShannonByClass = CalculateBestValues(MetricsByClass, metric => metric.ShannonKfe);
            BestKullbackByClass = CalculateBestValues(MetricsByClass, metric => metric.KullbackKfe);
        }

        public int Delta { get; }
        public double Selec { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> MetricsByClass { get; }
        public IReadOnlyDictionary<int, double> BestShannonByClass { get; }
        public IReadOnlyDictionary<int, double> BestKullbackByClass { get; }

        public double TotalBestShannon => SumFinite(BestShannonByClass.Values);
        public double TotalBestKullback => SumFinite(BestKullbackByClass.Values);

        private static double SumFinite(IEnumerable<double> values)
        {
            double sum = 0.0;
            bool hasValue = false;

            foreach (var value in values)
            {
                if (double.IsFinite(value))
                {
                    sum += value;
                    hasValue = true;
                }
            }

            return hasValue ? sum : double.NaN;
        }

        private static IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> CloneMetrics(IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> source)
        {
            var result = new Dictionary<int, IReadOnlyList<Pz5RadiusMetrics>>();

            foreach (var kvp in source)
            {
                var clonedList = kvp.Value.Select(metric => metric.Clone()).ToList();
                result[kvp.Key] = clonedList;
            }

            return result;
        }

        private static IReadOnlyDictionary<int, double> CalculateBestValues(
            IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> metricsByClass,
            Func<Pz5RadiusMetrics, double> selector)
        {
            var bestValues = new Dictionary<int, double>();

            foreach (var (classIndex, metrics) in metricsByClass)
            {
                double best = double.NaN;
                foreach (var metric in metrics)
                {
                    if (!metric.IsReliable)
                    {
                        continue;
                    }

                    double value = selector(metric);
                    if (!double.IsFinite(value))
                    {
                        continue;
                    }

                    if (!double.IsFinite(best) || value > best)
                    {
                        best = value;
                    }
                }

                bestValues[classIndex] = best;
            }

            return bestValues;
        }
    }

    /// <summary>
    /// Итог оптимизации.
    /// </summary>
    public sealed class Pz6OptimizationResult
    {
        public Pz6OptimizationResult(Pz6OptimizationSnapshot original, Pz6OptimizationSnapshot optimized)
        {
            Original = original;
            Optimized = optimized;
        }

        public Pz6OptimizationSnapshot Original { get; }
        public Pz6OptimizationSnapshot Optimized { get; }

        public double ScoreGain
        {
            get
            {
                double orig = Original.TotalBestShannon;
                double opt = Optimized.TotalBestShannon;
                if (!double.IsFinite(orig) || !double.IsFinite(opt))
                {
                    return double.NaN;
                }

                return opt - orig;
            }
        }
    }
}
