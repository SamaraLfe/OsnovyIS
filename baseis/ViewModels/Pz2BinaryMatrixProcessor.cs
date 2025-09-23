using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 2. Формирование бинарной обучающей матрицы и визуализация результатов.
    /// </summary>
    public class Pz2BinaryMatrixProcessor
    {
        private readonly MainWindowViewModel _viewModel;

        public Pz2BinaryMatrixProcessor(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        // Формирование бинарной матрицы X
        public void CreateBinaryMatrix()
        {
            var Y = _viewModel.GetYMatrix();
            var X = _viewModel.GetXMatrix();
            int delta = _viewModel.GetDelta();
            var ndkMatrix = new double[2, 100];
            var vdkMatrix = new double[2, 100];

            for (int k = 0; k < 2; k++)
            {
                for (int i = 0; i < 100; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < 100; j++)
                    {
                        sum += Y[k, i, j];
                    }
                    double mean = sum / 100.0;

                    double ndk = mean - delta;
                    double vdk = mean + delta;
                    ndkMatrix[k, i] = ndk;
                    vdkMatrix[k, i] = vdk;

                    for (int j = 0; j < 100; j++)
                    {
                        int bit = (ndk <= Y[k, i, j] && Y[k, i, j] <= vdk) ? 1 : 0;
                        X[k, i, j] = bit;
                    }
                }
            }


            _viewModel.SetXMatrix(X);
            _viewModel.SetNdkMatrix(ndkMatrix);
            _viewModel.SetVdkMatrix(vdkMatrix);
        }

        // Отрисовка бинарной матрицы X
        public void VisualizeBinaryMatrix()
        {
            var X = _viewModel.GetXMatrix();
            var trainingImages = _viewModel.GetTrainingImages();

            try
            {
                int width1 = trainingImages[0].PixelSize.Width;
                int height1 = trainingImages[0].PixelSize.Height;
                int width2 = trainingImages[1].PixelSize.Width;
                int height2 = trainingImages[1].PixelSize.Height;

                var binaryImage1 = CreateBinaryImage(X, 0, width1, height1);
                var binaryImage2 = CreateBinaryImage(X, 1, width2, height2);

                _viewModel.SetBinaryMatrixVisualization1(binaryImage1);
                _viewModel.SetBinaryMatrixVisualization2(binaryImage2);
            }
            catch (System.Exception ex)
            {
                // Логируем ошибку визуализации бинарной матрицы
                System.Diagnostics.Debug.WriteLine($"Ошибка визуализации бинарной матрицы: {ex.Message}");
                return;
            }
        }


        // Отрисовка бинарной матрицы X
        private Bitmap CreateBinaryImage(int[,,] X, int classIndex, int width, int height)
        {
            using var image = new Image<Rgba32>(width, height);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var binaryColor = X[classIndex, y, x] == 1 
                        ? new Rgba32(255, 255, 255)
                        : new Rgba32(0, 0, 0);
                    
                    image[x, y] = binaryColor;
                }
            }

            using var memoryStream = new MemoryStream();
            image.SaveAsPng(memoryStream);
            memoryStream.Position = 0;
            
            return new Bitmap(memoryStream);
        }

    }
}
