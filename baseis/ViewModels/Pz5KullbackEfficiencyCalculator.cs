using System;
using System.Collections.Generic;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 5. Задание 3.
    /// Вычисление критерия функциональной эффективности по Кульбаку.
    /// </summary>
    public class Pz5KullbackEfficiencyCalculator
    {
        public void Compute(IEnumerable<Pz5RadiusMetrics> metrics)
        {
            if (metrics == null)
            {
                return;
            }

            double previousValue = 0.0;
            bool hasPreviousValue = false;

            foreach (var metric in metrics)
            {
                if (metric == null)
                {
                    continue;
                }

                double fallback = hasPreviousValue ? previousValue : 0.0;
                double value = CalculateKullbackValue(metric, fallback, 100);
                metric.KullbackKfe = value;

                if (double.IsFinite(value))
                {
                    previousValue = value;
                    hasPreviousValue = true;
                }
            }
        }

        private static double CalculateKullbackValue(Pz5RadiusMetrics metric, double fallbackValue, int n)
        {
            double K1plusK2 = metric.K2 + metric.K3;
            if (K1plusK2 <= double.Epsilon)
            {
                return fallbackValue;
            }

            double numerator = 2.0*n + Math.Pow(10, -2) - K1plusK2;
            if (numerator <= 0.0)
            {
                return 0.0;
            }

            double ratio = numerator / K1plusK2;
            if (ratio <= 0.0)
            {
                return 0.0;
            }

            double result = Math.Log(ratio, 2) * (1.0*n - K1plusK2) / n;
            return double.IsFinite(result) ? result : fallbackValue;
        }
    }
}
