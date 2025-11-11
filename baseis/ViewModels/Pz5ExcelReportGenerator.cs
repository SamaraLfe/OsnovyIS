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
    public class Pz5ExcelReportGenerator
    {
        private static readonly XLColor HeaderColor = XLColor.FromHtml("#1E88E5");
        private static readonly XLColor HeaderTextColor = XLColor.White;
        private static readonly XLColor HighlightColor = XLColor.FromHtml("#C5E1A5");

        public string GenerateReport(IReadOnlyDictionary<int, IReadOnlyList<Pz5RadiusMetrics>> metricsByClass)
        {
            string directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PZ5Reports");
            Directory.CreateDirectory(directory);

            string filePath = System.IO.Path.Combine(directory, $"PZ5_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            using var workbook = new XLWorkbook();

            foreach (var (classIndex, metrics) in metricsByClass.OrderBy(k => k.Key))
            {
                var sheet = workbook.Worksheets.Add($"Класс {classIndex}");
                WriteClassSheet(sheet, metrics);
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
                Debug.WriteLine($"Не удалось открыть отчёт ПЗ5: {ex.Message}");
                return false;
            }
        }

        private static void WriteClassSheet(IXLWorksheet sheet, IReadOnlyList<Pz5RadiusMetrics> metrics)
        {
            var headers = new[]
            {
                "r", "D1", "α", "β", "D2", "KFE (Шеннона)", "KFE (Кульбака)", "Зона", "Оптим. r"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = HeaderColor;
                cell.Style.Font.FontColor = HeaderTextColor;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int? shannonBest = GetBestRadius(metrics, m => m.ShannonKfe);
            int? kullbackBest = GetBestRadius(metrics, m => m.KullbackKfe);

            for (int i = 0; i < metrics.Count; i++)
            {
                var m = metrics[i];
                int row = i + 2;

                sheet.Cell(row, 1).Value = m.Radius;
                sheet.Cell(row, 2).Value = m.D1;
                sheet.Cell(row, 3).Value = m.Alpha;
                sheet.Cell(row, 4).Value = m.Beta;
                sheet.Cell(row, 5).Value = m.D2;
                sheet.Cell(row, 6).Value = m.ShannonKfe;
                sheet.Cell(row, 7).Value = m.KullbackKfe;
                sheet.Cell(row, 8).Value = m.IsReliable ? "Рабочая" : "";
                sheet.Cell(row, 9).Value = GetOptimalMarker(m.Radius, shannonBest, kullbackBest);
                sheet.Range(row, 2, row, 7).Style.NumberFormat.Format = "0.000";

                if (m.IsReliable)
                {
                    sheet.Row(row).Style.Fill.BackgroundColor = HighlightColor;
                }
            }

            var table = sheet.Range(1, 1, metrics.Count + 1, headers.Length).CreateTable();
            table.Theme = XLTableTheme.TableStyleLight9;
            table.ShowAutoFilter = true;
            sheet.Columns(1, headers.Length).AdjustToContents();

            if (metrics.Count == 0)
            {
                return;
            }

            using var chartStream = new MemoryStream(CreateChartImage(metrics));
            var picture = sheet.AddPicture(chartStream, $"Chart_{Guid.NewGuid():N}"[..31]);
            picture.MoveTo(sheet.Cell(2, headers.Length + 1));
        }

        private static byte[] CreateChartImage(IReadOnlyList<Pz5RadiusMetrics> metrics)
        {
            var radii = metrics.Select(m => (double)m.Radius).ToList();
            var values = metrics.SelectMany(m => new[] { m.ShannonKfe, m.KullbackKfe }).Where(double.IsFinite).DefaultIfEmpty(1).ToList();

            var (minRadius, maxRadius) = NormalizeRange(radii);
            var (minValue, maxValue) = NormalizeRange(values);

            const int width = 780;
            const int height = 420;
            const int marginLeft = 80;
            const int marginRight = 40;
            const int marginTop = 50;
            const int marginBottom = 70;

            using var image = new Image<Rgba32>(width, height, new Rgba32(255, 255, 255));
            var fontTitle = ResolveFont(20, FontStyle.Bold);
            var fontAxis = ResolveFont(14);
            var fontTick = ResolveFont(12);

            float plotWidth = width - marginLeft - marginRight;
            float plotHeight = height - marginTop - marginBottom;

            float X(double r) => (float)(marginLeft + (r - minRadius) / (maxRadius - minRadius) * plotWidth);
            float Y(double v) => (float)(marginTop + (1 - (v - minValue) / (maxValue - minValue)) * plotHeight);

            image.Mutate(ctx =>
            {
                ctx.Fill(new Rgba32(250, 250, 250), new RectangleF(marginLeft, marginTop, plotWidth, plotHeight));
                ctx.DrawText("KFE (Шеннон и Кульбак)", fontTitle, new Rgba32(33, 33, 33), new PointF(marginLeft, 10));
                ctx.DrawText("r", fontAxis, new Rgba32(66, 66, 66), new PointF(width / 2f, height - marginBottom + 30));
                ctx.DrawText("KFE", fontAxis, new Rgba32(66, 66, 66), new PointF(10, marginTop - 20));
            });

            DrawLine(image, new Rgba32(0, 0, 0), 1.5f, new PointF(marginLeft, marginTop), new PointF(marginLeft, marginTop + plotHeight));
            DrawLine(image, new Rgba32(0, 0, 0), 1.5f, new PointF(marginLeft, marginTop + plotHeight), new PointF(marginLeft + plotWidth, marginTop + plotHeight));

            for (int i = 0; i <= 5; i++)
            {
                float y = Y(minValue + i * (maxValue - minValue) / 5);
                DrawLine(image, new Rgba32(210, 210, 210), 1f, new PointF(marginLeft, y), new PointF(marginLeft + plotWidth, y));
                image.Mutate(ctx => ctx.DrawText((minValue + i * (maxValue - minValue) / 5).ToString("0.00", CultureInfo.InvariantCulture), fontTick, new Rgba32(97, 97, 97), new PointF(10, y - 8)));
            }

            for (int i = 0; i <= Math.Min(10, metrics.Count); i++)
            {
                double radiusValue = minRadius + i * (maxRadius - minRadius) / Math.Max(1, Math.Min(10, metrics.Count));
                float x = X(radiusValue);
                DrawLine(image, new Rgba32(210, 210, 210), 1f, new PointF(x, marginTop), new PointF(x, marginTop + plotHeight));
                image.Mutate(ctx => ctx.DrawText(Math.Round(radiusValue).ToString("0", CultureInfo.InvariantCulture), fontTick, new Rgba32(97, 97, 97), new PointF(x - 10, height - marginBottom + 15)));
            }

            foreach (var (start, end) in CalculateReliableSpans(metrics))
            {
                float left = X(start);
                float right = X(end);
                DrawLine(image, new Rgba32(56, 142, 60), 2f, new PointF(left, marginTop), new PointF(left, marginTop + plotHeight), new[] { 8f, 6f });
                DrawLine(image, new Rgba32(56, 142, 60), 2f, new PointF(right, marginTop), new PointF(right, marginTop + plotHeight), new[] { 8f, 6f });
                image.Mutate(ctx => ctx.Fill(new Rgba32(129, 199, 132, 90), new RectangleF(left, marginTop, Math.Max(1, right - left), plotHeight)));
            }

            DrawMetricLine(image, metrics, m => m.ShannonKfe, X, Y, new Rgba32(30, 136, 229));
            DrawMetricLine(image, metrics, m => m.KullbackKfe, X, Y, new Rgba32(239, 83, 80));

            image.Mutate(ctx =>
            {
                float x = marginLeft + plotWidth - 170;
                float y = marginTop - 30;
                DrawLine(image, new Rgba32(30, 136, 229), 3f, new PointF(x, y), new PointF(x + 30, y));
                ctx.DrawText("Шеннон", fontTick, new Rgba32(33, 33, 33), new PointF(x + 40, y - 8));
                DrawLine(image, new Rgba32(239, 83, 80), 3f, new PointF(x, y + 20), new PointF(x + 30, y + 20));
                ctx.DrawText("Кульбак", fontTick, new Rgba32(33, 33, 33), new PointF(x + 40, y + 12));
            });

            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;
            return stream.ToArray();
        }

        private static void DrawMetricLine(
            Image<Rgba32> image,
            IEnumerable<Pz5RadiusMetrics> metrics,
            Func<Pz5RadiusMetrics, double> selector,
            Func<double, float> x,
            Func<double, float> y,
            Rgba32 color)
        {
            PointF? prev = null;
            foreach (var metric in metrics)
            {
                double value = selector(metric);
                if (!double.IsFinite(value))
                {
                    prev = null;
                    continue;
                }

                var point = new PointF(x(metric.Radius), y(value));
                if (prev.HasValue)
                {
                    DrawLine(image, color, 2.5f, prev.Value, point);
                }
                prev = point;
            }
        }

        private static void DrawLine(Image<Rgba32> image, Rgba32 color, float thickness, PointF start, PointF end, float[]? dash = null)
        {
            if (dash == null || dash.Length == 0)
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
            int dashIndex = 0;
            bool draw = true;

            while (offset < length)
            {
                float segmentLength = dash[dashIndex % dash.Length];
                float nextOffset = Math.Min(length, offset + segmentLength);
                float t1 = offset / length;
                float t2 = nextOffset / length;

                var segmentStart = new PointF(start.X + vector.X * t1, start.Y + vector.Y * t1);
                var segmentEnd = new PointF(start.X + vector.X * t2, start.Y + vector.Y * t2);

                if (draw)
                {
                    image.Mutate(ctx => ctx.Draw(color, thickness, new PathBuilder().AddLine(segmentStart, segmentEnd).Build()));
                }

                offset = nextOffset;
                dashIndex++;
                draw = !draw;
            }
        }

        private static Font ResolveFont(float size, FontStyle style = FontStyle.Regular)
        {
            if (SystemFonts.TryGet("Arial", out var arial))
            {
                return arial.CreateFont(size, style);
            }
            return SystemFonts.Collection.Families.First().CreateFont(size, style);
        }

        private static (double min, double max) NormalizeRange(IReadOnlyList<double> values)
        {
            double min = values.Min();
            double max = values.Max();
            return Math.Abs(max - min) < 1e-9 ? (min, min + 1) : (min, max);
        }

        private static IEnumerable<(double Start, double End)> CalculateReliableSpans(IReadOnlyList<Pz5RadiusMetrics> metrics)
        {
            double? start = null;
            double lastRadius = metrics[0].Radius;

            foreach (var metric in metrics)
            {
                if (metric.IsReliable)
                {
                    start ??= metric.Radius - 0.5;
                }
                else if (start != null)
                {
                    yield return (start.Value, lastRadius + 0.5);
                    start = null;
                }

                lastRadius = metric.Radius;
            }

            if (start != null)
            {
                yield return (start.Value, metrics[^1].Radius + 0.5);
            }
        }

        private static int? GetBestRadius(IReadOnlyList<Pz5RadiusMetrics> metrics, Func<Pz5RadiusMetrics, double> selector)
        {
            double bestValue = double.MinValue;
            int? bestRadius = null;
            foreach (var metric in metrics.Where(m => m.IsReliable))
            {
                double value = selector(metric);
                if (double.IsFinite(value) && (bestRadius == null || value > bestValue))
                {
                    bestValue = value;
                    bestRadius = metric.Radius;
                }
            }
            return bestRadius;
        }

        private static string GetOptimalMarker(int radius, int? shannonRadius, int? kullbackRadius)
        {
            var tags = new List<string>();
            if (radius == shannonRadius) tags.Add("KFE E");
            if (radius == kullbackRadius) tags.Add("KFE K");
            return string.Join(", ", tags);
        }
    }

}
