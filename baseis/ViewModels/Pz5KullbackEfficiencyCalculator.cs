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
                double value = CalculateKullbackValue(metric, fallback);
                metric.KullbackKfe = value;

                if (double.IsFinite(value))
                {
                    previousValue = value;
                    hasPreviousValue = true;
                }
            }
        }

        private static double CalculateKullbackValue(Pz5RadiusMetrics metric, double fallbackValue)
        {
            double errorSum = metric.Alpha + metric.Beta;
            if (errorSum <= double.Epsilon)
            {
                return fallbackValue;
            }

            double numerator = 2.0 - errorSum;
            if (numerator <= 0.0)
            {
                return 0.0;
            }

            double ratio = numerator / errorSum;
            if (ratio <= 0.0)
            {
                return 0.0;
            }

            double result = Math.Log(ratio, 2) * (1.0 - errorSum);
            return double.IsFinite(result) ? result : fallbackValue;
        }
    }
}
