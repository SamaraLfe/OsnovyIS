using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System;

namespace baseis.ViewModels
{
    /// <summary>
    /// Главная ViewModel приложения.
    /// 
    /// ПЗ1: Загрузка изображений и формирование обучающей матрицы Y
    /// ПЗ2: Создание бинарной матрицы X и её визуализация
    /// ПЗ3: Вычисление эталонных векторов и их отображение
    /// ПЗ4: Формирование массива кодовых расстояний
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        #region Константы
        private const int m = 2;        // Количество классов
        private const int N = 100;      // количество признаков распознавания (строки)
        private const int n = 100;      //  количество реализаций (столбцы)
        private const int delta = 50;   // контрольный допуск
        private const double selec = 0.63; // уровень селекции
        #endregion


        #region Свойства интерфейса - Матрицы для отображения
        [ObservableProperty] private string learningMatrixClass0 = "";
        [ObservableProperty] private string learningMatrixClass1 = "";
        [ObservableProperty] private string binaryMatrixClass0 = "";
        [ObservableProperty] private string binaryMatrixClass1 = "";
        [ObservableProperty] private string referenceVectorClass0 = "";
        [ObservableProperty] private string referenceVectorClass1 = "";
        [ObservableProperty] private string referenceVectors = "";
        [ObservableProperty] private string distanceMatrixClass0 = "";
        [ObservableProperty] private string distanceMatrixClass1 = "";
        [ObservableProperty] private string toleranceMatrixClass0 = "";
        [ObservableProperty] private string toleranceMatrixClass1 = "";
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
        [ObservableProperty] private double matrixFontSize = 10;
        #endregion

        #region Данные матриц
        private double[,,] Y = new double[m, N, n];           // Входная обучающая матрица Y[2,100,100]
        private int[,,] X = new int[m, N, n];                 // Бинарная учебная матрица X[2,100,100]
        private int[,] xm = new int[m, N];                    // центры классов (Эталонные векторы) xm[2,100]
        private double[,] _ndkMatrix = new double[m, N];      // Нижние системы допусков NDK[2,100]
        private double[,] _vdkMatrix = new double[m, N];      // Верхние системы допусков VDK[2,100]
        private double[,] _avgMatrix = new double[m, N];      // Средние значения AVG[2,100]
        private int[,,] _skMatrix = new int[m, m, n];         // Матрицы расстояний SK[2,2,100]
        private List<Bitmap> _trainingImages = new List<Bitmap>();
        #endregion

        #region Сервисы
        private readonly Pz1ImageLoader _imageLoader;
        private readonly Pz1TrainingMatrixBuilder _trainingMatrixBuilder;
        private readonly Pz2BinaryMatrixProcessor _binaryMatrixProcessor;
        private readonly Pz3ReferenceVectorCalculator _referenceVectorCalculator;
        private readonly Pz4CodeDistanceAnalyzer _codeDistanceAnalyzer;
        private readonly MatrixFormatter _matrixFormatter;
        #endregion

        #region Команды
        private RelayCommand? _calculateCommand;
        private RelayCommand? _loadFirstCommand;
        private RelayCommand? _loadSecondCommand;
        private RelayCommand? _clearCommand;
        
        private RelayCommand? _showTrainingDetailsClass0Command;
        private RelayCommand? _showTrainingDetailsClass1Command;
        private RelayCommand? _showBinaryDetailsClass0Command;
        private RelayCommand? _showBinaryDetailsClass1Command;
        private RelayCommand? _showReferenceDetailsClass0Command;
        private RelayCommand? _showReferenceDetailsClass1Command;
        private RelayCommand? _showDistanceDetailsClass0Command;
        private RelayCommand? _showDistanceDetailsClass1Command;
        private RelayCommand? _showToleranceDetailsClass0Command;
        private RelayCommand? _showToleranceDetailsClass1Command;
        
        #endregion

        #region Конструктор
        public MainWindowViewModel()
        {
            // Инициализация сервисов
            _imageLoader = new Pz1ImageLoader(this);
            _trainingMatrixBuilder = new Pz1TrainingMatrixBuilder(this);
            _binaryMatrixProcessor = new Pz2BinaryMatrixProcessor(this);
            _referenceVectorCalculator = new Pz3ReferenceVectorCalculator(this);
            _codeDistanceAnalyzer = new Pz4CodeDistanceAnalyzer(this);
            _matrixFormatter = new MatrixFormatter();

            // Инициализация команд
            InitializeCommands();
        }
        #endregion

