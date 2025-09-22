using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Collections.Generic;
using System;

namespace baseis.ViewModels
{
    /// <summary>
    /// Главная ViewModel приложения для системы распознавания образов.
    /// 
    /// Управляет процессом обучения системы распознавания:
    /// ПЗ1: Загрузка изображений и формирование обучающей матрицы Y
    /// ПЗ2: Создание бинарной матрицы X и её визуализация
    /// ПЗ3: Вычисление эталонных векторов и их отображение
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        #region Константы
        private const int m = 2;        // Количество классов
        private const int N = 100;      // Размер по вертикали (строки)
        private const int n = 100;      // Размер по горизонтали (столбцы)
        private const int delta = 50;   // Порог для бинаризации
        #endregion


        #region Свойства интерфейса - Матрицы для отображения
        [ObservableProperty] private string learningMatrixClass0 = "";
        [ObservableProperty] private string learningMatrixClass1 = "";
        [ObservableProperty] private string binaryMatrixClass0 = "";
        [ObservableProperty] private string binaryMatrixClass1 = "";
        [ObservableProperty] private string referenceVectorClass0 = "";
        [ObservableProperty] private string referenceVectorClass1 = "";
        [ObservableProperty] private string referenceVectors = "";
        #endregion

        #region Свойства интерфейса - Изображения
        [ObservableProperty] private Bitmap? sourceImage1;
        [ObservableProperty] private Bitmap? sourceImage2;
        [ObservableProperty] private Bitmap? binaryMatrixVisualization1;
        [ObservableProperty] private Bitmap? binaryMatrixVisualization2;
        [ObservableProperty] private Bitmap? referenceVectorsVisualization1;
        [ObservableProperty] private Bitmap? referenceVectorsVisualization2;
        #endregion

        #region Свойства интерфейса - Состояние кнопок
        [ObservableProperty] private bool canLoadFirst = true;
        [ObservableProperty] private bool canLoadSecond = false;
        [ObservableProperty] private bool canCalculate = false;
        [ObservableProperty] private bool canShowDetails = false;
        [ObservableProperty] private bool canClear = false;
        [ObservableProperty] private bool canShowBinaryMatrix = false;
        [ObservableProperty] private double matrixFontSize = 10;
        #endregion

        #region Данные матриц
        private double[,,] Y = new double[m, N, n];           // Обучающая матрица Y[2,100,100]
        private int[,,] X = new int[m, N, n];                 // Бинарная матрица X[2,100,100]
        private int[,] xm = new int[m, N];                    // Эталонные векторы xm[2,100]
        private double[,] _ndkMatrix = new double[m, N];      // Нижние границы NDK[2,100]
        private double[,] _vdkMatrix = new double[m, N];      // Верхние границы VDK[2,100]
        private double[,] _avgMatrix = new double[m, N];      // Средние значения AVG[2,100]
        private List<Bitmap> _trainingImages = new List<Bitmap>();
        #endregion

        #region Сервисы
        private readonly Pz1ImageLoader _imageLoader;
        private readonly Pz1TrainingMatrixBuilder _trainingMatrixBuilder;
        private readonly Pz2BinaryMatrixProcessor _binaryMatrixProcessor;
        private readonly Pz3ReferenceVectorCalculator _referenceVectorCalculator;
        private readonly MatrixFormatter _matrixFormatter;
        #endregion

        #region Команды
        private RelayCommand? _calculateCommand;
        private RelayCommand? _loadFirstCommand;
        private RelayCommand? _loadSecondCommand;
        private RelayCommand? _clearCommand;
        private RelayCommand? _showSourceDetailsCommand;
        private RelayCommand? _showSecondImageDetailsCommand;
        private RelayCommand? _showTrainingDetailsClass0Command;
        private RelayCommand? _showTrainingDetailsClass1Command;
        private RelayCommand? _showBinaryDetailsClass0Command;
        private RelayCommand? _showBinaryDetailsClass1Command;
        private RelayCommand? _showReferenceDetailsClass0Command;
        private RelayCommand? _showReferenceDetailsClass1Command;
        private RelayCommand? _visualizeBinaryMatrixCommand;
        #endregion

