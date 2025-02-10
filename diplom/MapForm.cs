using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;

namespace SolarPowerCalculator
{
    public class MapForm : Form
    {
        private GMapControl gmap;
        private GMapOverlay gridOverlay;
        private List<PointLatLng> selectedPoints = new List<PointLatLng>();
        private List<GMapPolygon> selectedSectors = new List<GMapPolygon>();
        private PointLatLng? _savedAveragePoint; // Переменная для хранения средней координаты

        public event Action<PointLatLng> AverageCoordinatesSelected; // Событие для передачи координат

        public MapForm()
        {
            // Отключение локального кеша для избежания блокировок
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;

            this.Text = "Выбор сектора на карте";
            this.Size = new Size(800, 600);

            gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.GoogleMap,
                Position = new PointLatLng(55.7558, 37.6173),
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = 5
            };
            gmap.MouseClick += Gmap_MouseClick;
            this.Controls.Add(gmap);

            gridOverlay = new GMapOverlay("grid");
            gmap.Overlays.Add(gridOverlay);

            GenerateGrid();
        }

        private void GenerateGrid()
        {
            double step = 1.0; // Размер клетки в градусах
            for (double lat = -85.0; lat <= 85.0; lat += step)
            {
                for (double lng = -180.0; lng <= 180.0; lng += step)
                {
                    List<PointLatLng> points = new List<PointLatLng>
                    {
                        new PointLatLng(lat, lng),
                        new PointLatLng(lat + step, lng),
                        new PointLatLng(lat + step, lng + step),
                        new PointLatLng(lat, lng + step),
                    };

                    GMapPolygon sector = new GMapPolygon(points, "sector")
                    {
                        Fill = new SolidBrush(Color.FromArgb(50, Color.Red)),
                        Stroke = new Pen(Color.Red, 1)
                    };
                    gridOverlay.Polygons.Add(sector);
                }
            }
        }

        private void Gmap_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var point = gmap.FromLocalToLatLng(e.X, e.Y);

                foreach (var polygon in gridOverlay.Polygons)
                {
                    if (polygon.IsInside(point))
                    {
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
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (selectedSectors.Count > 0)
                {
                    selectedPoints.Clear();
                    foreach (var sector in selectedSectors)
                    {
                        var center = GetPolygonCenter(sector);
                        selectedPoints.Add(center);
                    }

                    var averagePoint = CalculateAverageCoordinates();
                    _savedAveragePoint = averagePoint; // Сохраняем в переменную

                    SaveCoordinatesToFile(averagePoint); // Сохраняем в файл
                    MessageBox.Show($"Средние координаты сохранены:\nШирота: {averagePoint.Lat}\nДолгота: {averagePoint.Lng}", "Информация");

                    AverageCoordinatesSelected?.Invoke(averagePoint); // Передаём координаты в `MainForm`
                    this.Close();
                }
            }
        }

        private PointLatLng GetPolygonCenter(GMapPolygon polygon)
        {
            double latSum = 0;
            double lngSum = 0;
            int pointCount = polygon.Points.Count;

            foreach (var point in polygon.Points)
            {
                latSum += point.Lat;
                lngSum += point.Lng;
            }

            double latCenter = latSum / pointCount;
            double lngCenter = lngSum / pointCount;

            return new PointLatLng(latCenter, lngCenter);
        }

        private PointLatLng CalculateAverageCoordinates()
        {
            if (selectedPoints.Count == 0)
                throw new InvalidOperationException("Секторы не выбраны.");

            double averageLat = selectedPoints.Average(p => p.Lat);
            double averageLng = selectedPoints.Average(p => p.Lng);

            return new PointLatLng(averageLat, averageLng);
        }

        /// <summary>
        /// Метод для сохранения координат в файл.
        /// </summary>
        private void SaveCoordinatesToFile(PointLatLng coordinates)
        {
            string filePath = "coordinates.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"Широта: {coordinates.Lat}");
                    writer.WriteLine($"Долгота: {coordinates.Lng}");
                }
                Console.WriteLine($"Координаты успешно сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении координат: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
