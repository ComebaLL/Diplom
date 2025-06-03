using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class ChartForm : Form
{
    private readonly DateTime startDate;
    private readonly DateTime endDate;

    public ChartForm(DateTime startDate, DateTime endDate)
    {
        this.startDate = startDate;
        this.endDate = endDate;

        Text = "Графики выработки по группам";
        Size = new System.Drawing.Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;

        var tabControl = new TabControl { Dock = DockStyle.Fill };

        tabControl.TabPages.Add(CreateTab("Группа 1", "group1_output.txt"));
        tabControl.TabPages.Add(CreateTab("Группа 2", "group2_output.txt"));
        tabControl.TabPages.Add(CreateTab("Сравнение", null, compare: true));

        Controls.Add(tabControl);
    }

    private TabPage CreateTab(string title, string filePath, bool compare = false)
    {
        var tab = new TabPage { Text = title };
        var plotView = new PlotView { Dock = DockStyle.Fill };
        var plotModel = new PlotModel { Title = title };

        plotModel.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.TopCenter,
            LegendPlacement = LegendPlacement.Outside,
            LegendOrientation = LegendOrientation.Horizontal,
            LegendBorderThickness = 0
        });

        if (compare)
        {
            var group1 = LoadDataExtended("group1_output.txt");
            var group2 = LoadDataExtended("group2_output.txt");

            if (group1.Count == 0 && group2.Count == 0)
            {
                plotModel.Title = "Нет данных для сравнения";
            }
            else
            {
                if (group1.Count > 0)
                {
                    var prod1 = new LineSeries { Title = "Группа 1 — Выработка", MarkerType = MarkerType.Circle };
                    var cons1 = new LineSeries { Title = "Группа 1 — Потребление", MarkerType = MarkerType.None, LineStyle = LineStyle.Dash };

                    foreach (var entry in group1)
                    {
                        prod1.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Date), entry.Output));
                        if (entry.Consumption.HasValue)
                            cons1.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Date), entry.Consumption.Value));
                    }

                    plotModel.Series.Add(prod1);
                    if (cons1.Points.Count > 0)
                        plotModel.Series.Add(cons1);
                }

                if (group2.Count > 0)
                {
                    var prod2 = new LineSeries { Title = "Группа 2 — Выработка", MarkerType = MarkerType.Square };
                    var cons2 = new LineSeries { Title = "Группа 2 — Потребление", MarkerType = MarkerType.None, LineStyle = LineStyle.Dash };

                    foreach (var entry in group2)
                    {
                        prod2.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Date), entry.Output));
                        if (entry.Consumption.HasValue)
                            cons2.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Date), entry.Consumption.Value));
                    }

                    plotModel.Series.Add(prod2);
                    if (cons2.Points.Count > 0)
                        plotModel.Series.Add(cons2);
                }

                AddAxes(plotModel);
            }
        }
        else if (filePath != null && File.Exists(filePath))
        {
            var data = LoadDataExtended(filePath);

            if (data.Count > 0)
            {
                var prodSeries = new LineSeries { Title = title + " — Выработка", MarkerType = MarkerType.Circle };
                var consSeries = new LineSeries { Title = title + " — Потребление", MarkerType = MarkerType.None, LineStyle = LineStyle.Dash };

                foreach (var entry in data)
                {
                    prodSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Date), entry.Output));
                    if (entry.Consumption.HasValue)
                        consSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entry.Date), entry.Consumption.Value));
                }

                plotModel.Series.Add(prodSeries);
                if (consSeries.Points.Count > 0)
                    plotModel.Series.Add(consSeries);

                AddAxes(plotModel);
            }
            else
            {
                plotModel.Title = "Нет данных для отображения";
            }
        }
        else
        {
            plotModel.Title = "Файл не найден или пуст";
        }

        plotView.Model = plotModel;
        tab.Controls.Add(plotView);
        return tab;
    }


    private void AddAxes(PlotModel model)
    {
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd.MM",
            Title = "Дата",
            IntervalType = DateTimeIntervalType.Days,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Энергия (кВт⋅ч)",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
    }

    private List<(DateTime Date, double Output, double? Consumption)> LoadDataExtended(string filePath)
    {
        var data = new List<(DateTime, double, double?)>();
        var lines = File.ReadAllLines(filePath);
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Пропускаем заголовки
            if (line.StartsWith("Результаты") || line.StartsWith("Период") || line.StartsWith("ИТОГО") || line.StartsWith("Общая"))
                continue;

            if (Regex.IsMatch(line, @"^\d{4}-\d{2}-\d{2}:\s*\d+,\d+\s*кВт·ч"))
            {
                var parts = line.Split(':');
                if (parts.Length >= 2 && DateTime.TryParse(parts[0].Trim(), out var date))
                {
                    var energyStr = parts[1].Replace("кВт·ч", "").Trim();
                    if (double.TryParse(energyStr, NumberStyles.Any, culture, out var energy))
                    {
                        data.Add((date, energy, null));  // Только выработка
                    }
                }
            }
            else if (Regex.IsMatch(line, @"^\d{4}-\d{2}-\d{2}:.*Производство.*Потребление"))
            {
                var match = Regex.Match(line, @"^(?<date>\d{4}-\d{2}-\d{2}):\s*Производство\s*=\s*(?<prod>[\d,]+)\s*кВт·ч,\s*Потребление\s*=\s*(?<cons>[\d,]+)\s*кВт·ч");
                if (match.Success &&
                    DateTime.TryParse(match.Groups["date"].Value, out var date) &&
                    double.TryParse(match.Groups["prod"].Value, NumberStyles.Any, culture, out var prod) &&
                    double.TryParse(match.Groups["cons"].Value, NumberStyles.Any, culture, out var cons))
                {
                    data.Add((date, prod, cons));
                }
            }
        }

        return data;
    }

}
