using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Legends;

public class ChartForm : Form
{
    private PeriodSelectionDialog.PeriodOption period;


    public ChartForm(PeriodSelectionDialog.PeriodOption period)
    {
        this.period = period;

        Text = $"Графики выработки – {period}";
        Size = new System.Drawing.Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;

        var tabControl = new TabControl { Dock = DockStyle.Fill };

        tabControl.TabPages.Add(CreateTab("Статическая панель", GetFile("static")));
        tabControl.TabPages.Add(CreateTab("Динамическая панель", GetFile("tracker")));
        tabControl.TabPages.Add(CreateTab("Сравнение", null, compare: true));

        Controls.Add(tabControl);
    }

    private string GetFile(string type)
    {
        return period switch
        {
            PeriodSelectionDialog.PeriodOption.Week => $"energy_{type}.txt",
            PeriodSelectionDialog.PeriodOption.Month => $"energy_{type}_month.txt",
            PeriodSelectionDialog.PeriodOption.Year => $"energy_{type}_year.txt",
            _ => null
        };
    }


    private TabPage CreateTab(string title, string filePath, bool compare = false)
    {
        var tab = new TabPage { Text = title };
        var plotView = new PlotView { Dock = DockStyle.Fill };
        var plotModel = new PlotModel { Title = title };

        // Создаём легенду и настраиваем её параметры
        var legend = new Legend
        {
            LegendPosition = LegendPosition.TopRight,
            LegendPlacement = LegendPlacement.Outside,
            LegendOrientation = LegendOrientation.Horizontal,
            LegendBorderThickness = 0
        };

        // Добавляем легенду в модель графика
        plotModel.Legends.Add(legend);

        if (compare)
        {
            var staticData = LoadData(GetFile("static"));
            var trackerData = LoadData(GetFile("tracker"));

            if (staticData.Count == 0 && trackerData.Count == 0)
            {
                plotModel.Title = "Нет данных для сравнения";
            }
            else
            {
                if (staticData.Count > 0)
                {
                    var staticSeries = new LineSeries { Title = "Статическая панель", MarkerType = MarkerType.Circle };
                    foreach (var (time, energy) in staticData)
                        staticSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), energy));
                    plotModel.Series.Add(staticSeries);
                }

                if (trackerData.Count > 0)
                {
                    var trackerSeries = new LineSeries { Title = "Динамическая панель", MarkerType = MarkerType.Square };
                    foreach (var (time, energy) in trackerData)
                        trackerSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), energy));
                    plotModel.Series.Add(trackerSeries);
                }


                AddAxes(plotModel);
            }
        }
        else if (filePath != null && File.Exists(filePath))
        {
            var data = LoadData(filePath);
            if (data.Count > 0)
            {
                var series = new LineSeries { Title = title, MarkerType = MarkerType.Circle };
                foreach (var (time, energy) in data)
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(time), energy));

                plotModel.Series.Add(series);
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
            Title = "Выработка (Вт⋅ч)",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        });
    }

    private List<(DateTime, double)> LoadData(string filePath)
    {
        var data = new List<(DateTime, double)>();

        if (!File.Exists(filePath)) return data;

        var lines = File.ReadAllLines(filePath).Skip(1); // Пропускаем заголовок
        var culture = CultureInfo.GetCultureInfo("ru-RU"); // Обрабатываем запятую как десятичный знак

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("|") || line.StartsWith("Итог"))
                continue;

            var parts = line.Split('|');
            if (parts.Length < 2) continue;

            string dateStr = parts[0].Trim();
            string energyStr = parts[1].Trim();

            if (DateTime.TryParse(dateStr, out DateTime date) &&
                double.TryParse(energyStr, NumberStyles.Any, culture, out double energy))
            {
                data.Add((date, energy));
            }
        }

        return data;
    }

}
