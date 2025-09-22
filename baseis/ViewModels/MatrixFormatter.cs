using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace baseis.ViewModels
{
    public class MatrixFormatter
    {
        private readonly MainWindowViewModel _viewModel;

        public MatrixFormatter(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

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

        // Формирование обучающей матрицы Y
        public static string GetLearningMatrixString(double[,,] matrix, int classIndex, int size = 100)
        {
            int limit = System.Math.Clamp(size, 1, 100);
            var sb = new StringBuilder();

            for (int y = 0; y < limit; y++)
            {
                for (int x = 0; x < limit; x++)
                {
                    int value = (int)System.Math.Round(matrix[classIndex, y, x]);
                    sb.AppendFormat("{0,4}", value);
                }
                if (limit < 100)
                {
                    sb.Append("   ...");
                }
                sb.AppendLine();
            }

            if (limit < 100)
            {
                sb.AppendLine("...");
            }

            return sb.ToString();
        }

        // Формирование бинарной матрицы X
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
                sb.Append(((int)System.Math.Round(ndk[classIndex, feature])).ToString());
                sb.Append(" VDK=");
                sb.Append(((int)System.Math.Round(vdk[classIndex, feature])).ToString());
                sb.Append(" AVG=");
                sb.Append(avg[classIndex, feature].ToString("F2"));
                sb.AppendLine();
            }

            if (limit < 100)
            {
                sb.AppendLine("...");
            }

            return sb.ToString();
        }

        public static string GetReferenceVectorString(int[,] referenceVectors, int classIndex, int size = 100)
        {
            int limit = System.Math.Clamp(size, 1, 100);
            var sb = new StringBuilder();

            for (int i = 0; i < limit; i++)
            {
                sb.AppendLine(referenceVectors[classIndex, i].ToString());
            }

            if (limit < 100)
            {
                sb.AppendLine("...");
            }

            return sb.ToString();
        }

        // Формирование эталонного вектора EV геометрических центров классов распознавания
        public static string GetReferenceVectorsString(int[,] referenceVectors)
        {
            var sb = new StringBuilder();
            for (int k = 0; k < 2; k++)
            {
                sb.AppendLine(GetReferenceVectorString(referenceVectors, k, 100));
                if (k < 1)
                {
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }


        public static string GetPixelMatrixPreview(Image<Rgba32> image, string title, int size = 20)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(title))
            {
                sb.AppendLine(title);
            }
            AppendImageMatrix(sb, image, size);
            return sb.ToString();
        }

        public static string GetFullPixelMatrix(Image<Rgba32> image, string title)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(title))
            {
                sb.AppendLine(title);
            }
            AppendImageMatrix(sb, image, 100);
            return sb.ToString();
        }

        private static void AppendImageMatrix(StringBuilder sb, Image<Rgba32> image, int size)
        {
            int limit = System.Math.Clamp(size, 1, 100);
            for (int y = 0; y < limit; y++)
            {
                for (int x = 0; x < limit; x++)
                {
                    var pixel = image[x, y];
                    int grayValue = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                    sb.AppendFormat("{0,4}", grayValue);
                }
                if (limit < 100)
                {
                    sb.Append("   ...");
                }
                sb.AppendLine();
            }
            if (limit < 100)
            {
                sb.AppendLine("...");
            }
        }
    }
}
