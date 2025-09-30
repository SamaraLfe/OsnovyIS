using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 1. Подготовка входной обучающей матрицы Y из нормализованных изображений.
    /// </summary>
    public class Pz1TrainingMatrixBuilder
    {
        private readonly MainWindowViewModel _viewModel;

        public Pz1TrainingMatrixBuilder(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Build()
        {
            var Y = _viewModel.GetYMatrix();
            var classCount = Y.GetLength(0);
            var featureCount = Y.GetLength(1);
            var realizationCount = Y.GetLength(2);

            // Заполняем матрицу Y из загруженных изображений
            for (int k = 0; k < _viewModel.GetTrainingImages().Count && k < classCount; k++)
            {
                using var stream = new MemoryStream();
                _viewModel.GetTrainingImages()[k].Save(stream);
                stream.Position = 0;

                using var image = Image.Load<Rgba32>(stream);
                
                for (int y = 0; y < featureCount; y++)
                {
                    for (int x = 0; x < realizationCount; x++)
                    {
                        var pixel = image[x, y];
                        Y[k, y, x] = (pixel.R + pixel.G + pixel.B) / 3.0;
                    }
                }
            }
        }
    }
}
