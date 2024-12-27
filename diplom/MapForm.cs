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
    public class MapForm : Form
    {
        private GMapControl gmap;
        private GMapOverlay gridOverlay;
        private List<PointLatLng> selectedPoints = new List<PointLatLng>();
        private List<GMapPolygon> selectedSectors = new List<GMapPolygon>();  // Список выбранных секторов

        // Событие для передачи выбранных секторов
        public event Action<List<PointLatLng>> SectorsSelected;

        public MapForm()
        {
            // Устанавливаем заголовок окна
            this.Text = "Выбор сектора на карте";
            this.Size = new Size(800, 600);

            // Инициализация карты
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

            // Создаем наложение для сетки
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

        // Метод для получения центра полигона
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
                            // Если сектор уже выбран, отменяем выделение
                            selectedSectors.Remove(polygon);
                            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red)); // Вернуть исходный цвет
                        }
                        else
                        {
                            // Если сектор не выбран, выделяем его
                            selectedSectors.Add(polygon);
                            polygon.Fill = new SolidBrush(Color.FromArgb(150, Color.Red)); // Изменить цвет
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
                    // Передаем координаты центров выбранных секторов
                    selectedPoints.Clear();
                    foreach (var sector in selectedSectors)
                    {
                        // Используем новый метод для получения центра полигона
                        var center = GetPolygonCenter(sector);
                        selectedPoints.Add(center);
                    }
                    SectorsSelected?.Invoke(selectedPoints); // Передаем выбранные сектора
                    this.Close();
                }
            }
        }
    }
}