        #region Конструктор
        public MainWindowViewModel()
        {
            // Инициализация сервисов
            _imageLoader = new Pz1ImageLoader(this);
            _trainingMatrixBuilder = new Pz1TrainingMatrixBuilder(this);
            _binaryMatrixProcessor = new Pz2BinaryMatrixProcessor(this);
            _referenceVectorCalculator = new Pz3ReferenceVectorCalculator(this);
            _matrixFormatter = new MatrixFormatter(this);

            // Инициализация команд
            InitializeCommands();
        }
        #endregion

        #region Публичные свойства команд
        public IRelayCommand LoadFirstCommand => _loadFirstCommand!;
        public IRelayCommand LoadSecondCommand => _loadSecondCommand!;
        public IRelayCommand CalculateCommand => _calculateCommand!;
        public IRelayCommand ClearCommand => _clearCommand!;
        public IRelayCommand ShowSourceDetailsCommand => _showSourceDetailsCommand!;
        public IRelayCommand ShowSecondImageDetailsCommand => _showSecondImageDetailsCommand!;
        public IRelayCommand ShowTrainingDetailsClass0Command => _showTrainingDetailsClass0Command!;
        public IRelayCommand ShowTrainingDetailsClass1Command => _showTrainingDetailsClass1Command!;
        public IRelayCommand ShowBinaryDetailsClass0Command => _showBinaryDetailsClass0Command!;
        public IRelayCommand ShowBinaryDetailsClass1Command => _showBinaryDetailsClass1Command!;
        public IRelayCommand ShowReferenceDetailsClass0Command => _showReferenceDetailsClass0Command!;
        public IRelayCommand ShowReferenceDetailsClass1Command => _showReferenceDetailsClass1Command!;
        public IRelayCommand VisualizeBinaryMatrixCommand => _visualizeBinaryMatrixCommand!;
        #endregion

        #region Публичные методы доступа к данным
        public double[,,] GetYMatrix() => Y;
        public int[,,] GetXMatrix() => X;
        public int[,] GetXmMatrix() => xm;
        public double[,] GetNdkMatrix() => _ndkMatrix;
        public double[,] GetVdkMatrix() => _vdkMatrix;
        public double[,] GetAvgMatrix() => _avgMatrix;
        public List<Bitmap> GetTrainingImages() => _trainingImages;
        public int GetDelta() => delta;
        #endregion

        #region Публичные методы установки данных
        public void SetYMatrix(double[,,] value) => Y = value;
        public void SetXMatrix(int[,,] value) => X = value;
        public void SetXmMatrix(int[,] value) => xm = value;
        public void SetNdkMatrix(double[,] value) => _ndkMatrix = value;
        public void SetVdkMatrix(double[,] value) => _vdkMatrix = value;
        public void SetAvgMatrix(double[,] value) => _avgMatrix = value;
        public void SetBinaryMatrixVisualization1(Bitmap? value) => BinaryMatrixVisualization1 = value;
        public void SetBinaryMatrixVisualization2(Bitmap? value) => BinaryMatrixVisualization2 = value;
        public void SetReferenceVectorsVisualization1(Bitmap? value) => ReferenceVectorsVisualization1 = value;
        public void SetReferenceVectorsVisualization2(Bitmap? value) => ReferenceVectorsVisualization2 = value;
        public void SetCanShowBinaryMatrix(bool value) => CanShowBinaryMatrix = value;
        #endregion

        #region Обработчики изменений свойств
        partial void OnCanLoadFirstChanged(bool value) => _loadFirstCommand?.NotifyCanExecuteChanged();
        partial void OnCanLoadSecondChanged(bool value) => _loadSecondCommand?.NotifyCanExecuteChanged();
        partial void OnCanCalculateChanged(bool value) => _calculateCommand?.NotifyCanExecuteChanged();
        partial void OnCanClearChanged(bool value) => _clearCommand?.NotifyCanExecuteChanged();
        partial void OnSourceImage1Changed(Bitmap? value) => _showSourceDetailsCommand?.NotifyCanExecuteChanged();
        partial void OnSourceImage2Changed(Bitmap? value) => _showSecondImageDetailsCommand?.NotifyCanExecuteChanged();
        partial void OnCanShowDetailsChanged(bool value)
        {
            _showTrainingDetailsClass0Command?.NotifyCanExecuteChanged();
            _showTrainingDetailsClass1Command?.NotifyCanExecuteChanged();
            _showBinaryDetailsClass0Command?.NotifyCanExecuteChanged();
            _showBinaryDetailsClass1Command?.NotifyCanExecuteChanged();
            _showReferenceDetailsClass0Command?.NotifyCanExecuteChanged();
            _showReferenceDetailsClass1Command?.NotifyCanExecuteChanged();
        }
        partial void OnCanShowBinaryMatrixChanged(bool value) => _visualizeBinaryMatrixCommand?.NotifyCanExecuteChanged();
        #endregion

