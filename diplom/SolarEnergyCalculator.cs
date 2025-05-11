using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;
//using OfficeOpenXml;
//using OfficeOpenXml.Style;
using ClosedXML;

public class SolarCalculator
{
    private List<SolarPanel> _panels; // списко панелей
    private double _latitude; // широта
    private double _longitude; // долгота
    private Dictionary<int, (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)> _sunData; // словарь с восходом\закатом солнца

    private const string CoordinatesFile = "coordinates.txt";
    private const string WeatherDataFile = "weather_weekly.txt";
    private const string SunDataFile = "sun_data.csv";
    private const string OutputFile = "energy_weekly.txt";

    /// Конструктор класса SolarCalculator
    /// Принимает список солнечных панелей, отбирает только отмеченные (IsChecked),
    /// загружает координаты и данные о солнце (восход, полдень, закат) из файлов.
    public SolarCalculator(List<SolarPanel> panels)
    {
        // Сохраняем только отмеченные (выбранные пользователем) панели
        _panels = panels.Where(p => p.IsChecked).ToList();

        // Инициализируем словарь с данными о солнце (ключ — день года,
        // значение — кортеж с временем восхода, солнечного полдня и заката)
        _sunData = new Dictionary<int, (TimeSpan Sunrise, TimeSpan SolarNoon, TimeSpan Sunset)>();

        // Загружаем координаты местоположения (широту и долготу) из файла
        LoadCoordinatesFromFile();

        // Загружаем данные о восходе, полдне и закате солнца по дням года
        LoadSunData();
    }


    /// Загружает координаты широты и долготы из текстового файла coordinates.txt.

    private void LoadCoordinatesFromFile()
    {
        // Проверка: существует ли файл с координатами
        if (!File.Exists(CoordinatesFile))
            throw new FileNotFoundException("Файл coordinates.txt не найден.");

        // Чтение всех строк из файла
        string[] lines = File.ReadAllLines(CoordinatesFile);

        // Проходим по строкам и ищем те, что начинаются с нужных ключевых слов
        foreach (string line in lines)
        {
            // Если строка начинается со слова "Широта:", извлекаем значение и преобразуем в double
            if (line.StartsWith("Широта:"))
                _latitude = double.Parse(line.Replace("Широта:", "").Trim(), CultureInfo.GetCultureInfo("ru-RU"));

            // Если строка начинается со слова "Долгота:", извлекаем значение и преобразуем в double
            if (line.StartsWith("Долгота:"))
                _longitude = double.Parse(line.Replace("Долгота:", "").Trim(), CultureInfo.GetCultureInfo("ru-RU"));
        }

        // Debug.WriteLine($"Загружены координаты: широта {_latitude}, долгота {_longitude}");
    }


    /// Загружает данные о восходе, зените и закате солнца из файла sun_data.csv.

