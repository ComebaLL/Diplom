using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

public class SolarCalculator
{
    private List<SolarPanel> _panels; // списко панелей
    private Dictionary<(int Month, int Day), (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)> _sunData
    = new Dictionary<(int Month, int Day), (TimeSpan, TimeSpan, TimeSpan)>();
    private Dictionary<(int Month, int Day), (double Cloudiness, double Temperature)> weatherData;
    private int _groupNumber;
    private const string WeatherDataFile = "yearly_weather_forecast.csv";
    private const string SunDataFile = "sun_data.csv";

    /// Конструктор класса SolarCalculator
    /// Принимает список солнечных панелей, отбирает только отмеченные (IsChecked или IsChecked2),
    /// загружает данные о солнце (восход, полдень, закат) из файлов.
    public SolarCalculator(List<SolarPanel> panels, int groupNumber = 1)
    {
        // Сохраняем только отмеченные (выбранные пользователем) панели
        //_panels = panels.Where(p => p.IsChecked).ToList();

        _groupNumber = groupNumber;
        // Учитываем выбранную группу панелей
        _panels = groupNumber switch
        {
            1 => panels.Where(p => p.IsChecked).ToList(),
            2 => panels.Where(p => p.IsChecked2).ToList(),
            _ => throw new ArgumentException("Недопустимый номер группы. Должен быть 1 или 2.")
        };

        // Инициализируем словарь с данными о солнце (ключ — день года,
        // значение — кортеж с временем восхода, солнечного полдня и заката)
        //_sunData = new Dictionary<int, (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)>();
        _sunData = new Dictionary<(int Month, int Day), (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)>();


        // Загружаем данные о восходе, полдне и закате солнца по дням года
        _sunData = LoadSunData();

        weatherData = LoadWeatherData();
    }


    /// Загружает данные о восходе, зените и закате солнца из файла sun_data.csv.

    private Dictionary<(int Month, int Day), (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)> LoadSunData()
    {
        if (!File.Exists(SunDataFile))
            throw new FileNotFoundException($"Файл {SunDataFile} не найден.");

        _sunData.Clear();

        string[] lines = File.ReadAllLines(SunDataFile);

        foreach (string line in lines.Skip(1))
        {
            var parts = line.Split(';');
            if (parts.Length < 4) continue;

            // Преобразуем дату без года (dd.MM)
            if (DateTime.TryParseExact(parts[0], "dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                int month = parsedDate.Month;
                int day = parsedDate.Day;

                if (TimeSpan.TryParse(parts[1], out TimeSpan sunrise) &&
                    TimeSpan.TryParse(parts[2], out TimeSpan solarNoon) &&
                    TimeSpan.TryParse(parts[3], out TimeSpan sunset))
                {
                    _sunData[(month, day)] = (sunrise, solarNoon, sunset);
                }
            }
        }

        return _sunData;
        // Debug.WriteLine($"Загружены данные по солнцу на {_sunData.Count} дней");
    }
    

    /// Загружает погодные данные из текстового файла, содержащего информацию по часам.
    private Dictionary<(int Month, int Day), (double Cloudiness, double Temperature)> LoadWeatherData()
    {
        if (!File.Exists(WeatherDataFile))
            throw new FileNotFoundException("Файл yearly_weather_forecast.csv не найден.");

        var lines = File.ReadAllLines(WeatherDataFile).Skip(1); // пропустить заголовок
        var weatherData = new Dictionary<(int Month, int Day), (double Cloudiness, double Temperature)>();

        foreach (var line in lines)
        {
            var parts = line.Split(',');

            if (parts.Length < 3) continue;

            // Парсим дату без года (в формате "MM-dd" или "dd-MM")
            if (DateTime.TryParseExact(parts[0], "MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate) ||
                DateTime.TryParseExact(parts[0], "dd-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                int month = parsedDate.Month;
                int day = parsedDate.Day;

                if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double temperature) &&
                    double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double cloudiness))
                {
                    weatherData[(month, day)] = (cloudiness, temperature);
                }
            }
        }

        return weatherData;
    }


    /// Вычисляет точное положение Солнца (угол возвышения и азимут) в заданный момент времени,
    /// на основе координат местности (_latitude, _longitude) и текущей даты и времени.
    /// Используются астрономические формулы, учитывающие склонение Солнца, уравнение времени и часовой угол.
    private (double Elevation, double Azimuth) CalculateScientificSolarPosition(DateTime time, double latitude, double longitude)
    {
        // День года (от 1 до 365)
        int dayOfYear = time.DayOfYear;

        // Широта в радианах
        double f = latitude * (Math.PI / 180);

        // Склонение Солнца (в градусах)
        double b = 23.45 * Math.Sin((2 * Math.PI / 365) * (dayOfYear - 81));

        // Склонение в радианах
        double bRad = b * (Math.PI / 180);

        // Вычисление уравнения времени (Equation of Time)
        double B = 2 * Math.PI * (dayOfYear - 81) / 364;
        double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);

