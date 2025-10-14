using System.Text;

namespace baseis.ViewModels
{
    /// <summary>
    /// Утилита форматирования матриц и показа окна деталей.
    /// Содержит методы для преобразования матриц Y, X и xm в удобочитаемые строки.
    /// </summary>
    public class MatrixFormatter
    {
        /// <summary>
        /// Открывает окно с произвольным текстовым содержимым.
        /// </summary>
        public void ShowDetailsWindow(string title, string content)
        {
            var detailsWindow = new Views.DetailsWindow
            {
                DataContext = new DetailsViewModel
                {
                    Title = title,
                    Content = content
                }
            };

            detailsWindow.Show();
        }

        /// <summary>
        /// Формирует текст для обучающей матрицы Y по заданному классу.
        /// </summary>
        public static string GetLearningMatrixString(double[,,] matrix, int classIndex)
        {
            int rows = matrix.GetLength(1);
            int cols = matrix.GetLength(2);
            var sb = new StringBuilder();

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int value = (int)System.Math.Round(matrix[classIndex, y, x]);
                    sb.Append(value);
                    sb.Append(' ');
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Формирует текст для бинарной матрицы X по заданному классу, 
        /// добавляя для каждой строки соответствующие NDK/VDK/AVG.
        /// </summary>
        public static string GetBinaryMatrixString(int[,,] matrix, double[,] ndk, double[,] vdk, double[,] avg, int classIndex)
        {
            int rows = matrix.GetLength(1);
            int cols = matrix.GetLength(2);
            var sb = new StringBuilder();

            for (int feature = 0; feature < rows; feature++)
            {
                for (int realization = 0; realization < cols; realization++)
                {
                    sb.Append(matrix[classIndex, feature, realization]);
                    sb.Append(' ');
                }

                sb.Append(" | NDK=");
                sb.Append((int)System.Math.Round(ndk[classIndex, feature]));
                sb.Append(" VDK=");
                sb.Append((int)System.Math.Round(vdk[classIndex, feature]));
                sb.Append(" AVG=");
                sb.Append(avg[classIndex, feature].ToString("F1"));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Формирует текст для матрицы допусков (NDK/VDK/AVG) указанного класса.
        /// </summary>
        public static string GetToleranceMatrixString(double[,] ndk, double[,] vdk, double[,] avg, int classIndex)
        {
            if (ndk == null || vdk == null || avg == null)
            {
                return string.Empty;
            }

            if (classIndex < 0 || classIndex >= ndk.GetLength(0))
            {
                return string.Empty;
            }

            int featureCount = ndk.GetLength(1);
            var sb = new StringBuilder();

            for (int feature = 0; feature < featureCount; feature++)
            {
                sb.Append("NDK=");
                sb.Append(ndk[classIndex, feature].ToString("F1"));
                sb.Append("  VDK=");
                sb.Append(vdk[classIndex, feature].ToString("F1"));
                sb.Append("  AVG=");
                sb.Append(avg[classIndex, feature].ToString("F2"));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Формирует текст для эталонного вектора (xm) указанного класса
        /// или всех классов, если класс не задан.
        /// </summary>
        public static string GetReferenceVectorString(int[,] referenceVectors, int? classIndex = null)
        {
            int vectorLength = referenceVectors.GetLength(1);
            if (vectorLength == 0)
            {
                return string.Empty;
            }

            int[] indices;
            if (classIndex.HasValue)
            {
                indices = new[] { classIndex.Value };
            }
            else
            {
                int classCount = referenceVectors.GetLength(0);
                if (classCount == 0)
                {
                    return string.Empty;
                }

                indices = new int[classCount];
                for (int i = 0; i < classCount; i++)
                {
                    indices[i] = i;
                }
            }

            var sb = new StringBuilder();

            for (int index = 0; index < indices.Length; index++)
            {
                int currentClass = indices[index];

                for (int i = 0; i < vectorLength; i++)
                {
                    sb.AppendLine(referenceVectors[currentClass, i].ToString());
                }

                if (index < indices.Length - 1)
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Формирует текстовое представление матрицы расстояний SK для выбранного класса.
        /// </summary>
        public static string GetDistanceMatrixString(int[,,] skMatrix, int classIndex)
        {
            if (skMatrix == null || skMatrix.Length == 0)
            {
                return string.Empty;
            }

            if (classIndex < 0 || classIndex >= skMatrix.GetLength(0))
            {
                return string.Empty;
            }

            int totalRealizations = skMatrix.GetLength(2);
            if (totalRealizations == 0)
            {
                return string.Empty;
            }

            int classCount = skMatrix.GetLength(1);
            var sb = new StringBuilder();

            sb.AppendLine($"Расстояния до признаков своего класса K={classIndex}:");
            AppendCodeDistanceRow(sb, skMatrix, classIndex, classIndex, totalRealizations);

            for (int neighborIndex = 0; neighborIndex < classCount; neighborIndex++)
            {
                if (neighborIndex == classIndex)
                {
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine($"Расстояния до признаков класса K={neighborIndex}:");
                AppendCodeDistanceRow(sb, skMatrix, classIndex, neighborIndex, totalRealizations);
            }

            return sb.ToString();
        }

        private static void AppendCodeDistanceRow(StringBuilder sb, int[,,] matrix, int classIndex, int rowIndex, int totalCount)
        {
            for (int realization = 0; realization < totalCount; realization++)
            {
                sb.Append(matrix[classIndex, rowIndex, realization]);

                if (realization < totalCount - 1)
                {
                    sb.Append(' ');
                }
            }

            sb.AppendLine();
        }
    }
}
