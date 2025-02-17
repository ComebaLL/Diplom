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
        private const string PVGIS_API_URL = "https://re.jrc.ec.europa.eu/api/v5_2/timeseries";
        private static readonly HttpClient client = new HttpClient();
        private const double Step = 0.1;

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
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = 5
            };
            gmap.MouseClick += Gmap_MouseClick;
            Controls.Add(gmap);

            gridOverlay = new GMapOverlay("grid");
            gmap.Overlays.Add(gridOverlay);
            Task.Run(GenerateGridAsync);
        }
        private const string GridCacheFile = "grid_cache.dat"; // Файл кеша сетки

        private async Task GenerateGridAsync()
        {
            // Если есть кеш, загружаем сетку из файла
            if (File.Exists(GridCacheFile))
            {
                Console.WriteLine("Файл кеша найден, загружаем сетку...");
                LoadGridFromFile();
                return;
            }

            Console.WriteLine("Файл кеша отсутствует, генерируем сетку...");

            List<GMapPolygon> polygons = new List<GMapPolygon>();

            for (double lat = -85.0; lat <= 85.0; lat += Step)
            {
                for (double lng = -180.0; lng <= 180.0; lng += Step)
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

            // 🔹 Добавляем полигоны в UI порциями
            Invoke(new Action(() =>
            {
                foreach (var polygon in polygons)
                {
                    gridOverlay.Polygons.Add(polygon);
                }
                gmap.Refresh();
            }));

            // 🔹 Сохраняем сетку в файл после первой генерации
            SaveGridToFile(polygons);
        }

        // 🔹 **Метод сохранения сетки в файл**
        private void SaveGridToFile(List<GMapPolygon> polygons)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(GridCacheFile, false))
                {
                    foreach (var polygon in polygons)
                    {
                        writer.WriteLine($"{string.Join(";", polygon.Points.Select(p => $"{p.Lat},{p.Lng}"))}");
                    }
                }
                Console.WriteLine("Сетка сохранена в файл.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения сетки: {ex.Message}");
            }
        }

        // 🔹 **Метод загрузки сетки из файла**
        private void LoadGridFromFile()
        {
            try
            {
                if (!File.Exists(GridCacheFile))
                {
                    Console.WriteLine("Файл сетки не найден, генерация...");
                    Task.Run(GenerateGridAsync);
                    return;
                }

                List<GMapPolygon> polygons = new List<GMapPolygon>();

                foreach (var line in File.ReadLines(GridCacheFile))
                {
                    var points = line.Split(';')
                        .Select(p => p.Split(','))
                        .Select(coords => new PointLatLng(double.Parse(coords[0]), double.Parse(coords[1])))
                        .ToList();

                    var polygon = new GMapPolygon(points, "sector")
                    {
                        Fill = new SolidBrush(Color.FromArgb(50, Color.Red)),
                        Stroke = new Pen(Color.Red, 1)
                    };
                    polygons.Add(polygon);
                }

                // 🔹 Добавляем загруженные полигоны в UI
                Invoke(new Action(() =>
                {
                    gridOverlay.Polygons.Clear();
                    foreach (var polygon in polygons)
                    {
                        gridOverlay.Polygons.Add(polygon);
                    }
                    gmap.Refresh();
                }));

                Console.WriteLine("Сетка загружена из файла.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки сетки: {ex.Message}");
                Task.Run(GenerateGridAsync);
            }
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
            string requestUrl = $"{PVGIS_API_URL}?lat={coordinates.Lat}&lon={coordinates.Lng}&start={DateTime.UtcNow:yyyy-MM-dd}&end={DateTime.UtcNow.AddDays(7):yyyy-MM-dd}&outputformat=json&usehorizon=1&components=1";
            const string filePath = "weather_weekly.txt";
            try
            {
                string jsonResponse = await client.GetStringAsync(requestUrl);
                File.WriteAllText(filePath, jsonResponse);
                Console.WriteLine($"Прогноз погоды на неделю сохранён в {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения прогноза погоды: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
