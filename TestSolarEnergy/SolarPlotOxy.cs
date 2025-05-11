using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

public class SolarPlotOxy
{
    public static void GenerateSolarProductionPlot()
    {
        double latitude = 52.02428 * Math.PI / 180;
        double declination = -20.1039 * Math.PI / 180;
        int sunrise = 508;
        int sunset = 1048;
        int zenithTime = 810;
        double nominalPower = 300;

        var hours = new List<double>();
        var production = new List<double>();

        for (int minute = sunrise; minute <= sunset; minute += 10)
        {
            double H = (minute - zenithTime) * Math.PI / 720.0;
            double sin_iSZ = Math.Sin(declination) * Math.Sin(latitude) +
                             Math.Cos(declination) * Math.Cos(latitude) * Math.Cos(H);
            double iSZ = Math.Asin(sin_iSZ);

            double kT = 1.0;
            double Pv = nominalPower * Math.Cos(Math.PI / 2 - iSZ) * kT;
            if (Pv < 0) Pv = 0;

            hours.Add(minute / 60.0);
            production.Add(Pv);
        }

        var model = new PlotModel { Title = "Суточная выработка солнечной панели" };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Время (часы)" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Мощность (Вт)" });

        var series = new LineSeries { Title = "Выработка" };
        for (int i = 0; i < hours.Count; i++)
            series.Points.Add(new DataPoint(hours[i], production[i]));

        model.Series.Add(series);

        // Показываем график во всплывающем окне
        var form = new PlotForm(model);
        Application.Run(form);
    }
}
