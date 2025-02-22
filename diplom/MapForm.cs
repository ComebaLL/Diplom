using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
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
        //private const string PVGIS_API_URL = "https://re.jrc.ec.europa.eu/api/v5_2/timeseries";
        private const string OPENWEATHER_API_URL = "https://api.openweathermap.org/data/2.5/forecast";
        private static readonly HttpClient client = new HttpClient();
        private const double Step = 0.15;
        private const string WeatherFilePath = "weather_weekly.txt";
        private const string DNIFilePath = "dni_weekly.txt";

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

        private async Task GenerateGridAsync()
        {
            Console.WriteLine("Генерация сетки Забайкальского края...");
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

        private void SaveAverageCoordinates()
        {
            selectedPoints.Clear();
            selectedPoints.AddRange(selectedSectors.Select(GetPolygonCenter));
            var averagePoint = CalculateAverageCoordinates();
            _savedAveragePoint = averagePoint;
            SaveCoordinatesToFile(averagePoint);
            FetchAndSaveWeatherData(averagePoint);
            FetchAndSaveDNI(averagePoint);
            MessageBox.Show($"Средние координаты сохранены:\nШирота: {averagePoint.Lat}\nДолгота: {averagePoint.Lng}", "Информация");
            AverageCoordinatesSelected?.Invoke(averagePoint);
            Close();
        }

        private static PointLatLng GetPolygonCenter(GMapPolygon polygon)
        {
            var latCenter = polygon.Points.Average(p => p.Lat);
            var lngCenter = polygon.Points.Average(p => p.Lng);
            return new PointLatLng(latCenter, lngCenter);
        }

        private PointLatLng CalculateAverageCoordinates()
        {
            if (!selectedPoints.Any())
                throw new InvalidOperationException("Секторы не выбраны.");
            return new PointLatLng(selectedPoints.Average(p => p.Lat), selectedPoints.Average(p => p.Lng));
        }

        private static void SaveCoordinatesToFile(PointLatLng coordinates)
        {
            const string filePath = "coordinates.txt";
            File.WriteAllText(filePath, $"Широта: {coordinates.Lat}\nДолгота: {coordinates.Lng}");
            Console.WriteLine($"Координаты сохранены в {filePath}");
        }

        private async void FetchAndSaveWeatherData(PointLatLng coordinates)
        {
            string weatherUrl = $"{OPENWEATHER_API_URL}?lat={coordinates.Lat}&lon={coordinates.Lng}&units=metric&appid=917617f28fc2c155c406a5abcf99ec92";
            //string dniUrl = $"{PVGIS_API_URL}?lat={coordinates.Lat}&lon={coordinates.Lng}&start={DateTime.UtcNow:yyyy-MM-dd}&end={DateTime.UtcNow.AddDays(7):yyyy-MM-dd}&outputformat=json&usehorizon=1&components=1";
            const string filePath = "weather_weekly.txt";
            try
            {
                string jsonResponse = await client.GetStringAsync(weatherUrl);
                File.WriteAllText(filePath, jsonResponse);
                Console.WriteLine($"Прогноз погоды на неделю сохранён в {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения прогноза погоды: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void FetchAndSaveDNI(PointLatLng coordinates)
        {
            string apiKey = "EmfUA53avO9LeohQPtarH01Ep2cuLlHedUQzeaut";
            string year = "2020"; // Берем исторические данные за 2020 год
            DateTime today = DateTime.UtcNow;
            DateTime weekLater = today.AddDays(6); // Берем прогноз на 7 дней

            string dniUrl = $"https://developer.nrel.gov/api/nsrdb/v2/solar/himawari-download.csv?" +
                            $"names={year}&wkt=POINT({coordinates.Lng}+{coordinates.Lat})&interval=60" +
                            $"&api_key={apiKey}&email=gumball20045@gmail.com";

            try
            {
                string csvResponse = await client.GetStringAsync(dniUrl);

                // Сохраняем в файл
                string filePath = "dni_weekly.txt";
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine($"DNI данные за неделю ({year}) для координат: {coordinates.Lat}, {coordinates.Lng}");
                    writer.Write(csvResponse);
                }

                Console.WriteLine($"DNI данные за неделю сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения DNI данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



    }
}
