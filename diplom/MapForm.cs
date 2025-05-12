using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Newtonsoft.Json.Linq;

namespace SolarPowerCalculator
{
    public class MapForm : Form
    {
        private GMapControl gmap;
        private GMapOverlay gridOverlay;
        private List<PointLatLng> selectedPoints = new List<PointLatLng>();
        private List<GMapPolygon> selectedSectors = new List<GMapPolygon>();
        private PointLatLng? _savedAveragePoint;

        private const string OPENWEATHER_API_URL = "https://api.openweathermap.org/data/2.5/forecast";
        private const string API_KEY = "443c1cb752e066cac67dcca488486dd6"; // API-ключ OpenWeather
        private const string WeatherFilePath = "weather_weekly.txt";
        private const string CoordinatesFilePath = "coordinates.txt";

        private const double Step = 0.15;
        private static readonly HttpClient client = new HttpClient();

        public event Action<PointLatLng> AverageCoordinatesSelected;

        public MapForm()
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            Text = "Выбор сектора на карте";
            Size = new Size(800, 600);

            gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.GoogleMap,
                Position = new PointLatLng(52.0317, 113.501),
                MinZoom = 6,
                MaxZoom = 18,
                Zoom = 7
            };
            gmap.MouseClick += Gmap_MouseClick;
            Controls.Add(gmap);

            gridOverlay = new GMapOverlay("grid");
            gmap.Overlays.Add(gridOverlay);
            Task.Run(GenerateGridAsync);
        }

        ///  Генерация сетки Забайкальского края
        private async Task GenerateGridAsync()
        {
            Console.WriteLine("Генерация сетки...");
            List<GMapPolygon> polygons = new List<GMapPolygon>();

            for (double lat = 49.0; lat <= 55.0; lat += Step)
            {
                for (double lng = 107.0; lng <= 120.0; lng += Step)
                {
                    var points = new List<PointLatLng>
                    {
                        new PointLatLng(lat, lng),
                        new PointLatLng(lat + Step, lng),
                        new PointLatLng(lat + Step, lng + Step),
                        new PointLatLng(lat, lng + Step),
                    };

                    var polygon = new GMapPolygon(points, "sector")
                    {
                        Fill = new SolidBrush(Color.FromArgb(50, Color.Red)),
                        Stroke = new Pen(Color.Red, 1)
                    };
                    polygons.Add(polygon);
                }
            }

            Invoke(new Action(() =>
            {
                foreach (var polygon in polygons)
                {
                    gridOverlay.Polygons.Add(polygon);
                }
                gmap.Refresh();
            }));
        }

        ///  Обработка кликов по карте
        private void Gmap_MouseClick(object sender, MouseEventArgs e)
        {
            var point = gmap.FromLocalToLatLng(e.X, e.Y);
            if (e.Button == MouseButtons.Left)
            {
                ToggleSectorSelection(point);
            }
            else if (e.Button == MouseButtons.Right && selectedSectors.Count > 0)
            {
                SaveAverageCoordinates();
            }
        }


        ///  Выбор или отмена выбора сектора
        private void ToggleSectorSelection(PointLatLng point)
        {
            foreach (var polygon in gridOverlay.Polygons)
            {
                if (!polygon.IsInside(point)) continue;
                if (selectedSectors.Contains(polygon))
                {
                    selectedSectors.Remove(polygon);
                    polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red));
                }
                else
                {
                    selectedSectors.Add(polygon);
                    polygon.Fill = new SolidBrush(Color.FromArgb(150, Color.Red));
                }
                gmap.Refresh();
                break;
            }
        }


        ///  Сохранение средних координат
        private void SaveAverageCoordinates()
        {
            selectedPoints.Clear();
            selectedPoints.AddRange(selectedSectors.Select(GetPolygonCenter));
            var averagePoint = CalculateAverageCoordinates();
            _savedAveragePoint = averagePoint;
            SaveCoordinatesToFile(averagePoint);
            //FetchAndSaveWeatherData(averagePoint);
            MessageBox.Show($"Средние координаты сохранены:\nШирота: {averagePoint.Lat}\nДолгота: {averagePoint.Lng}", "Информация");
            AverageCoordinatesSelected?.Invoke(averagePoint);
            Close();
        }


        ///  Получение центра сектора
        private static PointLatLng GetPolygonCenter(GMapPolygon polygon)
        {
            var latCenter = polygon.Points.Average(p => p.Lat);
            var lngCenter = polygon.Points.Average(p => p.Lng);
            return new PointLatLng(latCenter, lngCenter);
        }

        ///  Расчет средних координат
        private PointLatLng CalculateAverageCoordinates()
        {
            if (!selectedPoints.Any())
                throw new InvalidOperationException("Секторы не выбраны.");
            return new PointLatLng(selectedPoints.Average(p => p.Lat), selectedPoints.Average(p => p.Lng));
        }

        /// Сохранение координат в файл        
        private static void SaveCoordinatesToFile(PointLatLng coordinates)
        {
            File.WriteAllText(CoordinatesFilePath, $"Широта: {coordinates.Lat}\nДолгота: {coordinates.Lng}");
        }


        /*
        ///  Получение прогноза погоды и сохранение в файл
        private async void FetchAndSaveWeatherData(PointLatLng coordinates)
        {
            string weatherUrl = $"{OPENWEATHER_API_URL}?lat={coordinates.Lat}&lon={coordinates.Lng}&units=metric&appid={API_KEY}";
            try
            {
                string jsonResponse = await client.GetStringAsync(weatherUrl);
                var weatherData = JObject.Parse(jsonResponse);

                var rawForecast = weatherData["list"]
                    .Select(d => new
                    {
                        DateTime = DateTime.Parse((string)d["dt_txt"]),
                        Cloudiness = (int)d["clouds"]["all"],
                        Temperature = (double)d["main"]["temp"]
                    })
                    .ToList();

                /// Интерполированные значения
                var interpolated = new List<(DateTime time, double cloudiness, double temperature)>();

                for (int i = 0; i < rawForecast.Count - 1; i++)
                {
                    var curr = rawForecast[i];
                    var next = rawForecast[i + 1];

                    // 3 исходные точки: T0, T1, T2 (через 0ч, 1ч, 2ч, 3ч)
                    for (int h = 0; h < 3; h++)
                    {
                        DateTime t = curr.DateTime.AddHours(h);
                        double cloud = curr.Cloudiness + (next.Cloudiness - curr.Cloudiness) * (h / 3.0);
                        double temp = curr.Temperature + (next.Temperature - curr.Temperature) * (h / 3.0);
                        interpolated.Add((t, cloud, temp));
                    }
                }

                // Добавляем последнюю точку
                var last = rawForecast.Last();
                interpolated.Add((last.DateTime, last.Cloudiness, last.Temperature));

                // Сохраняем в файл
                using (StreamWriter writer = new StreamWriter(WeatherFilePath, false))
                {
                    writer.WriteLine("Дата;Облачность (%);Температура (°C)");
                    foreach (var entry in interpolated)
                    {
                        writer.WriteLine($"{entry.time:yyyy-MM-dd HH:mm:ss};{entry.cloudiness:F1};{entry.temperature:F1}");
                    }
                }

                Console.WriteLine($"Интерполированные погодные данные сохранены в {WeatherFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения прогноза: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }*/
       
    }
}