    private void LoadSunData()
    {
        // Проверка: существует ли файл с данными о солнце
        if (!File.Exists(SunDataFile))
            throw new FileNotFoundException($"Файл {SunDataFile} не найден.");

        // Очищаем словарь перед загрузкой новых данных
        _sunData.Clear();

        // Считываем все строки из файла
        string[] lines = File.ReadAllLines(SunDataFile);

        // Пропускаем первую строку (заголовки), обрабатываем остальные
        foreach (string line in lines.Skip(1))
        {
            // Разделяем строку по символу `;`
            var parts = line.Split(';');
            if (parts.Length < 4) continue; // Пропускаем строку, если в ней недостаточно данных

            // Добавляем ".2025" к дате, чтобы получить полный формат и распарсить в DateTime
            if (DateTime.TryParseExact(parts[0] + ".2025", "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                int dayOfYear = date.DayOfYear; // Получаем номер дня в году (от 1 до 365/366)

                // Парсим время восхода, зенита и заката
                if (TimeSpan.TryParse(parts[1], out TimeSpan sunrise) &&
                    TimeSpan.TryParse(parts[2], out TimeSpan solarNoon) &&
                    TimeSpan.TryParse(parts[3], out TimeSpan sunset))
                {
                    // Добавляем данные в словарь по дню года
                    _sunData[dayOfYear] = (sunrise, solarNoon, sunset);

                    // Debug.WriteLine($"День {dayOfYear}: Восход {sunrise}, Зенит {solarNoon}, Закат {sunset}");
                }
            }
        }

        // Debug.WriteLine($"Загружены данные по солнцу на {_sunData.Count} дней");
    }


    /// Выполняет расчет выработки и потребления энергии солнечными панелями в заданном диапазоне дат.
    public void CalculateEnergyProduction(DateTime startDate, DateTime endDate)
    {
        // Загружаем погодные данные и отбираем те, которые попадают в указанный диапазон дат
        var weatherData = LoadWeatherData()
            .Where(d => d.time.Date >= startDate.Date && d.time.Date <= endDate.Date)
            .OrderBy(d => d.time)
            .ToList();

        // Имена файлов для вывода результатов
        string staticFile = "energy_static.txt";
        string trackerFile = "energy_tracker.txt";

        // Создаем два потока записи в файлы
        using (StreamWriter staticWriter = new StreamWriter(staticFile, false))
        using (StreamWriter trackerWriter = new StreamWriter(trackerFile, false))
        {
            // Заголовки таблиц
            staticWriter.WriteLine("Дата и время | Выработка (Вт⋅ч) | Потребление | Чистая энергия (Вт⋅ч)");
            trackerWriter.WriteLine("Дата и время | Выработка (Вт⋅ч) | Потребление | Чистая энергия (Вт⋅ч)");

            // Перебираем каждый день в диапазоне
            for (DateTime day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
            {
                int dayOfYear = day.DayOfYear;
                // Пропускаем день, если нет данных о солнце
                if (!_sunData.ContainsKey(dayOfYear)) continue;

                // Получаем время восхода, зенита и заката
                var (sunrise, solarNoon, sunset) = _sunData[dayOfYear];
                DateTime sunriseTime = day + sunrise;
                DateTime sunsetTime = day + sunset;

                // Перебираем каждый час суток
                for (int hour = 0; hour < 24; hour++)
                {
                    DateTime time = new DateTime(day.Year, day.Month, day.Day, hour, 0, 0);

                    // Пропускаем часы до восхода и после заката
                    if (time < sunriseTime || time > sunsetTime)
                    {
                        Debug.WriteLine($"[Пропуск] {time:yyyy-MM-dd HH:mm:ss} вне диапазона: {sunriseTime:HH:mm} - {sunsetTime:HH:mm}");
                        staticWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | 0.00 | 0.00 | 0.00");
                        trackerWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | 0.00 | 0.00 | 0.00");
                        continue;
                    }

                    // Ищем ближайшие погодные данные к текущему часу
                    var closestWeather = weatherData
                        .OrderBy(d => Math.Abs((d.time - time).TotalMinutes))
                        .FirstOrDefault();

                    if (closestWeather == default) continue;

                    // Получаем облачность и температуру
                    var (cloudiness, temperature) = (closestWeather.cloudiness, closestWeather.temperature);

                    // Вычисляем солнечные углы: возвышение и азимут
                    var (solarElevation, solarAzimuth) = CalculateScientificSolarPosition(time);

                    // Обнуляем переменные для накопления выработки и потребления
                    double staticProduction = 0, staticConsumption = 0;
                    double trackerProduction = 0, trackerConsumption = 0;

                    // Перебираем все панели
                    foreach (var panel in _panels)
                    {
                        if (panel.Type == "Динамическая")
                        {
                            // Расчет для динамической панели
                            double p = CalculateTrackerPanelProduction(panel, cloudiness, solarElevation, solarAzimuth, time, sunriseTime, sunsetTime);
                            trackerProduction += p * panel.Count;
                            trackerConsumption += panel.ConsumptionPower * panel.Count;
                        }
                        else
                        {
                            // Расчет для статической панели
                            double p = CalculateStaticPanelProduction(panel, cloudiness, solarElevation, solarAzimuth);
                            staticProduction += p * panel.Count;
                            staticConsumption += panel.ConsumptionPower * panel.Count;
                        }
                    }

                    // На закате учитываем возврат панели в исходное положение (энергия тратится)
                    if (Math.Abs((time - sunsetTime).TotalMinutes) < 1)
                    {
                        foreach (var panel in _panels.Where(p => p.Type == "Динамическая"))
                        {
                            // Энергия на возврат пропорциональна числу поворотов
                            double returnEnergy = 0.1 * panel.ConsumptionPower * (panel.RotationVertical + panel.RotationHorizontal);
                            returnEnergy *= panel.Count;
                            trackerConsumption += returnEnergy;

                            Debug.WriteLine($"[ВОЗВРАТ] Панель: {panel.Type} | Энергия на возврат: {returnEnergy:F2} Вт");
                        }
                    }

                    // Чистая энергия = выработка - потребление
                    double staticNet = staticProduction - staticConsumption;
                    double trackerNet = trackerProduction - trackerConsumption;

                    // Запись в файл
                    staticWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | {staticProduction:F2} | {staticConsumption:F2} | {staticNet:F2}");
                    trackerWriter.WriteLine($"{time:yyyy-MM-dd HH:mm:ss} | {trackerProduction:F2} | {trackerConsumption:F2} | {trackerNet:F2}");

                    Debug.WriteLine($"Дата: {time:yyyy-MM-dd HH:mm:ss} | Static = {staticProduction:F2}, Tracker = {trackerProduction:F2}");
                }
            }
        }

        Console.WriteLine($"Результаты сохранены в:\n - {Path.GetFullPath(staticFile)}\n - {Path.GetFullPath(trackerFile)}");
    }



    /// Загружает погодные данные из текстового файла, содержащего информацию по часам.
    private List<(DateTime time, double cloudiness, double temperature)> LoadWeatherData()
    {
        // Проверяем наличие файла, если нет — выбрасываем исключение
        if (!File.Exists(WeatherDataFile))
            throw new FileNotFoundException("Файл weather_weekly.txt не найден.");

        // Читаем все строки, пропуская первую строку (обычно заголовок)
        var lines = File.ReadAllLines(WeatherDataFile).Skip(1);

        // Список для хранения результатов
        var weatherData = new List<(DateTime, double, double)>();

        // Обрабатываем каждую строку
        foreach (var line in lines)
        {
            // Разделяем строку по символу ';'
            var parts = line.Split(';');

            // Пропускаем строки, где недостаточно данных
            if (parts.Length < 3) continue;

            // Парсим дату и время в формате "yyyy-MM-dd HH:mm:ss"
            DateTime time = DateTime.ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Парсим значения облачности и температуры
            double cloudiness = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double temperature = double.Parse(parts[2], CultureInfo.InvariantCulture);

            // Добавляем данные в результирующий список
            weatherData.Add((time, cloudiness, temperature));
        }

        // Возвращаем собранные данные
        return weatherData;
    }



    /// Вычисляет научно точное положение Солнца (угол возвышения и азимут) в заданный момент времени,
    /// на основе координат местности (_latitude, _longitude) и текущей даты и времени.
    /// Используются астрономические формулы, учитывающие склонение Солнца, уравнение времени и часовой угол.
    private (double Elevation, double Azimuth) CalculateScientificSolarPosition(DateTime time)
    {
        // День года (от 1 до 365)
        int dayOfYear = time.DayOfYear;

        // Широта в радианах
        double f = _latitude * (Math.PI / 180);

        // Склонение Солнца (в градусах)
        double b = 23.45 * Math.Sin((2 * Math.PI / 365) * (dayOfYear - 81));

        // Склонение в радианах
        double bRad = b * (Math.PI / 180);

        // Вычисление уравнения времени (Equation of Time)
        double B = 2 * Math.PI * (dayOfYear - 81) / 364;
        double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);

        // Коррекция солнечного времени (в минутах), с учетом долготы и часового пояса
        double solarTimeCorrection = 4 * (_longitude - 15 * TimeZoneInfo.Local.BaseUtcOffset.TotalHours) + EoT;

        // Истинное солнечное время (в минутах)
        double solarTime = time.TimeOfDay.TotalMinutes + solarTimeCorrection;

        // Часовой угол (Hour Angle), 0° в солнечный полдень, 15° на каждый час
        double H = (solarTime - 720) * 0.25; // 720 минут — это 12:00
        double Hrad = H * (Math.PI / 180);

        // Угол возвышения Солнца (Elevation), в градусах
        double elevation = Math.Asin(Math.Sin(bRad) * Math.Sin(f) + Math.Cos(bRad) * Math.Cos(f) * Math.Cos(Hrad)) * (180 / Math.PI);

        // Азимут Солнца (Azimuth), в градусах
        double azimuth = Math.Acos(
            (Math.Sin(bRad) * Math.Cos(f) - Math.Cos(bRad) * Math.Sin(f) * Math.Cos(Hrad)) /
            Math.Cos(elevation * Math.PI / 180)
        );

        // Преобразование азимута в градусы и корректировка направления:
        // если Солнце находится после полудня (H > 0), азимут > 180°
        azimuth = H > 0 ? 180 + azimuth * (180 / Math.PI) : 180 - azimuth * (180 / Math.PI);

        // Приведение азимута к диапазону [0, 360)
        azimuth = azimuth % 360;

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


    /// Рассчитывает выработку электроэнергии от солнечной панели с трекером (поворотным механизмом),
    /// учитывая текущее положение солнца, облачность и возможности поворота панели.
    private double CalculateTrackerPanelProduction(
        SolarPanel panel,
        double cloudiness,
        double solarElevation,
        double solarAzimuth,
        DateTime time,
        DateTime sunriseTime,
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



    /// Загружает данные о погоде на год из CSV-файла <c>yearly_weather_forecast.csv</c>.
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

                // Преобразуем дату в формате MM-dd
                if (!DateTime.TryParseExact(dateStr, "MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    continue;

                // Преобразуем облачность (в процентах)
                if (!double.TryParse(cloudinessStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double cloudiness))
                    continue;

                // Устанавливаем год 2025 для унификации
                date = new DateTime(2025, date.Month, date.Day);
                weatherData.Add((date, cloudiness));
            }
        }

        return weatherData;
    }


    /// Выполняет расчет выработки и потребления энергии для всех статических панелей
    /// за указанный период времени и сохраняет данные в Excel-файл с новым листом.
    public void CalculateStaticPanelProductionForPeriod(string period)
    {
        var weatherData = LoadYearlyWeatherData();
        string outputFile = "energy_static_month_year.xlsx";
        string sheetName = $"Статика_{period}_{DateTime.Now:HHmmss}";

        using (var workbook = File.Exists(outputFile) ? new XLWorkbook(outputFile) : new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).Value = "Дата";
            worksheet.Cell(1, 2).Value = "Выработка (Вт⋅ч)";
            worksheet.Cell(1, 3).Value = "Потребление (Вт⋅ч)";

            // Добавим параметры панелей
            worksheet.Cell(1, 5).Value = "Характеристики панелей:";
            int rowInfo = 2;
            foreach (var panel in _panels.Where(p => p.Type == "Статическая"))
            {
                worksheet.Cell(rowInfo++, 5).Value =
                    $"Мощность: {panel.Power} Вт | Углы: V={panel.AngleVertical}°, H={panel.AngleHorizontal}° | Кол-во: {panel.Count}";
            }

            double totalEnergy = 0;
            double totalConsumption = 0;
            int row = 2;

            foreach (var (date, cloudiness) in weatherData)
            {
                if (period == "Месяц" && date.Month != DateTime.Now.Month)
                    continue;

                int dayOfYear = date.DayOfYear;
                if (!_sunData.ContainsKey(dayOfYear)) continue;

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
                        dailyProduction += energy * panel.Count * 0.5; // 30 минут
                    }
                }

                double dailyConsumption = _panels
                    .Where(p => p.Type == "Статическая")
                    .Sum(p => p.ConsumptionPower * p.Count * (sunsetTime - sunriseTime).TotalHours);

                totalEnergy += dailyProduction;
                totalConsumption += dailyConsumption;

                worksheet.Cell(row, 1).Value = date.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = Math.Round(dailyProduction, 2);
                worksheet.Cell(row, 3).Value = Math.Round(dailyConsumption, 2);
                row++;
            }

            worksheet.Cell(row + 1, 1).Value = $"Итоговая выработка: {totalEnergy:F2} Вт⋅ч";
            worksheet.Cell(row + 2, 1).Value = $"Итоговое потребление: {totalConsumption:F2} Вт⋅ч";

            workbook.SaveAs(outputFile);
            Console.WriteLine($"Статические данные сохранены в Excel: {Path.GetFullPath(outputFile)}");
        }
    }





    /// Выполняет расчет выработки и потребления энергии для всех динамических панелей
    /// за указанный период времени и сохраняет данные в Excel-файл с новым листом.
    public void CalculateTrackerPanelProductionForPeriod(string period)
    {
        var weatherData = LoadYearlyWeatherData();
        string outputFile = "energy_tracker_month_year.xlsx";
        string sheetName = $"Трекер_{period}_{DateTime.Now:HHmmss}";

        using (var workbook = File.Exists(outputFile) ? new XLWorkbook(outputFile) : new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).Value = "Дата";
            worksheet.Cell(1, 2).Value = "Выработка (Вт⋅ч)";
            worksheet.Cell(1, 3).Value = "Потребление (Вт⋅ч)";

            worksheet.Cell(1, 5).Value = "Характеристики панелей:";
            int rowInfo = 2;
            foreach (var panel in _panels.Where(p => p.Type == "Динамическая"))
            {
                worksheet.Cell(rowInfo++, 5).Value =
                    $"Мощность: {panel.Power} Вт | Повороты: V={panel.RotationVertical}, H={panel.RotationHorizontal} | Кол-во: {panel.Count}";
            }

            double totalEnergy = 0;
            double totalConsumption = 0;
            int row = 2;

            foreach (var (date, cloudiness) in weatherData)
            {
                if (period == "Месяц" && date.Month != DateTime.Now.Month)
                    continue;

                int dayOfYear = date.DayOfYear;
                if (!_sunData.ContainsKey(dayOfYear)) continue;

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

                        double hourlyConsumption = panel.ConsumptionPower * panel.Count * 0.5; // 30 минут
                        dailyConsumption += hourlyConsumption;
                    }
                }

                // Энергия на возврат в конце дня
                foreach (var panel in _panels.Where(p => p.Type == "Динамическая"))
                {
                    double returnEnergy = 0.1 * panel.ConsumptionPower * (panel.RotationVertical + panel.RotationHorizontal) * panel.Count;
                    dailyConsumption += returnEnergy;
                }

                totalEnergy += dailyProduction;
                totalConsumption += dailyConsumption;

                worksheet.Cell(row, 1).Value = date.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = Math.Round(dailyProduction, 2);
                worksheet.Cell(row, 3).Value = Math.Round(dailyConsumption, 2);
                row++;
            }

            worksheet.Cell(row + 1, 1).Value = $"Итоговая выработка: {totalEnergy:F2} Вт⋅ч";
            worksheet.Cell(row + 2, 1).Value = $"Итоговое потребление: {totalConsumption:F2} Вт⋅ч";

            workbook.SaveAs(outputFile);
            Console.WriteLine($"Динамические данные сохранены в Excel: {Path.GetFullPath(outputFile)}");
        }
    }

}