        // Коррекция солнечного времени (в минутах), с учетом долготы и часового пояса
        double timeZoneOffset = TimeZoneInfo.Local.BaseUtcOffset.TotalHours;
        double solarTimeCorrection = 4 * (longitude - 15 * timeZoneOffset) + EoT;

        // Истинное солнечное время (в минутах)
        double solarTime = time.TimeOfDay.TotalMinutes + solarTimeCorrection;

        // Часовой угол (Hour Angle), 0° в солнечный полдень, 15° на каждый час
        double H = (solarTime - 720) * 0.25; // 720 минут — это 12:00
        double Hrad = H * (Math.PI / 180);

        // Угол возвышения Солнца (Elevation), в градусах
        double elevation = Math.Asin(Math.Sin(bRad) * Math.Sin(f) + Math.Cos(bRad) * Math.Cos(f) * Math.Cos(Hrad)) * (180 / Math.PI);

        // Азимут Солнца (Azimuth), в градусах
        double cosAzimuth = (Math.Sin(bRad) * Math.Cos(f) - Math.Cos(bRad) * Math.Sin(f) * Math.Cos(Hrad)) / Math.Cos(elevation * Math.PI / 180);
        cosAzimuth = Math.Clamp(cosAzimuth, -1.0, 1.0); // защита от погрешностей

        double azimuth = Math.Acos(cosAzimuth) * (180 / Math.PI);

        // Корректировка направления: если H > 0 (после полудня), азимут > 180°
        azimuth = H > 0 ? 180 + azimuth : 180 - azimuth;

        // Приведение азимута к диапазону [0, 360)
        azimuth = (azimuth + 360) % 360;

