using System;
using System.Collections.Generic;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 5. Задание 1.
    /// Вычисление точностных характеристик (D1, alpha, beta, D2) для диапазона радиусов.
    /// </summary>
    public class Pz5AccuracyMetricsCalculator
    {
        private const double ReliabilityThreshold = 0.5;
        private readonly MainWindowViewModel _viewModel;
        private readonly Dictionary<int, int> _maxRadiusByClass = new();

        public Pz5AccuracyMetricsCalculator(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public IReadOnlyDictionary<int, int> MaxRadiusByClass => _maxRadiusByClass;

        /// <summary>
        /// Возвращает метрики для каждого класса и радиуса.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> Compute()
        {
            _maxRadiusByClass.Clear();

            var skMatrix = _viewModel.GetSkMatrix();
            int classCount = skMatrix.GetLength(0);
            int featureCount = skMatrix.GetLength(2);
            var referenceVectors = _viewModel.GetXmMatrix();
            var result = new Dictionary<int, IReadOnlyList<Pz5RadiusMetrics>>(classCount);

            for (int classIndex = 0; classIndex < classCount; classIndex++)
            {
                int otherClass = classIndex == 0 ? 1 : 0;
                int maxRadius = CalculateReferenceDistance(referenceVectors, classIndex, otherClass);

                _maxRadiusByClass[classIndex] = maxRadius;

                var classMetrics = new List<Pz5RadiusMetrics>(maxRadius);

                for (int radius = 1; radius <= maxRadius; radius++)
                {
                    int k1 = CountWithinRadius(skMatrix, classIndex, classIndex, radius);
                    int k2 = featureCount - k1;
                    int k3 = CountWithinRadius(skMatrix, classIndex, otherClass, radius);
                    int k4 = featureCount - k3;

                    double size = featureCount;
                    double d1 = k1 / size;
                    double alpha = k2 / size;
                    double beta = k3 / size;
                    double d2 = k4 / size;
                    bool isReliable = d1 >= ReliabilityThreshold && d2 >= ReliabilityThreshold;

                    classMetrics.Add(new Pz5RadiusMetrics(
                        radius,
                        d1,
                        alpha,
                        beta,
                        d2,
                        k1,
                        k2,
                        k3,
                        k4,
                        featureCount,
                        classIndex,
                        isReliable));
                }

                result[classIndex] = classMetrics;
            }

            return result;
        }

        private static int CountWithinRadius(int[,,] skMatrix, int classIndex, int rowIndex, int radius)
        {
            int featureCount = skMatrix.GetLength(2);
            int count = 0;

            for (int feature = 0; feature < featureCount; feature++)
            {
                if (skMatrix[classIndex, rowIndex, feature] <= radius)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CalculateReferenceDistance(int[,] referenceVectors, int classIndex, int otherClass)
        {
            int distance = 0;
            int featureCount = referenceVectors.GetLength(1);
            for (int i = 0; i < featureCount; i++)
            {
                if (referenceVectors[classIndex, i] != referenceVectors[otherClass, i])
                {
                    distance++;
                }
            }

            return distance;
        }
    }

    /// <summary>
    /// Совокупность рассчитанных значений для конкретного радиуса r.
    /// </summary>
    public sealed class Pz5RadiusMetrics
    {
        public Pz5RadiusMetrics(
            int radius,
            double d1,
            double alpha,
            double beta,
            double d2,
            double k1,
            double k2,
            double k3,
            double k4,
            int sampleSize,
            int classIndex,
            bool isReliable)
        {
            Radius = radius;
            D1 = d1;
            Alpha = alpha;
            Beta = beta;
            D2 = d2;
            K1 = k1;
            K2 = k2;
            K3 = k3;
            K4 = k4;
            SampleSize = sampleSize;
            ClassIndex = classIndex;
            IsReliable = isReliable;
        }

        public int Radius { get; }
        public double D1 { get; }
        public double Alpha { get; }
        public double Beta { get; }
        public double D2 { get; }
        public double K1 { get; }
        public double K2 { get; }
        public double K3 { get; }
        public double K4 { get; }
        public int SampleSize { get; }
        public int ClassIndex { get; }
        public bool IsReliable { get; }

        public double ShannonKfe { get; set; } = double.NaN;
        public double KullbackKfe { get; set; } = double.NaN;

        public Pz5RadiusMetrics Clone()
        {
            var copy = new Pz5RadiusMetrics(
                Radius,
                D1,
                Alpha,
                Beta,
                D2,
                K1,
                K2,
                K3,
                K4,
                SampleSize,
                ClassIndex,
                IsReliable)
            {
                ShannonKfe = this.ShannonKfe,
                KullbackKfe = this.KullbackKfe
            };

            return copy;
        }
    }
}
