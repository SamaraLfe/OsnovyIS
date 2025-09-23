using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Threading.Tasks;

namespace baseis.ViewModels
{
    // Загрузка изображения
    /// <summary>
    /// Практическое занятие 1. Загрузка исходных изображений.
    /// </summary>
    public class Pz1ImageLoader
    {
        private readonly MainWindowViewModel _viewModel;

        public Pz1ImageLoader(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public async Task LoadImageAsync(int imageIndex)
        {
            try
            {
                var storageProvider = GetWindow()?.StorageProvider;
                if (storageProvider == null) return;
                var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = imageIndex == 0 ? "Выберите первое изображение" : "Выберите второе изображение",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Изображения")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
                        }
                    }
                });

                if (files != null && files.Count > 0)
                {
                    using var stream = await files[0].OpenReadAsync();
                    using var bitmap = new Bitmap(stream);

                    // Нормируем изображение к размерам матриц (n x N)
                    int targetWidth = _viewModel.Getn();   // столбцы
                    int targetHeight = _viewModel.GetN();  // строки
                    var normalizedBitmap = NormalizeBitmap(bitmap, targetWidth, targetHeight);

                    if (imageIndex == 0)
                    {
                        _viewModel.SourceImage1 = normalizedBitmap;
                        _viewModel.GetTrainingImages().Clear();
                        _viewModel.GetTrainingImages().Add(normalizedBitmap);

                        _viewModel.CanLoadFirst = false;
                        _viewModel.CanLoadSecond = true;
                        _viewModel.CanClear = true;
                    }
                    else
                    {
                        _viewModel.SourceImage2 = normalizedBitmap;
                        _viewModel.GetTrainingImages().Add(normalizedBitmap);

                        _viewModel.CanLoadSecond = false;
                        _viewModel.CanCalculate = true;
                        _viewModel.CanClear = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Логируем ошибку загрузки изображения
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения {imageIndex}: {ex.Message}");
            }
        }

        private static Avalonia.Controls.Window? GetWindow()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }

        private static Bitmap NormalizeBitmap(Bitmap source, int width, int height)
        {
            using var memStream = new MemoryStream();
            source.Save(memStream);
            memStream.Position = 0;
            using var image = Image.Load<Rgba32>(memStream);
            using var normalized = image.Clone(ctx => ctx.Resize(width, height));

            using var normalizedStream = new MemoryStream();
            normalized.SaveAsPng(normalizedStream);
            normalizedStream.Position = 0;
            return new Bitmap(normalizedStream);
        }
    }
}
