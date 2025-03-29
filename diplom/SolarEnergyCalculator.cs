using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GMap.NET;
using Newtonsoft.Json.Linq;

public class SolarCalculator
{
    private List<SolarPanel> _panels;
    private double _latitude;
    private double _longitude;
    private const double Pi = Math.PI;

    private const string CoordinatesFile = "coordinates.txt";
    private const string SunDataFile = "sun_data.csv";
    private const string WeatherDataFile = "weather_weekly.txt";
    private const string OutputFile = "energy_weekly.txt";

    public SolarCalculator(List<SolarPanel> panels)
    {
        _panels = panels.Where(p => p.IsChecked).ToList(); /// Учитываем только выбранные панели
        LoadCoordinatesFromFile();
    }

    /// Загружаем координаты из файла
    private void LoadCoordinatesFromFile()
    {
        if (!File.Exists(CoordinatesFile))
            throw new FileNotFoundException("Файл coordinates.txt не найден.");

        string[] lines = File.ReadAllLines(CoordinatesFile);
        foreach (string line in lines)
        {
            if (line.StartsWith("Широта:"))
                _latitude = double.Parse(line.Replace("Широта:", "").Trim(), CultureInfo.GetCultureInfo("ru-RU"));
            if (line.StartsWith("Долгота:"))
                _longitude = double.Parse(line.Replace("Долгота:", "").Trim(), CultureInfo.GetCultureInfo("ru-RU"));
        }
    }


    /// Вычисляем склонение солнца (B) по дню года
    private double CalculateSolarDeclination(int dayOfYear)
    {
        return 23.45 * Math.Sin((360.0 / 365.0) * (dayOfYear - 81) * Pi / 180);
    }

