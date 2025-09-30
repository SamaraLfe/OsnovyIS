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
        /// Показывает квадрат size x size (по умолчанию 100x100).
        /// </summary>
        public static string GetLearningMatrixString(double[,,] matrix, int classIndex, int size = 100)
        {
            int limit = System.Math.Clamp(size, 1, 100);
            var sb = new StringBuilder();

            for (int y = 0; y < limit; y++)
            {
                for (int x = 0; x < limit; x++)
                {
                    int value = (int)System.Math.Round(matrix[classIndex, y, x]);
                    sb.Append(value);
                    sb.Append(' ');
                }
                if (limit < 100)
                {
                    sb.Append("...");
                }
                sb.AppendLine();
            }

            if (limit < 100)
            {
                sb.AppendLine("...");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Формирует текст для бинарной матрицы X по заданному классу, 
        /// добавляя для каждой строки соответствующие NDK/VDK/AVG.
        /// </summary>
        public static string GetBinaryMatrixString(int[,,] matrix, double[,] ndk, double[,] vdk, double[,] avg, int classIndex, int size = 100)
        {
            int limit = System.Math.Clamp(size, 1, 100);
            var sb = new StringBuilder();

            for (int feature = 0; feature < limit; feature++)
            {
                for (int realization = 0; realization < limit; realization++)
                {
                    sb.Append(matrix[classIndex, feature, realization]);
                    sb.Append(' ');
                }

                if (limit < 100)
                {
                    sb.Append("... ");
                }

                sb.Append(" | NDK=");
                sb.Append((int)System.Math.Round(ndk[classIndex, feature]));
                sb.Append(" VDK=");
                sb.Append((int)System.Math.Round(vdk[classIndex, feature]));
                sb.Append(" AVG=");
                sb.Append(avg[classIndex, feature].ToString("F1"));
                sb.AppendLine();
            }

            if (limit < 100)
            {
                sb.AppendLine("...");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Формирует текст для эталонного вектора (xm) указанного класса
        /// или всех классов, если класс не задан.
        /// </summary>
        public static string GetReferenceVectorString(int[,] referenceVectors, int? classIndex = null, int size = 100)
        {
            int vectorLength = referenceVectors.GetLength(1);
            if (vectorLength == 0)
            {
                return string.Empty;
            }

            int limit = System.Math.Clamp(size, 1, vectorLength);

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

                for (int i = 0; i < limit; i++)
                {
                    sb.AppendLine(referenceVectors[currentClass, i].ToString());
                }

                if (limit < vectorLength)
                {
                    sb.AppendLine("...");
                }

                if (index < indices.Length - 1)
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
