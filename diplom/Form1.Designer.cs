// MainForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;

namespace SolarPowerCalculator
{
    public class MainForm : Form
    {
        public MainForm()
        {
            // Устанавливаем заголовок окна
            this.Text = "Solar Power Calculator";
            this.Size = new Size(1000, 600);

            // Создаем метки и текстовые поля для каждого параметра
            Label angleLabel = new Label { Text = "Угол установки:", Location = new Point(10, 10), Width = 150 };
            TextBox angleTextBox = new TextBox { Location = new Point(170, 10), Width = 100 };
            this.Controls.Add(angleLabel);
            this.Controls.Add(angleTextBox);

            Label powerLabel = new Label { Text = "Мощность установки (Вт):", Location = new Point(10, 40), Width = 150 };
            TextBox powerTextBox = new TextBox { Location = new Point(170, 40), Width = 100 };
            this.Controls.Add(powerLabel);
            this.Controls.Add(powerTextBox);

            // Создаем кнопку для открытия карты
            Button mapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(10, 80),
                Width = 150
            };
            this.Controls.Add(mapButton);

            // Обработчик нажатия кнопки для открытия карты
            mapButton.Click += (sender, args) =>
            {
                MapForm mapForm = new MapForm();
                mapForm.Show();
            };
        }
    }

    public class MapForm : Form
    {
        private GMapControl gmap;
        private GMapOverlay gridOverlay;
        private List<PointLatLng> selectedPoints;

        public MapForm()
        {
            // Устанавливаем заголовок окна
            this.Text = "Выбор сектора на карте";
            this.Size = new Size(800, 600);

            selectedPoints = new List<PointLatLng>();

            // Инициализация карты
            gmap = new GMapControl();
            gmap.Dock = DockStyle.Fill;
            gmap.MapProvider = GMapProviders.GoogleMap;
            gmap.Position = new PointLatLng(55.7558, 37.6173); // Координаты Москвы
            gmap.MinZoom = 2;
            gmap.MaxZoom = 18;
            gmap.Zoom = 5;
            gmap.MouseClick += Gmap_MouseClick;
            this.Controls.Add(gmap);

            // Создаем наложение для сетки
            gridOverlay = new GMapOverlay("grid");
            gmap.Overlays.Add(gridOverlay);

            // Генерация сетки
            GenerateGrid();
        }

        private void GenerateGrid()
        {
            double step = 1.0; // Размер клетки в градусах
            for (double lat = -85.0; lat <= 85.0; lat += step)
            {
                for (double lng = -180.0; lng <= 180.0; lng += step)
                {
                    // Создаем квадратный сектор
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
                // Получение координат клика
                var point = gmap.FromLocalToLatLng(e.X, e.Y);

                // Определение, попал ли клик в сектор
                foreach (var polygon in gridOverlay.Polygons)
                {
                    if (polygon.IsInside(point))
                    {
                        // Изменение цвета сектора
                        polygon.Fill = new SolidBrush(Color.FromArgb(150, Color.Red));
                        gmap.Refresh();

                        // Добавление координат в список выбранных точек
                        selectedPoints.Add(point);
                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Вычисление средней точки
                if (selectedPoints.Count > 0)
                {
                    double avgLat = 0;
                    double avgLng = 0;

                    foreach (var p in selectedPoints)
                    {
                        avgLat += p.Lat;
                        avgLng += p.Lng;
                    }

                    avgLat /= selectedPoints.Count;
                    avgLng /= selectedPoints.Count;

                    MessageBox.Show($"Средняя точка: Широта {avgLat}, Долгота {avgLng}");
                }
            }
        }
    }
}