        #region Публичные свойства команд
        public IRelayCommand LoadFirstCommand => _loadFirstCommand!;
        public IRelayCommand LoadSecondCommand => _loadSecondCommand!;
        public IRelayCommand CalculateCommand => _calculateCommand!;
        public IRelayCommand ClearCommand => _clearCommand!;
        
        public IRelayCommand ShowTrainingDetailsClass0Command => _showTrainingDetailsClass0Command!;
        public IRelayCommand ShowTrainingDetailsClass1Command => _showTrainingDetailsClass1Command!;
        public IRelayCommand ShowBinaryDetailsClass0Command => _showBinaryDetailsClass0Command!;
        public IRelayCommand ShowBinaryDetailsClass1Command => _showBinaryDetailsClass1Command!;
        public IRelayCommand ShowReferenceDetailsClass0Command => _showReferenceDetailsClass0Command!;
        public IRelayCommand ShowReferenceDetailsClass1Command => _showReferenceDetailsClass1Command!;
        public IRelayCommand ShowDistanceDetailsClass0Command => _showDistanceDetailsClass0Command!;
        public IRelayCommand ShowDistanceDetailsClass1Command => _showDistanceDetailsClass1Command!;
        public IRelayCommand ShowToleranceDetailsClass0Command => _showToleranceDetailsClass0Command!;
        public IRelayCommand ShowToleranceDetailsClass1Command => _showToleranceDetailsClass1Command!;
        
        #endregion

        #region Публичные свойства UI заголовков
        public string TrainingHeaderClass0 => "Обучающаяся матрица (K=0)";
        public string TrainingHeaderClass1 => "Обучающаяся матрица (K=1)";
        public string BinaryHeaderClass0 => $"Бинарная матрица (K=0, delta = {delta})";
        public string BinaryHeaderClass1 => $"Бинарная матрица (K=1, delta = {delta})";
        public string ReferenceHeaderClass0 => $"Эталонный вектор (K=0, p = {selec})";
        public string ReferenceHeaderClass1 => $"Эталонный вектор (K=1, p = {selec})";
        public string DistanceHeaderClass0 => "Матрица расстояний (K=0)";
        public string DistanceHeaderClass1 => "Матрица расстояний (K=1)";
        public string ToleranceHeaderClass0 => "Матрица допусков (K=0)";
        public string ToleranceHeaderClass1 => "Матрица допусков (K=1)";
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
        public double GetSelec() => selec;
        public int GetN() => N; 
        public int Getn() => n;  
        public int[,,] GetSkMatrix() => _skMatrix;
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
        public void SetSkMatrix(int[,,] value) => _skMatrix = value;
        #endregion