        #region Приватные методы
        /// <summary>
        /// Инициализирует все команды приложения
        /// </summary>
        private void InitializeCommands()
        {
            _loadFirstCommand = new RelayCommand(LoadFirstImage, () => CanLoadFirst);
            _loadSecondCommand = new RelayCommand(LoadSecondImage, () => CanLoadSecond);
            _calculateCommand = new RelayCommand(Calculate, () => CanCalculate);
            _clearCommand = new RelayCommand(ClearAll, () => CanClear);
            _showSourceDetailsCommand = new RelayCommand(ShowSourceDetails, () => SourceImage1 != null);
            _showSecondImageDetailsCommand = new RelayCommand(ShowSecondImageDetails, () => SourceImage2 != null);
            _showTrainingDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Обучающая матрица Y"), () => CanShowDetails);
            _showTrainingDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Обучающая матрица Y"), () => CanShowDetails);
            _showBinaryDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Бинарная матрица X"), () => CanShowDetails);
            _showBinaryDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Бинарная матрица X"), () => CanShowDetails);
            _showReferenceDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Эталонный вектор"), () => CanShowDetails);
            _showReferenceDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Эталонный вектор"), () => CanShowDetails);
            _visualizeBinaryMatrixCommand = new RelayCommand(VisualizeBinaryMatrix, () => CanShowBinaryMatrix);
        }

        /// <summary>
        /// Универсальный метод для отображения деталей матриц
        /// </summary>
        private void ShowDetails(int classIndex, string matrixType)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{matrixType.ToUpper()} - КЛАСС {classIndex}");
            sb.AppendLine(new string('=', matrixType.Length + 15));
            sb.AppendLine();