        return (elevation, azimuth);
    }



    /// Вычисляет значения cos(i), коэффициент облачности и эффективность панели
    /// i — угол отклонения солнечных лучей от нормали панели, вычисляется с использованием тангенсов
    private (double cosIncidence, double kT, double efficiency) GetBaseCalculationValues(double iA, double iZ, double cloudiness)
    {
        // Преобразуем углы из градусов в радианы
        double iARad = iA * Math.PI / 180.0;
        double iZRad = iZ * Math.PI / 180.0;

        // Вычисляем угол отклонения 
        double tanIA = Math.Tan(iARad);
        double tanIZ = Math.Tan(iZRad);
        double angleDeviationRad = Math.Atan(Math.Sqrt(tanIA * tanIA + tanIZ * tanIZ));

        // Вычисляем косинус угла отклонения
        double cosIncidence = Math.Cos(angleDeviationRad);

        // Учет облачности: 1 - 0.75 * (облачность в долях)
        double kT = 1 - 0.75 * (cloudiness / 100.0);

        // Эффективность преобразования панели (фиксированная)
        double efficiency = 0.85;

        return (cosIncidence, kT, efficiency);
    }



    /// Рассчитывает выработку электроэнергии от статичной солнечной панели 
    /// на основе ее ориентации, текущего положения солнца и облачности.
    private double CalculateStaticPanelProduction(SolarPanel panel, double cloudiness, double solarElevation, double solarAzimuth)
    {
        // Угол вертикального наклона панели (относительно горизонта)
        double angleVert = Convert.ToDouble(panel.AngleVertical);

        // Горизонтальный угол панели (азимут), например, юг — 180°
        double angleHoriz = Convert.ToDouble(panel.AngleHorizontal);

        // Разница между положением солнца и углами панели
        double iZ = Math.Abs(angleVert - solarElevation);
        double iA = Math.Abs(angleHoriz - solarAzimuth);

        // Получение базовых коэффициентов: косинус угла падения, прозрачность атмосферы и эффективность
        var (cosIncidence, kT, efficiency) = GetBaseCalculationValues(iA, iZ, cloudiness);

        // Номинальная мощность панели
        double rawPower = panel.Power;

        // Итоговая мощность с учетом угла падения, облачности и эффективности
        double power = rawPower * cosIncidence * kT * efficiency;

        // Мощность не может быть отрицательной
        return Math.Max(0, power);
    }


    /// Выполняет расчет суммарной выработки энергии статическими солнечными панелями 
    /// за указанный промежуток времени с учетом погодных условий и солнечного положения.
    /// Учитываются только панели, у которых установлены флаги IsChecked или IsChecked2.
    public double CalculateStaticPanelProductionForPeriod(DateTime startDate, DateTime endDate)
    {
        double totalProduction = 0;
        var lines = new List<string>();

        // Имя выходного файла — зависит от группы
        string outputFilePath = _groupNumber switch
        {
            1 => "group1_output.txt",
            2 => "group2_output.txt",
            _ => "unknown_group.txt"
        };

        // Добавляем заголовок
        lines.Add($"Результаты расчета для группы {_groupNumber}");
        lines.Add("");
        lines.Add($"Период: {startDate:dd.MM.yyyy} — {endDate:dd.MM.yyyy}");
        lines.Add("");

        for (DateTime day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            double dailyProduction = 0;

            // Значения по умолчанию
            TimeSpan sunrise = new(4, 0, 0);
            TimeSpan sunset = new(22, 0, 0);

            if (_sunData.TryGetValue((day.Month, day.Day), out var sunTimes))
            {
                sunrise = sunTimes.Sunrise;
                sunset = sunTimes.Sunset;
            }

            foreach (var panel in _panels.Where(p => p.Type == "Статическая"))
            {
                double latitude = Convert.ToDouble(panel.Latitude);
                double longitude = Convert.ToDouble(panel.Longitude);

                for (DateTime time = day.Date + sunrise; time <= day.Date + sunset; time = time.AddMinutes(30))
                {
                    var (elevation, azimuth) = CalculateScientificSolarPosition(time, latitude, longitude);
                    if (elevation <= 0)
                        continue;

                    if (!weatherData.TryGetValue((day.Month, day.Day), out var weather))
                        continue;

                    double cloudiness = weather.Cloudiness;
                    double power = CalculateStaticPanelProduction(panel, cloudiness, elevation, azimuth);
                    double energy30min = power * 0.5;

                    dailyProduction += energy30min;


                }
            }

            lines.Add($"{day:yyyy-MM-dd}: {dailyProduction:F2} кВт·ч");
            totalProduction += dailyProduction;

            Debug.WriteLine($"[STATIC] Дата: {day:yyyy-MM-dd}, Выработка: {dailyProduction:F2} кВт·ч");
            Debug.WriteLine($"[STATIC] Путь к файлу: {Path.GetFullPath(outputFilePath)}");


        }

        lines.Add("");
        lines.Add($"Общая выработка: {totalProduction:F2} кВт·ч");

        // Записываем в файл текущей группы 
        File.WriteAllLines(outputFilePath, lines);


        return totalProduction;
    }


    /// Рассчитывает выработку электроэнергии от солнечной панели с трекером (поворотным механизмом),
    /// учитывая текущее положение солнца, облачность и возможности поворота панели.
    private double CalculateTrackerPanelProduction(SolarPanel panel,double cloudiness,double solarElevation,double solarAzimuth,DateTime time,DateTime sunriseTime,
        DateTime sunsetTime)
    {
        // Если солнце под горизонтом — панель ничего не вырабатывает
        if (solarElevation <= 0) return 0;

        // Продолжительность светового дня
        TimeSpan totalDaylight = sunsetTime - sunriseTime;

        // Минут с начала восхода до текущего момента
        double minutesSinceSunrise = (time - sunriseTime).TotalMinutes;
        double totalMinutes = totalDaylight.TotalMinutes;

        // Шаг поворота панели по вертикали и горизонтали.
        // Больше количество поворотов → меньший шаг → точнее слежение
        double stepVert = panel.RotationVertical > 0 ? 90.0 / panel.RotationVertical : 90;
        double stepHoriz = panel.RotationHorizontal > 0 ? 90.0 / panel.RotationHorizontal : 90;

        // Расчет текущего положения панели по вертикали и горизонтали
        double angleVert = (minutesSinceSunrise / totalMinutes) * 90.0;
        double angleHoriz = (minutesSinceSunrise / totalMinutes) * 90.0;

        // 🔸 Округление углов панели до ближайшего значения в пределах допустимого шага
        angleVert = panel.RotationVertical > 0 ? Math.Round(angleVert / stepVert) * stepVert : 0;
        angleHoriz = panel.RotationHorizontal > 0 ? Math.Round(angleHoriz / stepHoriz) * stepHoriz : 0;

        // 🔸 Ограничение углов панели максимумом 90°
        angleVert = Math.Min(angleVert, 90);
        angleHoriz = Math.Min(angleHoriz, 90);

        // 🔸 Углы отклонения между направлением солнца и положением панели
        double iZ = Math.Abs(angleVert - solarElevation);
        double iA = Math.Abs(angleHoriz - solarAzimuth);

        // Расчет коэффициентов: угол падения, коэффициент прозрачности атмосферы, эффективность
        var (cosIncidence, kT, efficiency) = GetBaseCalculationValues(iA, iZ, cloudiness);

        // Расчет итоговой мощности
        double rawPower = panel.Power;
        double power = rawPower * cosIncidence * kT * efficiency;

        Debug.WriteLine($"[ТРЕКЕР] Панель {panel.Type} ({panel.Power} Вт × {panel.Count}) @ {time:HH:mm}");
        Debug.WriteLine($"  Установленные углы: V={angleVert:F1}°, H={angleHoriz:F1}°");
        Debug.WriteLine($"  Отклонения: ΔiZ={iZ:F1}°, ΔiA={iA:F1}° | cos(i)={cosIncidence:F3}");
        Debug.WriteLine($"  kT={kT:F3}, η={efficiency:F2} → Мощность: {power:F2} Вт");
        Debug.WriteLine(new string('-', 60));

        // Гарантируем, что мощность не отрицательная
        return Math.Max(0, power);
    }



    /// Выполняет расчет суммарной выработки энергии трекерными солнечными панелями 
    /// за указанный промежуток времени с учетом погодных условий и солнечного положения.
    /// Учитываются только панели, у которых установлены флаги IsChecked или IsChecked2.
    public double CalculateTrackerPanelProductionForPeriod(DateTime startDate, DateTime endDate)
    {
        double totalProduction = 0;
        double totalConsumption = 0;
        var lines = new List<string>();

        // Имя выходного файла — зависит от группы
        string outputFilePath = _groupNumber switch
        {
            1 => "group1_output.txt",
            2 => "group2_output.txt",
            _ => "unknown_group.txt"
        };

        for (DateTime day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            double dailyProduction = 0;
            double dailyConsumption = 0;

            TimeSpan sunrise = new TimeSpan(4, 0, 0);
            TimeSpan sunset = new TimeSpan(22, 0, 0);

            if (_sunData.TryGetValue((day.Month, day.Day), out var sunTimes))
            {
                sunrise = sunTimes.Sunrise;
                sunset = sunTimes.Sunset;
            }

            foreach (var panel in _panels.Where(p =>
                         p.Type == "Динамическая" &&
                         ((_groupNumber == 1 && p.IsChecked) || (_groupNumber == 2 && p.IsChecked2))))
            {
                double latitude = Convert.ToDouble(panel.Latitude);
                double longitude = Convert.ToDouble(panel.Longitude);

                DateTime sunriseTime = day + sunrise;
                DateTime sunsetTime = day + sunset;

                for (DateTime time = sunriseTime; time <= sunsetTime; time = time.AddMinutes(30))
                {
                    var (elevation, azimuth) = CalculateScientificSolarPosition(time, latitude, longitude);

                    if (elevation <= 0) continue;

                    if (!weatherData.TryGetValue((day.Month, day.Day), out var weather))
                        continue;

                    double cloudiness = weather.Cloudiness;

                    double power = CalculateTrackerPanelProduction(
                        panel,
                        cloudiness,
                        elevation,
                        azimuth,
                        time,
                        sunriseTime,
                        sunsetTime
                    );

                    double energy30min = power * 0.5;
                    dailyProduction += energy30min * panel.Count;

                    double consumption30min = panel.ConsumptionPower * 0.5;
                    dailyConsumption += consumption30min * panel.Count;
                }

                double returnEnergy = panel.ReturnEnergy * panel.Count;
                dailyConsumption += returnEnergy;
            }

            totalProduction += dailyProduction;
            totalConsumption += dailyConsumption;

            lines.Add($"{day:yyyy-MM-dd}: Производство = {dailyProduction:F2} кВт·ч, Потребление = {dailyConsumption:F2} кВт·ч");
        }

        lines.Add($"ИТОГО за период: Производство = {totalProduction:F2} кВт·ч, Потребление = {totalConsumption:F2} кВт·ч");

        File.WriteAllLines(outputFilePath, lines);
        Debug.WriteLine($"[ТРЕКЕРНЫЕ][Группа {_groupNumber}] Общая выработка: {totalProduction:F2} кВт·ч, Потребление: {totalConsumption:F2} кВт·ч");

        return totalProduction;
    }
}
