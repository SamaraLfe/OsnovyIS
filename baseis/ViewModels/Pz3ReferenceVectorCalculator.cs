using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 3. Определение геометрических центров классов (эталонных векторов).
    /// </summary>
    public class Pz3ReferenceVectorCalculator
    {
        private readonly MainWindowViewModel _viewModel;

        public Pz3ReferenceVectorCalculator(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Compute()
        {
            var X = _viewModel.GetXMatrix();
            var selec = _viewModel.GetSelec();

            var avg = new double[2, 100];
            var xm = new int[2, 100];

            for (int k = 0; k < 2; k++)
            {
                for (int i = 0; i < 100; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < 100; j++)
                    {
                        sum += X[k, i, j];
                    }
                    avg[k, i] = sum / 100.0;
                    xm[k, i] = avg[k, i] > selec ? 1 : 0;
                }
            }

            _viewModel.SetAvgMatrix(avg);
            _viewModel.SetXmMatrix(xm);
            _viewModel.ReferenceVectors = MatrixFormatter.GetReferenceVectorsString(xm);
        }

        // Отрисовка эталонного вектора
        public void VisualizeReferenceVectors()
        {
            var xm = _viewModel.GetXmMatrix();
            var trainingImages = _viewModel.GetTrainingImages();

            if (trainingImages.Count < 2)
            {
                return;
            }

            try
            {
                int width1 = trainingImages[0].PixelSize.Width;
                int height1 = trainingImages[0].PixelSize.Height;
                int width2 = trainingImages[1].PixelSize.Width;
                int height2 = trainingImages[1].PixelSize.Height;

                var referenceImage1 = CreateReferenceImage(xm, 0, width1, height1);
                var referenceImage2 = CreateReferenceImage(xm, 1, width2, height2);

                _viewModel.SetReferenceVectorsVisualization1(referenceImage1);
                _viewModel.SetReferenceVectorsVisualization2(referenceImage2);
            }
            catch (System.Exception ex)
            {
                // Логируем ошибку визуализации эталонных векторов
                System.Diagnostics.Debug.WriteLine($"Ошибка визуализации эталонных векторов: {ex.Message}");
                return;
            }
        }

        // Отрисовка эталонного вектора
        private Bitmap CreateReferenceImage(int[,] xm, int classIndex, int width, int height)
        {
            using var image = new Image<Rgba32>(1, height);
            
            for (int y = 0; y < height; y++)
            {
                var referenceColor = xm[classIndex, y] == 1
                    ? new Rgba32(255, 255, 255)
                    : new Rgba32(0, 0, 0);
                
                image[0, y] = referenceColor;
            }

            using var memoryStream = new MemoryStream();
            image.SaveAsPng(memoryStream);
            memoryStream.Position = 0;
            
            return new Bitmap(memoryStream);
        }
    }
}
