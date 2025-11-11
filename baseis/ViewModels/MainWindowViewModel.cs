using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        [ObservableProperty] private int delta = 42;   // контрольный допуск
        [ObservableProperty] private double selec = 0.55; // уровень селекции
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
        [ObservableProperty] private string pz5ShannonRadiusClass0 = "";
        [ObservableProperty] private string pz5ShannonD1Class0 = "";
        [ObservableProperty] private string pz5ShannonAlphaClass0 = "";
        [ObservableProperty] private string pz5ShannonBetaClass0 = "";
        [ObservableProperty] private string pz5ShannonD2Class0 = "";
        [ObservableProperty] private string pz5ShannonValueClass0 = "";
        [ObservableProperty] private string pz5ShannonRadiusClass1 = "";
        [ObservableProperty] private string pz5ShannonD1Class1 = "";
        [ObservableProperty] private string pz5ShannonAlphaClass1 = "";
        [ObservableProperty] private string pz5ShannonBetaClass1 = "";
        [ObservableProperty] private string pz5ShannonD2Class1 = "";
        [ObservableProperty] private string pz5ShannonValueClass1 = "";
        [ObservableProperty] private string pz5MaxRadiusText = "";
        [ObservableProperty] private string pz5KullbackRadiusClass0 = "";
        [ObservableProperty] private string pz5KullbackValueClass0 = "";
        [ObservableProperty] private string pz5KullbackD1Class0 = "";
        [ObservableProperty] private string pz5KullbackAlphaClass0 = "";
        [ObservableProperty] private string pz5KullbackBetaClass0 = "";
        [ObservableProperty] private string pz5KullbackD2Class0 = "";
        [ObservableProperty] private string pz5KullbackRadiusClass1 = "";
        [ObservableProperty] private string pz5KullbackValueClass1 = "";
        [ObservableProperty] private string pz5KullbackD1Class1 = "";
        [ObservableProperty] private string pz5KullbackAlphaClass1 = "";
        [ObservableProperty] private string pz5KullbackBetaClass1 = "";
        [ObservableProperty] private string pz5KullbackD2Class1 = "";
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
        [ObservableProperty] private bool canExportPz5 = false;
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
        private readonly Dictionary<int, List<Pz5RadiusMetrics>> _pz5Metrics = new();
        #endregion

        #region Сервисы
        private readonly Pz1ImageLoader _imageLoader;
        private readonly Pz1TrainingMatrixBuilder _trainingMatrixBuilder;
        private readonly Pz2BinaryMatrixProcessor _binaryMatrixProcessor;
        private readonly Pz3ReferenceVectorCalculator _referenceVectorCalculator;
        private readonly Pz4CodeDistanceAnalyzer _codeDistanceAnalyzer;
        private readonly Pz5AccuracyMetricsCalculator _pz5AccuracyMetricsCalculator;
        private readonly Pz5ShannonEfficiencyCalculator _pz5ShannonEfficiencyCalculator;
        private readonly Pz5KullbackEfficiencyCalculator _pz5KullbackEfficiencyCalculator;
        private readonly Pz5ExcelReportGenerator _pz5ExcelReportGenerator;
        private readonly Pz6ParameterOptimizer _pz6ParameterOptimizer;
        private readonly Pz6ExcelReportGenerator _pz6ExcelReportGenerator;
        private readonly MatrixFormatter _matrixFormatter;
        #endregion

        #region Команды
        private RelayCommand? _calculateCommand;
        private RelayCommand? _loadFirstCommand;
        private RelayCommand? _loadSecondCommand;
        private RelayCommand? _clearCommand;
        private RelayCommand? _exportPz5ReportCommand;
        private RelayCommand? _optimizeParametersCommand;
        
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
            _pz5AccuracyMetricsCalculator = new Pz5AccuracyMetricsCalculator(this);
            _pz5ShannonEfficiencyCalculator = new Pz5ShannonEfficiencyCalculator();
            _pz5KullbackEfficiencyCalculator = new Pz5KullbackEfficiencyCalculator();
            _pz5ExcelReportGenerator = new Pz5ExcelReportGenerator();
            _pz6ParameterOptimizer = new Pz6ParameterOptimizer(this);
            _pz6ExcelReportGenerator = new Pz6ExcelReportGenerator();
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
        public IRelayCommand ExportPz5ReportCommand => _exportPz5ReportCommand!;
        public IRelayCommand OptimizeParametersCommand => _optimizeParametersCommand!;
        
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
        public string BinaryHeaderClass0 => $"Бинарная матрица (K=0, delta = {Delta})";
        public string BinaryHeaderClass1 => $"Бинарная матрица (K=1, delta = {Delta})";
        public string ReferenceHeaderClass0 => $"Эталонный вектор (K=0, p = {Selec})";
        public string ReferenceHeaderClass1 => $"Эталонный вектор (K=1, p = {Selec})";
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
        public int GetDelta() => Delta;
        public double GetSelec() => Selec;
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
        partial void OnCanCalculateChanged(bool value)
        {
            _calculateCommand?.NotifyCanExecuteChanged();
            _optimizeParametersCommand?.NotifyCanExecuteChanged();
        }
        partial void OnCanClearChanged(bool value) => _clearCommand?.NotifyCanExecuteChanged();
        partial void OnCanExportPz5Changed(bool value) => _exportPz5ReportCommand?.NotifyCanExecuteChanged();
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

        partial void OnDeltaChanged(int value)
        {
            OnPropertyChanged(nameof(BinaryHeaderClass0));
            OnPropertyChanged(nameof(BinaryHeaderClass1));
        }

        partial void OnSelecChanged(double value)
        {
            OnPropertyChanged(nameof(ReferenceHeaderClass0));
            OnPropertyChanged(nameof(ReferenceHeaderClass1));
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
            _exportPz5ReportCommand = new RelayCommand(ExportPz5Report, () => CanExportPz5);
            _optimizeParametersCommand = new RelayCommand(OptimizeParameters, () => CanCalculate);
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
            ClearPz5Properties();

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
            CanExportPz5 = false;
            _pz5Metrics.Clear();
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
            CanExportPz5 = false;
            _pz5Metrics.Clear();
            ClearPz5Properties();
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
        private void Calculate() => TryRunFullCalculation();

        internal bool TryRunFullCalculation()
        {
            if (GetTrainingImages().Count < 2)
            {
                return false;
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

                // ПЗ5: Расчёт критериев функциональной эффективности
                ComputePz5Metrics();

                // Обновление состояния интерфейса
                CanShowDetails = true;
                CanClear = true;

                return true;
            }
            catch (Exception ex)
            {
                // Логируем ошибку вычислений
                System.Diagnostics.Debug.WriteLine($"Ошибка при выполнении вычислений: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Формирует таблицу и графики ПЗ5 и открывает Excel-файл с отчётом.
        /// </summary>
        private void ExportPz5Report()
        {
            if (_pz5Metrics.Count == 0)
            {
                ComputePz5Metrics();
                if (_pz5Metrics.Count == 0)
                {
                    return;
                }
            }

            try
            {
                var reportData = _pz5Metrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<Pz5RadiusMetrics>)kvp.Value.ToList());

                var path = _pz5ExcelReportGenerator.GenerateReport(reportData);
                if (!_pz5ExcelReportGenerator.TryOpenReport(path))
                {
                    _matrixFormatter.ShowDetailsWindow(
                        "Отчёт ПЗ5",
                        $"Отчёт сохранён по пути:\n{path}\n\nОткройте файл вручную.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка формирования отчёта ПЗ5: {ex.Message}");
            }
        }

        private void OptimizeParameters()
        {
            var settings = Pz6OptimizationSettings.CreateDefault(Delta, Selec);
            var result = _pz6ParameterOptimizer.Optimize(settings);
            if (result == null)
            {
                return;
            }

            try
            {
                var path = _pz6ExcelReportGenerator.GenerateReport(result);
                if (!_pz6ExcelReportGenerator.TryOpenReport(path))
                {
                    _matrixFormatter.ShowDetailsWindow(
                        "Оптимизация ПЗ6",
                        $"Отчёт сохранён по пути:\n{path}\n\nОткройте файл вручную.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка формирования отчёта ПЗ6: {ex.Message}");
            }
        }

        private void ComputePz5Metrics()
        {
            var computed = _pz5AccuracyMetricsCalculator.Compute();
            _pz5Metrics.Clear();

            foreach (var kvp in computed)
            {
                var list = new List<Pz5RadiusMetrics>(kvp.Value);
                _pz5ShannonEfficiencyCalculator.Compute(list);
                _pz5KullbackEfficiencyCalculator.Compute(list);
                _pz5Metrics[kvp.Key] = list;
            }

            var maxRadiusByClass = _pz5AccuracyMetricsCalculator.MaxRadiusByClass;
            if (maxRadiusByClass.Count > 0)
            {
                int maxRadius = maxRadiusByClass.Values.Max();
                Pz5MaxRadiusText = $"Расстояние между центрами классов = {maxRadius+1}";
            }
            else
            {
                Pz5MaxRadiusText = "";
            }

            if (_pz5Metrics.Count == 0 || _pz5Metrics.All(kvp => kvp.Value.Count == 0))
            {
                ClearPz5Properties();
                CanExportPz5 = false;
                return;
            }

            for (int classIndex = 0; classIndex < m; classIndex++)
            {
                var bestShannon = GetBestMetric(classIndex, metric => metric.ShannonKfe);
                SetShannonProperties(classIndex, bestShannon);

                var bestKullback = GetBestMetric(classIndex, metric => metric.KullbackKfe);
                SetKullbackProperties(classIndex, bestKullback);
            }

            CanExportPz5 = true;
        }

        internal IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> GetPz5MetricsSnapshot()
        {
            var snapshot = new Dictionary<int, IReadOnlyList<Pz5RadiusMetrics>>(_pz5Metrics.Count);
            foreach (var kvp in _pz5Metrics)
            {
                var copy = kvp.Value.Select(metric => metric.Clone()).ToList();
                snapshot[kvp.Key] = copy;
            }

            return snapshot;
        }

        private Pz5RadiusMetrics? GetBestMetric(int classIndex, Func<Pz5RadiusMetrics, double> selector)
        {
            if (!_pz5Metrics.TryGetValue(classIndex, out var list) || list.Count == 0)
            {
                return null;
            }

            Pz5RadiusMetrics? best = null;
            double bestValue = double.MinValue;

            foreach (var metric in list)
            {
                if (!metric.IsReliable)
                {
                    continue;
                }

                double value = selector(metric);
                if (!double.IsFinite(value))
                {
                    continue;
                }

                if (best == null || value > bestValue)
                {
                    best = metric;
                    bestValue = value;
                }
            }

            return best;
        }

        private void SetShannonProperties(int classIndex, Pz5RadiusMetrics? metric)
        {
            string radius = metric != null ? $"r = {metric.Radius}" : "r = -";
            string d1 = metric != null ? $"D1 = {FormatValue(metric.D1)}" : "D1 = -";
            string alpha = metric != null ? $"α = {FormatValue(metric.Alpha)}" : "α = -";
            string beta = metric != null ? $"β = {FormatValue(metric.Beta)}" : "β = -";
            string d2 = metric != null ? $"D2 = {FormatValue(metric.D2)}" : "D2 = -";
            string value = metric != null ? $"KFE = {FormatValue(metric.ShannonKfe)}" : "KFE = -";

            if (classIndex == 0)
            {
                Pz5ShannonRadiusClass0 = radius;
                Pz5ShannonD1Class0 = d1;
                Pz5ShannonAlphaClass0 = alpha;
                Pz5ShannonBetaClass0 = beta;
                Pz5ShannonD2Class0 = d2;
                Pz5ShannonValueClass0 = value;
            }
            else
            {
                Pz5ShannonRadiusClass1 = radius;
                Pz5ShannonD1Class1 = d1;
                Pz5ShannonAlphaClass1 = alpha;
                Pz5ShannonBetaClass1 = beta;
                Pz5ShannonD2Class1 = d2;
                Pz5ShannonValueClass1 = value;
            }
        }

        private void SetKullbackProperties(int classIndex, Pz5RadiusMetrics? metric)
        {
            string radius = metric != null ? $"r = {metric.Radius}" : "r = -";
            string value = metric != null ? $"KFE = {FormatValue(metric.KullbackKfe)}" : "KFE = -";
            string d1 = metric != null ? $"D1 = {FormatValue(metric.D1)}" : "D1 = -";
            string alpha = metric != null ? $"α = {FormatValue(metric.Alpha)}" : "α = -";
            string beta = metric != null ? $"β = {FormatValue(metric.Beta)}" : "β = -";
            string d2 = metric != null ? $"D2 = {FormatValue(metric.D2)}" : "D2 = -";

            if (classIndex == 0)
            {
                Pz5KullbackRadiusClass0 = radius;
                Pz5KullbackValueClass0 = value;
                Pz5KullbackD1Class0 = d1;
                Pz5KullbackAlphaClass0 = alpha;
                Pz5KullbackBetaClass0 = beta;
                Pz5KullbackD2Class0 = d2;
            }
            else
            {
                Pz5KullbackRadiusClass1 = radius;
                Pz5KullbackValueClass1 = value;
                Pz5KullbackD1Class1 = d1;
                Pz5KullbackAlphaClass1 = alpha;
                Pz5KullbackBetaClass1 = beta;
                Pz5KullbackD2Class1 = d2;
            }
        }

        private void ClearPz5Properties()
        {
            Pz5ShannonRadiusClass0 = "";
            Pz5ShannonD1Class0 = "";
            Pz5ShannonAlphaClass0 = "";
            Pz5ShannonBetaClass0 = "";
            Pz5ShannonD2Class0 = "";
            Pz5ShannonValueClass0 = "";
            Pz5ShannonRadiusClass1 = "";
            Pz5ShannonD1Class1 = "";
            Pz5ShannonAlphaClass1 = "";
            Pz5ShannonBetaClass1 = "";
            Pz5ShannonD2Class1 = "";
            Pz5ShannonValueClass1 = "";
            Pz5KullbackRadiusClass0 = "";
            Pz5KullbackValueClass0 = "";
            Pz5KullbackD1Class0 = "";
            Pz5KullbackAlphaClass0 = "";
            Pz5KullbackBetaClass0 = "";
            Pz5KullbackD2Class0 = "";
            Pz5KullbackRadiusClass1 = "";
            Pz5KullbackValueClass1 = "";
            Pz5KullbackD1Class1 = "";
            Pz5KullbackAlphaClass1 = "";
            Pz5KullbackBetaClass1 = "";
            Pz5KullbackD2Class1 = "";
            Pz5MaxRadiusText = "";
        }

        private static string FormatValue(double value) =>
            double.IsFinite(value) ? value.ToString("0.000", CultureInfo.InvariantCulture) : "-";

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
