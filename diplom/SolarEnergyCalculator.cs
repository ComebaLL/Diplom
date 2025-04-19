using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

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

    private const double Pi = Math.PI;

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

    private (double solarElevation, double solarAzimuth) CalculateSolarPosition(DateTime time, DateTime sunrise, DateTime sunset, DateTime solarNoon)
    {
        double dayLength = (sunset - sunrise).TotalMinutes;
        double timeSinceSunrise = (time - sunrise).TotalMinutes;

        double solarElevation = -90 + (timeSinceSunrise / dayLength) * 180;
        solarElevation = Math.Max(0, Math.Min(90, solarElevation));

        double solarAzimuth = 180 * (timeSinceSunrise / dayLength);

        Debug.WriteLine($"Дата: {time:yyyy-MM-dd HH:mm:ss}, Угол возвышения: {solarElevation}, Азимут: {solarAzimuth}");

        return (solarElevation, solarAzimuth);
    }

    private double CalculateHourlyProduction(SolarPanel panel, double cloudiness, double solarElevation, double solarAzimuth)
    {
        if (solarElevation <= 0) return 0;

        double angleVert = Convert.ToDouble(panel.AngleVertical);
        double angleHoriz = Convert.ToDouble(panel.AngleHorizontal);
        double iZ = Math.Abs(angleVert - solarElevation);
        double iA = Math.Abs(angleHoriz - solarAzimuth);
        double incidenceAngleDeg = Math.Sqrt(iA * iA + iZ * iZ); // в градусах
        double incidenceAngleRad = incidenceAngleDeg * (Math.PI / 180.0); // в радианах

        double cosIncidence = Math.Cos(incidenceAngleRad);
        double kT = 1 - 0.75 * (cloudiness / 100.0);
        double efficiency = 0.85;

        double rawPower = Convert.ToDouble(panel.Power);
        double power = rawPower * cosIncidence * kT * efficiency;
        power = Math.Max(0, power);

        Debug.WriteLine($"Панель: {panel.Type}");
        Debug.WriteLine($"  Мощность: {panel.Power} Вт");
        Debug.WriteLine($"  Углы панели: вертикальный = {panel.AngleVertical}, горизонтальный = {panel.AngleHorizontal}");
        Debug.WriteLine($"  Облачность: {cloudiness}% → kT: {kT:F3}");
        Debug.WriteLine($"  Углы отклонения: iA = {iA:F2}°, iZ = {iZ:F2}°, итог = {incidenceAngleDeg:F2}°");
        Debug.WriteLine($"  Угол падения (рад): {incidenceAngleRad:F4}, cos(i) = {cosIncidence:F4}");
        Debug.WriteLine($"  Выработка (до Max): {rawPower:F2} Вт, финально: {power:F2} Вт");
        Debug.WriteLine(new string('-', 60));

        return power;
    }




    public void CalculateEnergyProduction()
    {
        var weatherData = LoadWeatherData();

        using (StreamWriter writer = new StreamWriter(OutputFile, false))
        {
            writer.WriteLine("Дата и время | Выработка (Вт⋅ч) | Потребление (Вт⋅ч) | Чистая энергия (Вт⋅ч)");

            foreach (var (time, cloudiness, temperature) in weatherData)
            {
                int dayOfYear = time.Date.DayOfYear;
                if (!_sunData.ContainsKey(dayOfYear)) continue;

                var (sunrise, solarNoon, sunset) = _sunData[dayOfYear];

                DateTime sunriseTime = time.Date + sunrise;
                DateTime solarNoonTime = time.Date + solarNoon;
                DateTime sunsetTime = time.Date + sunset;

                Debug.WriteLine($"Дата: {time:yyyy-MM-dd HH:mm:ss}, Восход: {sunriseTime}, Закат: {sunsetTime}");

                if (time < sunriseTime || time > sunsetTime)
                {
                    writer.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | 0.00 | 0.00 | 0.00");
                    continue;
                }

                double totalProduction = 0;
                double totalConsumption = 0;

                //var (solarElevation, solarAzimuth) = CalculateSolarPosition(time, sunriseTime, sunsetTime, solarNoonTime);
                var (solarElevation, solarAzimuth) = CalculateScientificSolarPosition(time);


                foreach (var panel in _panels)
                {
                    double production = CalculateHourlyProduction(panel, cloudiness, solarElevation, solarAzimuth);
                    totalProduction += production;
                    totalConsumption += panel.ConsumptionPower;
                }

                double netEnergy = totalProduction - totalConsumption;
                writer.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | {totalProduction:F2} | {totalConsumption:F2} | {netEnergy:F2}");

                Debug.WriteLine($"Дата: {time:yyyy-MM-dd HH:mm:ss}, Производство: {totalProduction:F2}, Потребление: {totalConsumption:F2}, Чистая энергия: {netEnergy:F2}");
            }
        }

        Console.WriteLine($"Результаты сохранены в {Path.GetFullPath(OutputFile)}");
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

        Debug.WriteLine($"Загружены погодные данные: {weatherData.Count} записей");

        return weatherData;
    }

    /// расчёт положения солнца
    private (double Elevation, double Azimuth) CalculateScientificSolarPosition(DateTime time)
    {
        int dayOfYear = time.DayOfYear;
        double f = _latitude * (Math.PI / 180); // широта в радианах

        // Склонение солнца
        double b = 23.45 * Math.Sin((2 * Math.PI / 365) * (dayOfYear - 81));
        double bRad = b * (Math.PI / 180); // в радианах

        // Время
        double B = 2 * Math.PI * (dayOfYear - 81) / 364;
        double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);
        double solarTimeCorrection = 4 * (_longitude - 15 * TimeZoneInfo.Local.BaseUtcOffset.TotalHours) + EoT;
        double solarTime = time.TimeOfDay.TotalMinutes + solarTimeCorrection;

        double H = (solarTime - 720) * 0.25; // Часовой угол в градусах
        double Hrad = H * (Math.PI / 180);

        // Угол возвышения
        double elevation = Math.Asin(Math.Sin(bRad) * Math.Sin(f) + Math.Cos(bRad) * Math.Cos(f) * Math.Cos(Hrad));
        elevation = elevation * (180 / Math.PI);

        // Азимут
        double azimuth = Math.Acos((Math.Sin(bRad) * Math.Cos(f) - Math.Cos(bRad) * Math.Sin(f) * Math.Cos(Hrad)) / Math.Cos(elevation * Math.PI / 180));
        azimuth = H > 0 ? 180 + azimuth * (180 / Math.PI) : 180 - azimuth * (180 / Math.PI);
        azimuth = azimuth % 360;

        Debug.WriteLine($"  b = {b:F2}°, H = {H:F2}°, Elevation = {elevation:F2}°, Azimuth = {azimuth:F2}°");

        return (elevation, azimuth);
    }

}
