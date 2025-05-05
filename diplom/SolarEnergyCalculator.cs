using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

public class SolarCalculator
{
    private List<SolarPanel> _panels;
    private double _latitude;
    private double _longitude;
    private Dictionary<int, (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)> _sunData;

    private const string CoordinatesFile = "coordinates.txt";
    private const string WeatherDataFile = "weather_weekly.txt";
    private const string SunDataFile = "sun_data.csv";
    private const string OutputFile = "energy_weekly.txt";

    public SolarCalculator(List<SolarPanel> panels)
    {
        _panels = panels.Where(p => p.IsChecked).ToList();
        _sunData = new Dictionary<int, (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)>();
        LoadCoordinatesFromFile();
        LoadSunData();
    }

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

        Debug.WriteLine($"Загружены координаты: широта {_latitude}, долгота {_longitude}");
    }

    private void LoadSunData()
    {
        if (!File.Exists(SunDataFile))
            throw new FileNotFoundException($"Файл {SunDataFile} не найден.");

        _sunData.Clear();
        string[] lines = File.ReadAllLines(SunDataFile);

        foreach (string line in lines.Skip(1))
        {
            var parts = line.Split(';');
            if (parts.Length < 4) continue;

            if (DateTime.TryParseExact(parts[0] + ".2025", "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                int dayOfYear = date.DayOfYear;
                if (TimeSpan.TryParse(parts[1], out TimeSpan sunrise) &&
                    TimeSpan.TryParse(parts[2], out TimeSpan solarNoon) &&
                    TimeSpan.TryParse(parts[3], out TimeSpan sunset))
                {
                    _sunData[dayOfYear] = (sunrise, solarNoon, sunset);
                    Debug.WriteLine($"День {dayOfYear}: Восход {sunrise}, Зенит {solarNoon}, Закат {sunset}");
                }
            }
        }

        Debug.WriteLine($"Загружены данные по солнцу на {_sunData.Count} дней");
    }

    public void CalculateEnergyProduction(DateTime startDate, DateTime endDate)
    {
        var weatherData = LoadWeatherData()
            .Where(d => d.time.Date >= startDate.Date && d.time.Date <= endDate.Date)
            .ToList();

        string staticFile = "energy_static.txt";
        string trackerFile = "energy_tracker.txt";

        using (StreamWriter staticWriter = new StreamWriter(staticFile, false))
        using (StreamWriter trackerWriter = new StreamWriter(trackerFile, false))
        {
            staticWriter.WriteLine("Дата и время | Выработка (Вт⋅ч) | Потребление (Вт⋅ч) | Чистая энергия (Вт⋅ч)");
            trackerWriter.WriteLine("Дата и время | Выработка (Вт⋅ч) | Потребление (Вт⋅ч) | Чистая энергия (Вт⋅ч)");

            foreach (var (time, cloudiness, temperature) in weatherData)
            {
                int dayOfYear = time.Date.DayOfYear;
                if (!_sunData.ContainsKey(dayOfYear)) continue;

                var (sunrise, solarNoon, sunset) = _sunData[dayOfYear];
                DateTime sunriseTime = time.Date + sunrise;
                DateTime sunsetTime = time.Date + sunset;

                if (time < sunriseTime || time > sunsetTime)
                {
                    Debug.WriteLine($"[Пропуск] {time:yyyy-MM-dd HH:mm:ss} вне диапазона: {sunriseTime:HH:mm} - {sunsetTime:HH:mm}");
                    staticWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | 0.00 | 0.00 | 0.00");
                    trackerWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | 0.00 | 0.00 | 0.00");
                    continue;
                }

                var (solarElevation, solarAzimuth) = CalculateScientificSolarPosition(time);

                double staticProduction = 0;
                double staticConsumption = 0;

                double trackerProduction = 0;
                double trackerConsumption = 0;

                foreach (var panel in _panels)
                {
                    if (panel.Type == "Динамическая")
                    {
                        double p = CalculateTrackerPanelProduction(panel, cloudiness, solarElevation, solarAzimuth, time, sunriseTime, sunsetTime);
                        trackerProduction += p * panel.Count;
                        trackerConsumption += panel.ConsumptionPower * panel.Count;

                    }
                    else
                    {
                        double p = CalculateStaticPanelProduction(panel, cloudiness, solarElevation, solarAzimuth);
                        staticProduction += p * panel.Count;
                        staticConsumption += panel.ConsumptionPower * panel.Count;
                    }
                }

                
                if (Math.Abs((time - sunsetTime).TotalMinutes) < 1)
                {
                    foreach (var panel in _panels.Where(p => p.Type == "Динамическая"))
                    {
                        double returnEnergy = 0.1 * panel.ConsumptionPower * (panel.RotationVertical + panel.RotationHorizontal);
                        returnEnergy *= panel.Count;
                        trackerConsumption += returnEnergy;

                        Debug.WriteLine($"[ВОЗВРАТ] Панель: {panel.Type} | Энергия на возврат: {returnEnergy:F2} Вт");
                    }
                }

                double staticNet = staticProduction - staticConsumption;
                double trackerNet = trackerProduction - trackerConsumption;

                staticWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | {staticProduction:F2} | {staticConsumption:F2} | {staticNet:F2}");
                trackerWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | {trackerProduction:F2} | {trackerConsumption:F2} | {trackerNet:F2}");

                Debug.WriteLine($"Дата: {time:yyyy-MM-dd HH:mm:ss} | Static = {staticProduction:F2}, Tracker = {trackerProduction:F2}");
            }
        }

        Console.WriteLine($"Результаты сохранены в:\n - {Path.GetFullPath(staticFile)}\n - {Path.GetFullPath(trackerFile)}");
    }



    private List<(DateTime time, double cloudiness, double temperature)> LoadWeatherData()
    {
        if (!File.Exists(WeatherDataFile))
            throw new FileNotFoundException("Файл weather_weekly.txt не найден.");

        var lines = File.ReadAllLines(WeatherDataFile).Skip(1);
        var weatherData = new List<(DateTime, double, double)>();

        foreach (var line in lines)
        {
            var parts = line.Split(';');
            if (parts.Length < 3) continue;

            DateTime time = DateTime.ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            double cloudiness = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double temperature = double.Parse(parts[2], CultureInfo.InvariantCulture);

            weatherData.Add((time, cloudiness, temperature));
        }

        return weatherData;
    }

    private (double Elevation, double Azimuth) CalculateScientificSolarPosition(DateTime time)
    {
        int dayOfYear = time.DayOfYear;
        double f = _latitude * (Math.PI / 180); // широта в радианах

        double b = 23.45 * Math.Sin((2 * Math.PI / 365) * (dayOfYear - 81));
        double bRad = b * (Math.PI / 180);

        double B = 2 * Math.PI * (dayOfYear - 81) / 364;
        double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);
        double solarTimeCorrection = 4 * (_longitude - 15 * TimeZoneInfo.Local.BaseUtcOffset.TotalHours) + EoT;
        double solarTime = time.TimeOfDay.TotalMinutes + solarTimeCorrection;

        double H = (solarTime - 720) * 0.25;
        double Hrad = H * (Math.PI / 180);

        double elevation = Math.Asin(Math.Sin(bRad) * Math.Sin(f) + Math.Cos(bRad) * Math.Cos(f) * Math.Cos(Hrad)) * (180 / Math.PI);
        double azimuth = Math.Acos((Math.Sin(bRad) * Math.Cos(f) - Math.Cos(bRad) * Math.Sin(f) * Math.Cos(Hrad)) / Math.Cos(elevation * Math.PI / 180));
        azimuth = H > 0 ? 180 + azimuth * (180 / Math.PI) : 180 - azimuth * (180 / Math.PI);
        azimuth = azimuth % 360;

        return (elevation, azimuth);
    }

    private (double cosIncidence, double kT, double efficiency) GetBaseCalculationValues(double iA, double iZ, double cloudiness)
    {
        double incidenceAngleDeg = Math.Sqrt(iA * iA + iZ * iZ);
        double incidenceAngleRad = incidenceAngleDeg * (Math.PI / 180.0);
        double cosIncidence = Math.Cos(incidenceAngleRad);
        double kT = 1 - 0.75 * (cloudiness / 100.0);
        double efficiency = 0.85;

        return (cosIncidence, kT, efficiency);
    }

    private double CalculateStaticPanelProduction(SolarPanel panel, double cloudiness, double solarElevation, double solarAzimuth)
    {
        double angleVert = Convert.ToDouble(panel.AngleVertical);
        double angleHoriz = Convert.ToDouble(panel.AngleHorizontal);
        double iZ = Math.Abs(angleVert - solarElevation);
        double iA = Math.Abs(angleHoriz - solarAzimuth);

        var (cosIncidence, kT, efficiency) = GetBaseCalculationValues(iA, iZ, cloudiness);

        double rawPower = panel.Power;
        double power = rawPower * cosIncidence * kT * efficiency;
        return Math.Max(0, power);
    }

    private double CalculateTrackerPanelProduction(SolarPanel panel, double cloudiness, double solarElevation, double solarAzimuth, DateTime time, DateTime sunriseTime, DateTime sunsetTime)
    {
        if (solarElevation <= 0) return 0;

        TimeSpan totalDaylight = sunsetTime - sunriseTime;
        double minutesSinceSunrise = (time - sunriseTime).TotalMinutes;
        double totalMinutes = totalDaylight.TotalMinutes;

        // 🔸 Порог отклонения — чем больше поворотов, тем точнее панель следует за солнцем
        double stepVert = panel.RotationVertical > 0 ? 90.0 / panel.RotationVertical : 90;
        double stepHoriz = panel.RotationHorizontal > 0 ? 90.0 / panel.RotationHorizontal : 90;

        // 🔸 Угол текущей установки панели (накопительный поворот)
        double angleVert = (minutesSinceSunrise / totalMinutes) * 90.0;
        double angleHoriz = (minutesSinceSunrise / totalMinutes) * 90.0;

        angleVert = panel.RotationVertical > 0 ? Math.Round(angleVert / stepVert) * stepVert : 0;
        angleHoriz = panel.RotationHorizontal > 0 ? Math.Round(angleHoriz / stepHoriz) * stepHoriz : 0;

        angleVert = Math.Min(angleVert, 90);
        angleHoriz = Math.Min(angleHoriz, 90);

        // 🔸 Углы отклонения
        double iZ = Math.Abs(angleVert - solarElevation);
        double iA = Math.Abs(angleHoriz - solarAzimuth);

        var (cosIncidence, kT, efficiency) = GetBaseCalculationValues(iA, iZ, cloudiness);
        double rawPower = panel.Power;
        double power = rawPower * cosIncidence * kT * efficiency;

        Debug.WriteLine($"[ТРЕКЕР] Панель {panel.Type} ({panel.Power} Вт × {panel.Count}) @ {time:HH:mm}");
        Debug.WriteLine($"  Установленные углы: V={angleVert:F1}°, H={angleHoriz:F1}°");
        Debug.WriteLine($"  Отклонения: ΔiZ={iZ:F1}°, ΔiA={iA:F1}° | cos(i)={cosIncidence:F3}");
        Debug.WriteLine($"  kT={kT:F3}, η={efficiency:F2} → Мощность: {power:F2} Вт");
        Debug.WriteLine(new string('-', 60));

        return Math.Max(0, power);
    }

    private List<(DateTime date, double cloudiness)> LoadYearlyWeatherData()
    {
        const string YearlyWeatherFile = "yearly_weather_forecast.csv";

        if (!File.Exists(YearlyWeatherFile))
            throw new FileNotFoundException($"Файл {YearlyWeatherFile} не найден.");

        var weatherData = new List<(DateTime, double)>();

        using (TextFieldParser parser = new TextFieldParser(YearlyWeatherFile))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;

            parser.ReadLine(); // пропускаем заголовок

            while (!parser.EndOfData)
            {
                var parts = parser.ReadFields();
                if (parts.Length < 3) continue;

                string dateStr = parts[0];
                string cloudinessStr = parts[2];

                if (!DateTime.TryParseExact(dateStr, "MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    continue;

                if (!double.TryParse(cloudinessStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cloudiness))
                    continue;

                date = new DateTime(2025, date.Month, date.Day);
                weatherData.Add((date, cloudiness));
            }
        }

        return weatherData;
    }

    public void CalculateStaticPanelProductionForPeriod(string period)
    {
        var weatherData = LoadYearlyWeatherData();
        string outputFile = period == "Год" ? "energy_static_year.txt" : "energy_static_month.txt";

        using (StreamWriter writer = new StreamWriter(outputFile, false))
        {
            writer.WriteLine("Дата | Выработка (Вт⋅ч) | Потребление (Вт⋅ч)");

            double totalEnergy = 0;
            double totalConsumption = 0;

            foreach (var (date, cloudiness) in weatherData)
            {
                if (period == "Месяц" && date.Month != DateTime.Now.Month)
                    continue;

                int dayOfYear = date.DayOfYear;
                if (!_sunData.ContainsKey(dayOfYear))
                    continue;

                var (sunrise, _, sunset) = _sunData[dayOfYear];
                DateTime sunriseTime = date.Date + sunrise;
                DateTime sunsetTime = date.Date + sunset;

                double dailyProduction = 0;

                for (DateTime time = sunriseTime; time <= sunsetTime; time = time.AddMinutes(30))
                {
                    var (solarElevation, solarAzimuth) = CalculateScientificSolarPosition(time);
                    if (solarElevation <= 0) continue;

                    foreach (var panel in _panels.Where(p => p.Type == "Статическая"))
                    {
                        double energy = CalculateStaticPanelProduction(panel, cloudiness, solarElevation, solarAzimuth);
                        dailyProduction += energy * panel.Count * 0.5;
                    }
                }

                // Расчёт суточного потребления
                double dailyConsumption = _panels
                    .Where(p => p.Type == "Статическая")
                    .Sum(p => p.ConsumptionPower * p.Count * (sunsetTime - sunriseTime).TotalHours);

                totalEnergy += dailyProduction;
                totalConsumption += dailyConsumption;

                writer.WriteLine($"{date:yyyy-MM-dd} | {dailyProduction:F2} | {dailyConsumption:F2}");
            }

            writer.WriteLine();
            writer.WriteLine($"Итоговая выработка за {period}: {totalEnergy:F2} Вт⋅ч");
            writer.WriteLine($"Итоговое потребление за {period}: {totalConsumption:F2} Вт⋅ч");
        }

        Console.WriteLine($"Результаты сохранены в {Path.GetFullPath(outputFile)}");
    }

    public void CalculateTrackerPanelProductionForPeriod(string period)
    {
        var weatherData = LoadYearlyWeatherData();
        string outputFile = period == "Год" ? "energy_tracker_year.txt" : "energy_tracker_month.txt";

        using (StreamWriter writer = new StreamWriter(outputFile, false))
        {
            writer.WriteLine("Дата | Выработка (Вт⋅ч) | Потребление (Вт⋅ч)");

            double totalEnergy = 0;
            double totalConsumption = 0;

            foreach (var (date, cloudiness) in weatherData)
            {
                if (period == "Месяц" && date.Month != DateTime.Now.Month)
                    continue;

                int dayOfYear = date.DayOfYear;
                if (!_sunData.ContainsKey(dayOfYear))
                    continue;

                var (sunrise, _, sunset) = _sunData[dayOfYear];
                DateTime sunriseTime = date.Date + sunrise;
                DateTime sunsetTime = date.Date + sunset;

                double dailyProduction = 0;
                double dailyConsumption = 0;

                for (DateTime time = sunriseTime; time <= sunsetTime; time = time.AddMinutes(30))
                {
                    var (solarElevation, solarAzimuth) = CalculateScientificSolarPosition(time);
                    if (solarElevation <= 0) continue;

                    foreach (var panel in _panels.Where(p => p.Type == "Динамическая"))
                    {
                        double energy = CalculateTrackerPanelProduction(panel, cloudiness, solarElevation, solarAzimuth, time, sunriseTime, sunsetTime);
                        dailyProduction += energy * panel.Count * 0.5;

                        double hourlyConsumption = panel.ConsumptionPower * panel.Count * 0.5; // 30 мин
                        dailyConsumption += hourlyConsumption;
                    }
                }

                // 🔸 Энергия на возврат в конце дня
                foreach (var panel in _panels.Where(p => p.Type == "Динамическая"))
                {
                    double returnEnergy = 0.1 * panel.ConsumptionPower * (panel.RotationVertical + panel.RotationHorizontal) * panel.Count;
                    dailyConsumption += returnEnergy;
                }

                totalEnergy += dailyProduction;
                totalConsumption += dailyConsumption;

                writer.WriteLine($"{date:yyyy-MM-dd} | {dailyProduction:F2} | {dailyConsumption:F2}");
            }

            writer.WriteLine();
            writer.WriteLine($"Итоговая выработка за {period}: {totalEnergy:F2} Вт⋅ч");
            writer.WriteLine($"Итоговое потребление за {period}: {totalConsumption:F2} Вт⋅ч");
        }

        Console.WriteLine($"Результаты сохранены в {Path.GetFullPath(outputFile)}");
    }

}
