using GMap.NET;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SolarPowerCalculator
{
    public class SolarEnergyCalculator
    {
        private PointLatLng _coordinates; // Координаты широты и долготы
        private double _nominalPower;    // Номинальная мощность
        private double _currentAngle;    // Угол поворота в градусах

        public SolarEnergyCalculator(PointLatLng coordinates, double nominalPower, double initialAngle = 0)
        {
            _coordinates = coordinates;
            _nominalPower = nominalPower;
            _currentAngle = initialAngle;
        }

        /// <summary>
        /// Устанавливает новые координаты с использованием GMap.
        /// </summary>
        public void SetCoordinates(PointLatLng coordinates)
        {
            _coordinates = coordinates;
        }

        /// <summary>
        /// Возвращает текущие координаты.
        /// </summary>
        public PointLatLng GetCoordinates()
        {
            return _coordinates;
        }

        /// <summary>
        /// Устанавливает номинальную мощность установки.
        /// </summary>
        public void SetNominalPower(double nominalPower)
        {
            _nominalPower = nominalPower;
        }

        /// <summary>
        /// Устанавливает текущий угол поворота панели.
        /// </summary>
        public void SetCurrentAngle(double angle)
        {
            _currentAngle = angle;
        }

        /// <summary>
        /// Возвращает текущий угол поворота панели.
        /// </summary>
        public double GetCurrentAngle()
        {
            return _currentAngle;
        }

        /// <summary>
        /// Вычисляет вырабатываемую электроэнергию на основе угла поворота и текущих условий.
        /// </summary>
        public double CalculateEnergy(double angle)
        {
            // Логика расчета будет здесь
            return 0.0;
        }

        /// <summary>
        /// Вычисляет влияние географических координат на выработку энергии.
        /// </summary>
        public double CalculateGeographicalEffect()
        {
            // Логика расчета будет здесь
            return 0.0;
        }

        /// <summary>
        /// Возвращает оценку потерь мощности при отклонении от нормали.
        /// </summary>
        public double CalculatePowerLoss(double angle)
        {
            // Логика расчета будет здесь
            return 0.0;
        }

        /// <summary>
        /// Возвращает итоговый расчет выработанной энергии за указанный период времени.
        /// </summary>
        public double CalculateTotalEnergy(double hours)
        {
            // Логика расчета будет здесь
            return 0.0;
        }

        /// <summary>
        /// Асинхронно получает прогноз погоды на неделю по текущим координатам.
        /// </summary>
        public async Task<string> GetWeatherForecastAsync()
        {
            string apiKey = "your_api_key"; // Замените на ваш API-ключ OpenWeatherMap
            string url = $"https://api.openweathermap.org/data/2.5/forecast?lat={_coordinates.Lat}&lon={_coordinates.Lng}&units=metric&appid={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject weatherData = JObject.Parse(responseBody);

                    // Обработка данных (например, получение средних значений температуры)
                    var forecastList = weatherData["list"];
                    string forecastSummary = "Прогноз погоды:\n";

                    foreach (var item in forecastList)
                    {
                        string dateTime = item["dt_txt"].ToString();
                        string temp = item["main"]["temp"].ToString();
                        forecastSummary += $"{dateTime}: {temp} °C\n";
                    }

                    return forecastSummary;
                }
                catch (Exception ex)
                {
                    return $"Ошибка получения прогноза погоды: {ex.Message}";
                }
            }
        }
    }
}
