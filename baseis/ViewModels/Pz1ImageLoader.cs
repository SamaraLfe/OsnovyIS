using Avalonia.Controls;
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
    /// Практическое занятие 1. Загрузка исходных изображений и подготовка сведений о матрицах яркости.
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
                var window = GetWindow();
                if (window == null)
                {
                    return;
                }

                var storageProvider = window.StorageProvider;
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
                    var bitmap = new Bitmap(stream);

                    if (imageIndex == 0)
                    {
                        // Нормируем изображение к 100x100
                        using var memStream = new MemoryStream();
                        bitmap.Save(memStream);
                        memStream.Position = 0;
                        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(memStream);
                        using var normalized = image.Clone(ctx => ctx.Resize(100, 100));
                        
                        // Создаем новый Bitmap из нормированного изображения
                        using var normalizedStream = new MemoryStream();
                        normalized.SaveAsPng(normalizedStream);
                        normalizedStream.Position = 0;
                        var normalizedBitmap = new Bitmap(normalizedStream);
                        
                        _viewModel.SourceImage1 = normalizedBitmap;
                        _viewModel.GetTrainingImages().Clear();
                        _viewModel.GetTrainingImages().Add(normalizedBitmap);

                        _viewModel.CanLoadFirst = false;
                        _viewModel.CanLoadSecond = true;
                        _viewModel.CanClear = true;
                    }
                    else
                    {
                        // Нормируем изображение к 100x100
                        using var memStream = new MemoryStream();
                        bitmap.Save(memStream);
                        memStream.Position = 0;
                        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(memStream);
                        using var normalized = image.Clone(ctx => ctx.Resize(100, 100));
                        
                        // Создаем новый Bitmap из нормированного изображения
                        using var normalizedStream = new MemoryStream();
                        normalized.SaveAsPng(normalizedStream);
                        normalizedStream.Position = 0;
                        var normalizedBitmap = new Bitmap(normalizedStream);
                        
                        _viewModel.SourceImage2 = normalizedBitmap;
                        _viewModel.GetTrainingImages().Add(normalizedBitmap);

                        _viewModel.CanLoadSecond = false;
                        _viewModel.CanCalculate = true;
                        _viewModel.CanClear = true;
                    }
                }
            }
            catch (System.Exception)
            {
                // Ошибка обрабатывается автоматически
            }
        }

        private Window? GetWindow()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }
    }
}