            switch (matrixType)
            {
                case "Обучающая матрица Y":
                    sb.AppendLine(MatrixFormatter.GetLearningMatrixString(GetYMatrix(), classIndex, 100));
                    break;
                case "Бинарная матрица X":
                    sb.AppendLine(MatrixFormatter.GetBinaryMatrixString(GetXMatrix(), GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), classIndex, 100));
                    break;
                case "Эталонный вектор":
                    sb.AppendLine(MatrixFormatter.GetReferenceVectorString(GetXmMatrix(), classIndex, 100));
                    break;
            }

            _matrixFormatter.ShowDetailsWindow($"{matrixType} - Класс {classIndex}", sb.ToString());
        }

        /// <summary>
        /// Показывает детали первого изображения
        /// </summary>
        private void ShowSourceDetails()
        {
            ShowImageDetails(0, "ПЕРВОЕ ИЗОБРАЖЕНИЕ");
        }

        /// <summary>
        /// Показывает детали второго изображения
        /// </summary>
        private void ShowSecondImageDetails()
        {
            ShowImageDetails(1, "ВТОРОЕ ИЗОБРАЖЕНИЕ");
        }

        /// <summary>
        /// Универсальный метод для отображения деталей изображений
        /// </summary>
        private void ShowImageDetails(int imageIndex, string title)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"МАТРИЦА {title}");
            sb.AppendLine(new string('=', title.Length + 8));
            sb.AppendLine();

            if (GetTrainingImages().Count > imageIndex)
            {
                using var stream = new System.IO.MemoryStream();
                GetTrainingImages()[imageIndex].Save(stream);
                stream.Position = 0;
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
                sb.AppendLine(MatrixFormatter.GetFullPixelMatrix(image, $"{title} (нормировано 100x100)"));
            }
            else
            {
                sb.AppendLine("Изображение не загружено");
            }

            _matrixFormatter.ShowDetailsWindow($"Матрица {title.ToLower()}", sb.ToString());
        }

        /// <summary>
        /// Очищает все данные и сбрасывает состояние интерфейса
        /// </summary>
        private void ClearAll()
        {
            // Очистка изображений
            SourceImage1 = null;
            SourceImage2 = null;
            BinaryMatrixVisualization1 = null;
            BinaryMatrixVisualization2 = null;
            ReferenceVectorsVisualization1 = null;
            ReferenceVectorsVisualization2 = null;
            _trainingImages.Clear();

            // Очистка текстовых полей
            ReferenceVectors = "";
            LearningMatrixClass0 = "";
            LearningMatrixClass1 = "";
            BinaryMatrixClass0 = "";
            BinaryMatrixClass1 = "";
            ReferenceVectorClass0 = "";
            ReferenceVectorClass1 = "";

            // Сброс матриц
            _ndkMatrix = new double[m, N];
            _vdkMatrix = new double[m, N];
            _avgMatrix = new double[m, N];

            // Сброс состояния кнопок
            CanLoadFirst = true;
            CanLoadSecond = false;
            CanCalculate = false;
            CanShowDetails = false;
            CanShowBinaryMatrix = false;
            CanClear = false;
        }

        /// <summary>
        /// Сбрасывает только вычисленные данные при загрузке новых изображений
        /// </summary>
        private void ResetCalculatedData()
        {
            LearningMatrixClass0 = "";
            LearningMatrixClass1 = "";
            BinaryMatrixClass0 = "";
            BinaryMatrixClass1 = "";
            ReferenceVectorClass0 = "";
            ReferenceVectorClass1 = "";
            BinaryMatrixVisualization1 = null;
            BinaryMatrixVisualization2 = null;
            ReferenceVectorsVisualization1 = null;
            ReferenceVectorsVisualization2 = null;
            CanShowDetails = false;
            CanShowBinaryMatrix = false;
            ReferenceVectors = "";
        }

        /// <summary>
        /// Загружает первое изображение
        /// </summary>
        private async void LoadFirstImage()
        {
            await _imageLoader.LoadImageAsync(0);
            ResetCalculatedData();
        }

        /// <summary>
        /// Загружает второе изображение
        /// </summary>
        private async void LoadSecondImage()
        {
            await _imageLoader.LoadImageAsync(1);
            ResetCalculatedData();
        }

        /// <summary>
        /// Выполняет полный цикл вычислений для системы распознавания
        /// </summary>
        private void Calculate()
        {
            if (GetTrainingImages().Count < 2)
            {
                return;
            }

            try
            {
                // ПЗ1: Формирование обучающей матрицы Y
                _trainingMatrixBuilder.Build();

                // ПЗ2: Формирование бинарной матрицы X
                _binaryMatrixProcessor.CreateBinaryMatrix();

                // ПЗ3: Формирование эталонного вектора EV
                _referenceVectorCalculator.Compute();

                // Обновление отображения матриц
                UpdateMatrixDisplay();

                // Визуализация результатов
                VisualizeBinaryMatrix();
                _referenceVectorCalculator.VisualizeReferenceVectors();

                // Обновление состояния интерфейса
                CanShowDetails = true;
                CanClear = true;
            }
            catch (Exception)
            {
                // Ошибка обрабатывается автоматически
            }
        }

        /// <summary>
        /// Обновляет отображение всех матриц в интерфейсе
        /// </summary>
        private void UpdateMatrixDisplay()
        {
            LearningMatrixClass0 = MatrixFormatter.GetLearningMatrixString(GetYMatrix(), 0, 20);
            LearningMatrixClass1 = MatrixFormatter.GetLearningMatrixString(GetYMatrix(), 1, 20);
            BinaryMatrixClass0 = MatrixFormatter.GetBinaryMatrixString(GetXMatrix(), GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), 0, 20);
            BinaryMatrixClass1 = MatrixFormatter.GetBinaryMatrixString(GetXMatrix(), GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), 1, 20);
            ReferenceVectorClass0 = MatrixFormatter.GetReferenceVectorString(GetXmMatrix(), 0, 100);
            ReferenceVectorClass1 = MatrixFormatter.GetReferenceVectorString(GetXmMatrix(), 1, 100);
            ReferenceVectors = MatrixFormatter.GetReferenceVectorsString(GetXmMatrix());
        }

        /// <summary>
        /// Запускает визуализацию бинарных матриц
        /// </summary>
        private void VisualizeBinaryMatrix()
        {
            _binaryMatrixProcessor.VisualizeBinaryMatrix();
        }
        #endregion
    }
}