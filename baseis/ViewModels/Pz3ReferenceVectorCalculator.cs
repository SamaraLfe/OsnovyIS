using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System;

namespace baseis.ViewModels
{
    // Формирование эталонного вектора EV геометрических центров классов распознавания
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
                    double average = sum / 100;
                    xm[k, i] = average > 0.5 ? 1 : 0;
                }
            }

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
            catch (System.Exception)
            {
                return;
            }
        }

        // Отрисовка эталонного вектора
        private Bitmap CreateReferenceImage(int[,] xm, int classIndex, int width, int height)
        {
            using var image = new Image<Rgba32>(1, height);
            
            for (int y = 0; y < height; y++)
            {
                // Прямое соответствие: верх изображения = начало вектора
                int scaledY = (int)((double)y / height * 100);
                scaledY = Math.Min(scaledY, 99);

                var referenceColor = xm[classIndex, scaledY] == 1
                    ? new Rgba32(255, 255, 255)
                    : new Rgba32(0, 0, 0);
                
                image[0, y] = referenceColor;
            }

            using var scaledImage = image.Clone(ctx => ctx.Resize(width, height));
            using var memoryStream = new MemoryStream();
            scaledImage.SaveAsPng(memoryStream);
            memoryStream.Position = 0;
            
            return new Bitmap(memoryStream);
        }
    }
}
