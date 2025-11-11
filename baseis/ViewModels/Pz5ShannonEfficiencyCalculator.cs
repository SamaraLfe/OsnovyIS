using System;
using System.Collections.Generic;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 5. Задание 3.
    /// Вычисление критерия функциональной эффективности по Шеннону.
    /// </summary>
    public class Pz5ShannonEfficiencyCalculator
    {
        public void Compute(IEnumerable<Pz5RadiusMetrics> metrics)
        {
            if (metrics == null)
            {
                return;
            }

            foreach (var metric in metrics)
            {
                if (metric == null)
                {
                    continue;
                }

                double alphaPlusD2 = metric.Alpha + metric.D2;
                double d1PlusBeta = metric.D1 + metric.Beta;

                double termAlpha = ProbabilityTerm(metric.Alpha, alphaPlusD2);
                double termD2 = ProbabilityTerm(metric.D2, alphaPlusD2);
                double termD1 = ProbabilityTerm(metric.D1, d1PlusBeta);
                double termBeta = ProbabilityTerm(metric.Beta, d1PlusBeta);

                metric.ShannonKfe = 1.0 + 0.5 * (termAlpha + termD2 + termD1 + termBeta);
            }
        }

        private static double ProbabilityTerm(double numerator, double denominator)
        {
            if (denominator <= 0)
            {
                return 0.0;
            }

            double ratio = numerator / denominator;
            if (ratio <= 0)
            {
                return 0.0;
            }

            ratio = Math.Min(ratio, 1.0);

            return ratio * Math.Log(ratio, 2);
        }
    }
}