    /// Загружаем данные восхода, зенита и заката солнца
    private (int sunrise, int solarNoon, int sunset) LoadSunTimes(int dayOfYear)
    {
        if (!File.Exists(SunDataFile))
            throw new FileNotFoundException("Файл sun_data.csv не найден.");

        var lines = File.ReadAllLines(SunDataFile);
        foreach (var line in lines.Skip(1)) // Пропускаем заголовок
        {
            var parts = line.Split(';');
            if (parts.Length < 4) continue;

            if (DateTime.TryParseExact(parts[0], "dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                if (date.DayOfYear == dayOfYear)
                {
                    int sunrise = TimeToMinutes(parts[1]);
                    int solarNoon = TimeToMinutes(parts[2]);
                    int sunset = TimeToMinutes(parts[3]);
                    return (sunrise, solarNoon, sunset);
                }
            }
        }
        throw new Exception($"Нет данных о солнце для дня {dayOfYear}.");
    }

    /// Конвертируем строковое время "ЧЧ:ММ" в минуты
    private int TimeToMinutes(string time)
    {
        var parts = time.Split(':').Select(int.Parse).ToArray();
        return parts[0] * 60 + parts[1];
    }

    /// Загружаем облачность на неделю
    private double[] LoadCloudiness()
    {
        if (!File.Exists("weather_weekly.txt"))
            throw new FileNotFoundException("Файл weather_weekly.txt не найден.");

        string[] lines = File.ReadAllLines("weather_weekly.txt");

        double[] cloudiness = new double[7];
        int index = 0;

        for (int i = 1; i < lines.Length; i++) // Пропускаем заголовок
        {
            var parts = lines[i].Split(';');
            if (parts.Length < 2) continue;

            if (index < 7) // Берём только первые 7 дней
            {
                cloudiness[index] = double.Parse(parts[1].Trim().Replace(",", "."), CultureInfo.InvariantCulture);
                index++;
            }
        }

        if (index < 7)
            throw new Exception("Недостаточно данных о погоде в файле weather_weekly.txt.");
        

        return cloudiness;
    }

    /// Вычисляем выработку электроэнергии за неделю
    public void CalculateWeeklyProduction()
    {
        //DebugOutput();
        double[] cloudiness = LoadCloudiness();

        System.Diagnostics.Debug.WriteLine($"Облачность на неделю: {string.Join(", ", cloudiness.Select(c => c.ToString("F2")))}");

        if (_panels.Count == 0)
        {
            Console.WriteLine("Нет выбранных панелей для расчета.");
            return;
        }

        using (StreamWriter writer = new StreamWriter(OutputFile))
        {
            writer.WriteLine("Дата | Выработка (Вт⋅ч) | Потребление (Вт⋅ч) | Чистая энергия (Вт⋅ч)");

            for (int i = 0; i < 7; i++)
            {
                int dayOfYear = DateTime.Now.AddDays(i).DayOfYear;
                double declination = CalculateSolarDeclination(dayOfYear);
                (int sunrise, int solarNoon, int sunset) = LoadSunTimes(dayOfYear);

                System.Diagnostics.Debug.WriteLine($"День {i + 1}: склонение {declination:F2}, восход {sunrise}, зенит {solarNoon}, закат {sunset}");

                double totalProduction = 0;
                double totalConsumption = 0;

                foreach (var panel in _panels)
                {
                    double dailyProduction = CalculateDailyProduction(panel, declination, sunrise, solarNoon, sunset, cloudiness[i]);
                    totalProduction += dailyProduction;
                    totalConsumption += panel.ConsumptionPower * ((sunset - sunrise) / 60.0); // Вт⋅ч

                    System.Diagnostics.Debug.WriteLine($"Панель {panel.Type}: Выработка {dailyProduction:F2} Вт⋅ч, Потребление {panel.ConsumptionPower} Вт");
                }

                double netEnergy = totalProduction - totalConsumption;
                writer.WriteLine($"{DateTime.Now.AddDays(i):dd.MM.yyyy} | {totalProduction:F2} Вт⋅ч | {totalConsumption:F2} Вт⋅ч | {netEnergy:F2} Вт⋅ч");

                System.Diagnostics.Debug.WriteLine($"День {i + 1}: Общая выработка {totalProduction:F2} Вт⋅ч, Общее потребление {totalConsumption:F2} Вт⋅ч, Чистая энергия {netEnergy:F2} Вт⋅ч");
            }
        }
        string fullpathfile = Path.GetFullPath(OutputFile);
        Console.WriteLine($"Результаты сохранены в {Path.GetFullPath(OutputFile)}");
    }

    /// Вычисляем дневную выработку электроэнергии для панелей
    private double CalculateDailyProduction(SolarPanel panel, double declination, int sunrise, int solarNoon, int sunset, double cloudFactor)
    {

        double totalEnergy = 0.0;
        double kT = 1 - 0.75 * cloudFactor; // Коэффициент пропускания облаков

        for (int H = sunrise; H <= sunset; H++)
        {
            double hourAngle = (H - solarNoon) * (15 * Pi / 180);
            double elevation = Math.Asin(Math.Sin(declination * Pi / 180) * Math.Sin(_latitude * Pi / 180) +
                                         Math.Cos(declination * Pi / 180) * Math.Cos(_latitude * Pi / 180) * Math.Cos(hourAngle));

            if (panel.AngleVertical == null) continue;
            double panelTilt = panel.AngleVertical.Value * Pi / 180;

            // Новый расчет угла падения солнечных лучей
            double incidenceAngle = Math.Acos(
                Math.Sin(elevation) * Math.Sin(panelTilt) +
                Math.Cos(elevation) * Math.Cos(panelTilt)
            );

            if (incidenceAngle > Pi / 2) continue; // Если солнце за горизонтом

            double efficiency = 0.85; // КПД панели
            double power = panel.Power * Math.Cos(incidenceAngle) * kT * efficiency / 60; // Переводим в Вт⋅ч

            totalEnergy += power;
        }

        return totalEnergy;
    }


}