        #region Обработчики изменений свойств
        partial void OnCanLoadFirstChanged(bool value) => _loadFirstCommand?.NotifyCanExecuteChanged();
        partial void OnCanLoadSecondChanged(bool value) => _loadSecondCommand?.NotifyCanExecuteChanged();
        partial void OnCanCalculateChanged(bool value) => _calculateCommand?.NotifyCanExecuteChanged();
        partial void OnCanClearChanged(bool value) => _clearCommand?.NotifyCanExecuteChanged();
        partial void OnCanShowDetailsChanged(bool value)
        {
            _showTrainingDetailsClass0Command?.NotifyCanExecuteChanged();
            _showTrainingDetailsClass1Command?.NotifyCanExecuteChanged();
            _showBinaryDetailsClass0Command?.NotifyCanExecuteChanged();
            _showBinaryDetailsClass1Command?.NotifyCanExecuteChanged();
            _showReferenceDetailsClass0Command?.NotifyCanExecuteChanged();
            _showReferenceDetailsClass1Command?.NotifyCanExecuteChanged();
            _showDistanceDetailsClass0Command?.NotifyCanExecuteChanged();
            _showDistanceDetailsClass1Command?.NotifyCanExecuteChanged();
            _showToleranceDetailsClass0Command?.NotifyCanExecuteChanged();
            _showToleranceDetailsClass1Command?.NotifyCanExecuteChanged();
        }
        
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
            _showTrainingDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Обучающая матрица Y"), () => CanShowDetails);
            _showTrainingDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Обучающая матрица Y"), () => CanShowDetails);
            _showBinaryDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Бинарная матрица X"), () => CanShowDetails);
            _showBinaryDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Бинарная матрица X"), () => CanShowDetails);
            _showReferenceDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Эталонный вектор"), () => CanShowDetails);
            _showReferenceDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Эталонный вектор"), () => CanShowDetails);
            _showDistanceDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Матрица расстояний"), () => CanShowDetails);
            _showDistanceDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Матрица расстояний"), () => CanShowDetails);
            _showToleranceDetailsClass0Command = new RelayCommand(() => ShowDetails(0, "Матрица допусков"), () => CanShowDetails);
            _showToleranceDetailsClass1Command = new RelayCommand(() => ShowDetails(1, "Матрица допусков"), () => CanShowDetails);
            
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
                    sb.AppendLine(MatrixFormatter.GetLearningMatrixString(GetYMatrix(), classIndex));
                    break;
                case "Бинарная матрица X":
                    sb.AppendLine(MatrixFormatter.GetBinaryMatrixString(GetXMatrix(), GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), classIndex));
                    sb.AppendLine($"delta = {GetDelta()}");
                    break;
                case "Эталонный вектор":
                    sb.AppendLine(MatrixFormatter.GetReferenceVectorString(GetXmMatrix(), classIndex));
                    sb.AppendLine($"p = {GetSelec()}");
                    break;
                case "Матрица расстояний":
                    sb.AppendLine(MatrixFormatter.GetDistanceMatrixString(GetSkMatrix(), classIndex));
                    break;
                case "Матрица допусков":
                    sb.AppendLine(MatrixFormatter.GetToleranceMatrixString(GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), classIndex));
                    break;
            }

            _matrixFormatter.ShowDetailsWindow($"{matrixType} - Класс {classIndex}", sb.ToString());
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
            DistanceMatrixClass0 = "";
            DistanceMatrixClass1 = "";
            ToleranceMatrixClass0 = "";
            ToleranceMatrixClass1 = "";

            // Сброс матриц
            _ndkMatrix = new double[m, N];
            _vdkMatrix = new double[m, N];
            _avgMatrix = new double[m, N];
            _skMatrix = new int[m, m, n];

            // Сброс состояния кнопок
            CanLoadFirst = true;
            CanLoadSecond = false;
            CanCalculate = false;
            CanShowDetails = false;
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
            DistanceMatrixClass0 = "";
            DistanceMatrixClass1 = "";
            ToleranceMatrixClass0 = "";
            ToleranceMatrixClass1 = "";
            BinaryMatrixVisualization1 = null;
            BinaryMatrixVisualization2 = null;
            ReferenceVectorsVisualization1 = null;
            ReferenceVectorsVisualization2 = null;
            CanShowDetails = false;
            ReferenceVectors = "";
            _skMatrix = new int[m, m, n];
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

                // ПЗ4: Формирование массива кодовых расстояний и распределения реализаций
                _codeDistanceAnalyzer.Compute();

                // Обновление отображения матриц
                UpdateMatrixDisplay();

                // Визуализация результатов
                _binaryMatrixProcessor.VisualizeBinaryMatrix();
                _referenceVectorCalculator.VisualizeReferenceVectors();

                // Обновление состояния интерфейса
                CanShowDetails = true;
                CanClear = true;
            }
            catch (Exception ex)
            {
                // Логируем ошибку вычислений
                System.Diagnostics.Debug.WriteLine($"Ошибка при выполнении вычислений: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет отображение всех матриц в интерфейсе
        /// </summary>
        private void UpdateMatrixDisplay()
        {
            LearningMatrixClass0 = MatrixFormatter.GetLearningMatrixString(GetYMatrix(), 0);
            LearningMatrixClass1 = MatrixFormatter.GetLearningMatrixString(GetYMatrix(), 1);
            BinaryMatrixClass0 = MatrixFormatter.GetBinaryMatrixString(GetXMatrix(), GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), 0) + $"\ndelta = {GetDelta()}";
            BinaryMatrixClass1 = MatrixFormatter.GetBinaryMatrixString(GetXMatrix(), GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), 1) + $"\ndelta = {GetDelta()}";
            ReferenceVectorClass0 = MatrixFormatter.GetReferenceVectorString(GetXmMatrix(), 0) + $"\np = {GetSelec()}";
            ReferenceVectorClass1 = MatrixFormatter.GetReferenceVectorString(GetXmMatrix(), 1) + $"\np = {GetSelec()}";
            ReferenceVectors = MatrixFormatter.GetReferenceVectorString(GetXmMatrix());
            DistanceMatrixClass0 = MatrixFormatter.GetDistanceMatrixString(GetSkMatrix(), 0);
            DistanceMatrixClass1 = MatrixFormatter.GetDistanceMatrixString(GetSkMatrix(), 1);
            ToleranceMatrixClass0 = MatrixFormatter.GetToleranceMatrixString(GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), 0);
            ToleranceMatrixClass1 = MatrixFormatter.GetToleranceMatrixString(GetNdkMatrix(), GetVdkMatrix(), GetAvgMatrix(), 1);
        }

        
        #endregion
    }
}
