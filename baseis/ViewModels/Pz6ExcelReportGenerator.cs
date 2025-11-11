using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace baseis.ViewModels
{
    /// <summary>
    /// Отчёт по оптимизации контрольных допусков (ПЗ6).
    /// </summary>
    public class Pz6ExcelReportGenerator
    {
        private static readonly XLColor HeaderColor = XLColor.FromHtml("#0D47A1");
        private static readonly XLColor HeaderTextColor = XLColor.White;
        private static readonly XLColor TableColor = XLColor.FromHtml("#E3F2FD");

        public string GenerateReport(Pz6OptimizationResult result)
        {
            string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PZ6Reports");
            Directory.CreateDirectory(directory);

            string fileName = $"PZ6_Optimization_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = System.IO.Path.Combine(directory, fileName);

            using var workbook = new XLWorkbook();

            foreach (var classIndex in result.Original.MetricsByClass.Keys
                         .Union(result.Optimized.MetricsByClass.Keys)
                         .OrderBy(i => i))
            {
                var originalMetrics = GetMetricsOrEmpty(result.Original.MetricsByClass, classIndex);
                var optimizedMetrics = GetMetricsOrEmpty(result.Optimized.MetricsByClass, classIndex);
                var classSheet = workbook.Worksheets.Add($"Класс {classIndex}");

                FillClassSummaryTable(classSheet, classIndex, result, originalMetrics, optimizedMetrics);

                var classShannonImage = CreateClassChartImage(
                    originalMetrics,
                    optimizedMetrics,
                    metric => metric.ShannonKfe,
                    $"КФЭ Шеннона — класс {classIndex}",
                    "KFE",
                    new Rgba32(30, 136, 229));

                InsertChartImage(classSheet, classShannonImage, 18, 1);

                var classKullbackImage = CreateClassChartImage(
                    originalMetrics,
                    optimizedMetrics,
                    metric => metric.KullbackKfe,
                    $"КФЭ Кульбака — класс {classIndex}",
                    "KFE",
                    new Rgba32(239, 125, 50));

                InsertChartImage(classSheet, classKullbackImage, 41, 1);
            }

            workbook.SaveAs(filePath);
            return filePath;
        }

        public bool TryOpenReport(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось открыть отчёт ПЗ6: {ex.Message}");
                return false;
            }
        }

        private static string FormatNumber(double value) =>
            double.IsFinite(value) ? value.ToString("0.000", CultureInfo.InvariantCulture) : "-";

        private static void InsertChartImage(IXLWorksheet worksheet, byte[] imageBytes, int firstRow, int firstColumn)
        {
            if (imageBytes.Length == 0)
            {
                return;
            }

            using var stream = new MemoryStream(imageBytes);
            var pictureName = $"Chart_{Guid.NewGuid():N}";
            if (pictureName.Length > 31)
            {
                pictureName = pictureName[..31];
            }

            var picture = worksheet.AddPicture(stream, pictureName);
            picture.MoveTo(worksheet.Cell(firstRow, firstColumn));
        }

        private static void FillClassSummaryTable(
            IXLWorksheet worksheet,
            int classIndex,
            Pz6OptimizationResult result,
            IReadOnlyList<Pz5RadiusMetrics> original,
            IReadOnlyList<Pz5RadiusMetrics> optimized)
        {
            worksheet.Cell(1, 1).Value = $"Класс {classIndex} — сравнение параметров";
            worksheet.Range(1, 1, 1, 3).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Range(3, 1, 3, 3)
                .Style.Fill.SetBackgroundColor(HeaderColor)
                .Font.SetFontColor(HeaderTextColor)
                .Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            worksheet.Cell(3, 1).Value = "Параметр";
            worksheet.Cell(3, 2).Value = "До";
            worksheet.Cell(3, 3).Value = "После";

            int row = 4;
            AddSummaryRow(worksheet, ref row, "delta", result.Original.Delta.ToString("0"), result.Optimized.Delta.ToString("0"));
            AddSummaryRow(worksheet, ref row, "p", result.Original.Selec.ToString("0.00", CultureInfo.InvariantCulture), result.Optimized.Selec.ToString("0.00", CultureInfo.InvariantCulture));

            double shannonBefore = GetBestValueFromList(original, metric => metric.ShannonKfe);
            double shannonAfter = GetBestValueFromList(optimized, metric => metric.ShannonKfe);
            AddSummaryRow(worksheet, ref row, "KFE (Шеннон)", FormatNumber(shannonBefore), FormatNumber(shannonAfter));
            int? shannonRadiusBefore = GetBestRadiusFromList(original, metric => metric.ShannonKfe);
            int? shannonRadiusAfter = GetBestRadiusFromList(optimized, metric => metric.ShannonKfe);
            AddSummaryRow(worksheet, ref row, "r* (Шеннон)", FormatRadius(shannonRadiusBefore), FormatRadius(shannonRadiusAfter));

            double kullbackBefore = GetBestValueFromList(original, metric => metric.KullbackKfe);
            double kullbackAfter = GetBestValueFromList(optimized, metric => metric.KullbackKfe);
            AddSummaryRow(worksheet, ref row, "KFE (Кульбак)", FormatNumber(kullbackBefore), FormatNumber(kullbackAfter));
            int? kullbackRadiusBefore = GetBestRadiusFromList(original, metric => metric.KullbackKfe);
            int? kullbackRadiusAfter = GetBestRadiusFromList(optimized, metric => metric.KullbackKfe);
            AddSummaryRow(worksheet, ref row, "r* (Кульбак)", FormatRadius(kullbackRadiusBefore), FormatRadius(kullbackRadiusAfter));

            worksheet.Range(3, 1, row - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(3, 1, row - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(4, 1, row - 1, 3).Style.Fill.BackgroundColor = TableColor;
            worksheet.Columns(1, 3).AdjustToContents();
        }

        private static byte[] CreateClassChartImage(
            IReadOnlyList<Pz5RadiusMetrics> original,
            IReadOnlyList<Pz5RadiusMetrics> optimized,
            Func<Pz5RadiusMetrics, double> selector,
            string title,
            string yAxisLabel,
            Rgba32 baseColor)
        {
            var series = new List<Pz6ChartSeries>
            {
                new Pz6ChartSeries(
                    "До оптимизации",
                    Lighten(baseColor),
                    2f,
                    new[] { 8f, 6f },
                    ExtractPoints(original, selector)),
                new Pz6ChartSeries(
                    "После оптимизации",
                    baseColor,
                    3.5f,
                    null,
                    ExtractPoints(optimized, selector))
            }.Where(s => s.Points.Count > 0).ToList();

            if (series.Count == 0)
            {
                return Array.Empty<byte>();
            }

            const int width = 860;
            const int height = 420;
            const int marginLeft = 80;
            const int marginRight = 40;
            const int marginTop = 50;
            const int marginBottom = 70;

            using var image = new Image<Rgba32>(width, height, new Rgba32(255, 255, 255));
            var fontTitle = ResolveFont(20, FontStyle.Bold);
            var fontAxis = ResolveFont(14);
            var fontTick = ResolveFont(12);

            var allPoints = series.SelectMany(s => s.Points).ToList();
            double minX = allPoints.Min(p => p.X);
            double maxX = allPoints.Max(p => p.X);
            double minY = allPoints.Min(p => p.Y);
            double maxY = allPoints.Max(p => p.Y);

            if (Math.Abs(maxX - minX) < double.Epsilon)
            {
                maxX = minX + 1;
            }
            if (Math.Abs(maxY - minY) < 1e-9)
            {
                maxY = minY + 1;
            }

            double xRange = maxX - minX;
            double yRange = maxY - minY;

            float TransformX(double x) =>
                (float)(marginLeft + (x - minX) / xRange * (width - marginLeft - marginRight));

            float TransformY(double y)
            {
                double normalized = (y - minY) / yRange;
                double inverted = 1 - normalized;
                return (float)(marginTop + inverted * (height - marginTop - marginBottom));
            }

            image.Mutate(ctx =>
            {
                ctx.Fill(new Rgba32(250, 250, 250), new RectangleF(marginLeft, marginTop, width - marginLeft - marginRight, height - marginTop - marginBottom));
                ctx.DrawText(title, fontTitle, new Rgba32(33, 33, 33), new PointF(marginLeft, 10));
                ctx.DrawText("r", fontAxis, new Rgba32(66, 66, 66), new PointF(width / 2f, height - marginBottom + 30));
                ctx.DrawText(yAxisLabel, fontAxis, new Rgba32(66, 66, 66), new PointF(10, marginTop - 20));
            });

            var axisColor = new Rgba32(0, 0, 0);
            DrawLine(image, axisColor, 1.5f, new PointF(marginLeft, marginTop), new PointF(marginLeft, height - marginBottom));
            DrawLine(image, axisColor, 1.5f, new PointF(marginLeft, height - marginBottom), new PointF(width - marginRight, height - marginBottom));

            const int gridLines = 5;
            for (int i = 0; i <= gridLines; i++)
            {
                double yValue = minY + i * (yRange / gridLines);
                float y = TransformY(yValue);
                DrawLine(image, new Rgba32(210, 210, 210), 1f, new PointF(marginLeft, y), new PointF(width - marginRight, y));

                image.Mutate(ctx => ctx.DrawText(
                    yValue.ToString("0.00", CultureInfo.InvariantCulture),
                    fontTick,
                    new Rgba32(97, 97, 97),
                    new PointF(15, y - 8)));
            }

            int xTicks = Math.Min(10, (int)xRange);
            if (xTicks < 5) xTicks = 5;
            for (int i = 0; i <= xTicks; i++)
            {
                double ratio = i / (double)Math.Max(1, xTicks);
                double radiusValue = minX + ratio * xRange;
                float x = TransformX(radiusValue);
                DrawLine(image, new Rgba32(210, 210, 210), 1f, new PointF(x, marginTop), new PointF(x, height - marginBottom));

                image.Mutate(ctx => ctx.DrawText(
                    Math.Round(radiusValue).ToString("0", CultureInfo.InvariantCulture),
                    fontTick,
                    new Rgba32(97, 97, 97),
                    new PointF(x - 10, height - marginBottom + 10)));
            }

            foreach (var seriesItem in series)
            {
                PointF? previous = null;
                foreach (var point in seriesItem.Points)
                {
                    var current = new PointF(TransformX(point.X), TransformY(point.Y));
                    if (previous.HasValue)
                    {
                        DrawLine(image, seriesItem.Color, seriesItem.Width, previous.Value, current, seriesItem.DashPattern);
                    }
                    previous = current;
                }
            }

            DrawLegend(image, series, fontTick, width - marginRight - 220, marginTop + 10);

            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;
            return stream.ToArray();
        }

        private static List<(double X, double Y)> ExtractPoints(
            IReadOnlyList<Pz5RadiusMetrics> metrics,
            Func<Pz5RadiusMetrics, double> selector)
        {
            var points = new List<(double X, double Y)>(metrics.Count);
            foreach (var metric in metrics)
            {
                double value = selector(metric);
                if (double.IsFinite(value))
                {
                    points.Add((metric.Radius, value));
                }
            }

            return points;
        }

        private static void DrawLegend(Image<Rgba32> image, IEnumerable<Pz6ChartSeries> series, Font font, float startX, float startY)
        {
            float y = startY;
            foreach (var seriesItem in series)
            {
                var x1 = startX;
                var x2 = startX + 30;
                DrawLine(image, seriesItem.Color, 3f, new PointF(x1, y), new PointF(x2, y), seriesItem.DashPattern);
                image.Mutate(ctx => ctx.DrawText(seriesItem.Label, font, new Rgba32(33, 33, 33), new PointF(x2 + 10, y - 8)));
                y += 20;
            }
        }

        private static void DrawLine(Image<Rgba32> image, Rgba32 color, float thickness, PointF start, PointF end, float[]? dashPattern = null)
        {
            if (dashPattern == null || dashPattern.Length == 0)
            {
                image.Mutate(ctx => ctx.Draw(color, thickness, new PathBuilder().AddLine(start, end).Build()));
                return;
            }

            var vector = new PointF(end.X - start.X, end.Y - start.Y);
            float length = MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            if (length <= float.Epsilon)
            {
                return;
            }

            float offset = 0;
            int patternIndex = 0;
            bool draw = true;

            while (offset < length)
            {
                float segmentLength = dashPattern[patternIndex % dashPattern.Length];
                float nextOffset = Math.Min(offset + segmentLength, length);

                float t1 = offset / length;
                float t2 = nextOffset / length;

                var segmentStart = new PointF(start.X + vector.X * t1, start.Y + vector.Y * t1);
                var segmentEnd = new PointF(start.X + vector.X * t2, start.Y + vector.Y * t2);

                if (draw)
                {
                    image.Mutate(ctx => ctx.Draw(color, thickness, new PathBuilder().AddLine(segmentStart, segmentEnd).Build()));
                }

                offset = nextOffset;
                patternIndex++;
                draw = !draw;
            }
        }

        private static Font ResolveFont(float size, FontStyle style = FontStyle.Regular)
        {
            if (SystemFonts.TryGet("Arial", out var arial))
            {
                return arial.CreateFont(size, style);
            }

            if (SystemFonts.TryGet("DejaVu Sans", out var dejavu))
            {
                return dejavu.CreateFont(size, style);
            }

            var families = SystemFonts.Collection.Families;
            if (families.Any())
            {
                return families.First().CreateFont(size, style);
            }

            throw new InvalidOperationException("Не удалось подобрать системный шрифт для построения графиков ПЗ6.");
        }

        private sealed record Pz6ChartSeries(
            string Label,
            Rgba32 Color,
            float Width,
            float[]? DashPattern,
            IReadOnlyList<(double X, double Y)> Points);

        private static IReadOnlyList<Pz5RadiusMetrics> GetMetricsOrEmpty(
            IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> source,
            int classIndex)
        {
            return source.TryGetValue(classIndex, out var metrics)
                ? metrics
                : Array.Empty<Pz5RadiusMetrics>();
        }

        private static Rgba32 Lighten(Rgba32 color)
        {
            byte Blend(byte component) => (byte)Math.Min(255, component + (255 - component) * 0.35);
            return new Rgba32(Blend(color.R), Blend(color.G), Blend(color.B));
        }

        private static void AddSummaryRow(IXLWorksheet worksheet, ref int row, string label, string beforeValue, string afterValue)
        {
            worksheet.Cell(row, 1).Value = label;
            worksheet.Cell(row, 2).Value = beforeValue;
            worksheet.Cell(row, 3).Value = afterValue;
            row++;
        }

        private static string FormatRadius(int? radius) =>
            radius.HasValue ? radius.Value.ToString("0") : "-";

        private static double GetBestValueFromList(IReadOnlyList<Pz5RadiusMetrics> metrics, Func<Pz5RadiusMetrics, double> selector)
        {
            double best = double.NaN;
            foreach (var metric in metrics)
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

                if (!double.IsFinite(best) || value > best)
                {
                    best = value;
                }
            }

            return best;
        }

        private static int? GetBestRadiusFromList(IReadOnlyList<Pz5RadiusMetrics> metrics, Func<Pz5RadiusMetrics, double> selector)
        {
            double bestValue = double.MinValue;
            int? bestRadius = null;

            foreach (var metric in metrics)
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

                if (bestRadius == null || value > bestValue)
                {
                    bestValue = value;
                    bestRadius = metric.Radius;
                }
            }

            return bestRadius;
        }
    }
}
