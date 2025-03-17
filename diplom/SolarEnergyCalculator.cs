using System;
using System.Globalization;
using System.IO;
using System.Linq;
using GMap.NET;

namespace SolarPowerCalculator
{
    public class SolarEnergyCalculator
    {
        private PointLatLng _coordinates;
        private double _nominalPower;
        private double _latitude;
        private double _longitude;
        private const double Pi = Math.PI;
        private const string SunDataFile = "sun_data.csv";
        private const string WeatherDataFile = "weather_weekly.txt";
        private const string CoordinatesFile = "coordinates.txt";
        private const string OutputFile = "energy_weekly.txt";

        public SolarEnergyCalculator()
        {
            LoadCoordinates();
        }

        /// <summary>
        /// Загружаем средние координаты из файла.
        /// </summary>
        private void LoadCoordinates()
        {
            if (!File.Exists(CoordinatesFile))
                throw new FileNotFoundException("Файл с координатами не найден.");

            var lines = File.ReadAllLines(CoordinatesFile);
            _latitude = double.Parse(lines[0].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
            _longitude = double.Parse(lines[1].Split(':')[1].Trim(), CultureInfo.InvariantCulture);
            _coordinates = new PointLatLng(_latitude, _longitude);
        }

        /// <summary>
        /// Рассчитываем склонение солнца для дня в году.
        /// </summary>
        private double CalculateSolarDeclination(int dayOfYear)
        {
            return 23.45 * Math.Sin((360.0 / 365.0) * (dayOfYear - 81) * Pi / 180);
        }

        /// <summary>
        /// Загружаем данные о восходе и закате из sun_data.csv.
        /// </summary>
        private (int sunrise, int sunset) LoadSunTimes(int dayOfYear)
        {
            if (!File.Exists(SunDataFile))
                throw new FileNotFoundException("Файл с данными о солнце не найден.");

            var lines = File.ReadAllLines(SunDataFile);
            foreach (var line in lines.Skip(1)) // Пропускаем заголовок
            {
                var parts = line.Split(';');
                if (parts.Length < 4) continue;

                if (DateTime.TryParseExact(parts[0], "dd.MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    if (date.DayOfYear == dayOfYear)
                    {
                        int sunrise = TimeToMinutes(parts[1].Split('—')[0]);
                        int sunset = TimeToMinutes(parts[3].Split('—')[1]);
                        return (sunrise, sunset);
                    }
                }
            }

            throw new Exception("Не найдены данные о восходе и закате для данного дня.");
        }

        /// <summary>
        /// Конвертируем время "ЧЧ:ММ:СС" в минуты с полуночи.
        /// </summary>
        private int TimeToMinutes(string time)
        {
            var parts = time.Split(':').Select(int.Parse).ToArray();
            return parts[0] * 60 + parts[1];
        }

        /// <summary>
        /// Загружаем облачность на неделю.
        /// </summary>
        private double[] LoadCloudiness()
        {
            if (!File.Exists(WeatherDataFile))
                throw new FileNotFoundException("Файл с прогнозом погоды не найден.");

            var json = File.ReadAllText(WeatherDataFile);
            var weatherData = Newtonsoft.Json.Linq.JObject.Parse(json);
            return weatherData["list"]
                .Select(d => (double)d["clouds"]["all"] / 100.0) // Преобразуем в коэффициент
                .Take(7)
                .ToArray();
        }

        /// <summary>
        /// Рассчитываем углы и выработку энергии на неделю.
        /// </summary>
        public void CalculateWeeklyEnergy()
        {
            double[] cloudiness = LoadCloudiness();
            var today = DateTime.UtcNow;
            var results = new System.Text.StringBuilder();

            results.AppendLine("Дата | Выработка (Вт)");

            for (int i = 0; i < 7; i++)
            {
                int dayOfYear = (today.AddDays(i)).DayOfYear;
                double declination = CalculateSolarDeclination(dayOfYear);
                (int sunrise, int sunset) = LoadSunTimes(dayOfYear);
                double dailyEnergy = CalculateDailyEnergy(declination, sunrise, sunset, cloudiness[i]);

                results.AppendLine($"{today.AddDays(i):dd.MM.yyyy} | {dailyEnergy:F2} Вт");
            }

            File.WriteAllText(OutputFile, results.ToString());
            Console.WriteLine($"Результаты сохранены в {OutputFile}");
        }

        /// <summary>
        /// Расчет суточной выработки энергии.
        /// </summary>
        private double CalculateDailyEnergy(double declination, int sunrise, int sunset, double cloudFactor)
        {
            double A = 0.25 * Pi / 180; // Шаг часового угла
            int Z = (sunrise + sunset) / 2; // Зенитное время
            int N = sunrise;
            int K = sunset;
            double totalEnergy = 0.0;

            for (int H = N; H <= K; H++)
            {
                double hourAngle = (H - Z) * A;
                double elevation = Math.Asin(Math.Sin(declination * Pi / 180) * Math.Sin(_latitude * Pi / 180) +
                                             Math.Cos(declination * Pi / 180) * Math.Cos(_latitude * Pi / 180) * Math.Cos(hourAngle));

                double OTe = Math.Abs(elevation - (Math.Asin(Math.Sin(declination * Pi / 180) * Math.Sin(_latitude * Pi / 180))));

                if (OTe > Pi / 2) OTe = Pi / 2;

                double incidenceAngle = Math.Atan(Math.Sqrt(Math.Tan(OTe) * Math.Tan(OTe)));
                double power = _nominalPower * Math.Cos(incidenceAngle) * (1 - cloudFactor) / 60; // Разделение на минуты

                totalEnergy += power;
            }

            return totalEnergy;
        }
    }
}
