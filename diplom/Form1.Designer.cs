using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using Newtonsoft.Json.Linq;

namespace SolarPowerCalculator
{
    public class MainForm : Form
    {
        private List<Panel> solarPanels = new List<Panel>(); // Список добавленных солнечных панелей
        private FlowLayoutPanel panelContainer; // Контейнер для отображения панелей
        private Button addPanelButton; // Кнопка для добавления новой панели
        private Button mapButton; // Кнопка для открытия карты
        private Button parseWeatherButton; // Кнопка для парсинга прогноза погоды
        private PointLatLng? selectedAveragePoint; // Сохранённая средняя координата выбранных секторов

        public MainForm()
        {
            this.Text = "Solar Power Calculator";
            this.Size = new Size(650, 400);

            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(580, 300),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(panelContainer);

            addPanelButton = new Button
            {
                Text = "Добавить солнечную панель",
                Location = new Point(10, 320),
                Width = 200
            };
            addPanelButton.Click += AddPanelButton_Click;
            this.Controls.Add(addPanelButton);

            mapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(220, 320),
                Width = 200
            };
            mapButton.Click += OpenMapButton_Click;
            this.Controls.Add(mapButton);

            parseWeatherButton = new Button
            {
                Text = "Сохранить прогноз погоды",
                Location = new Point(430, 320),
                Width = 200,
                Enabled = false
            };
            parseWeatherButton.Click += ParseWeatherButton_Click;
            this.Controls.Add(parseWeatherButton);
        }

        private void AddPanelButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SolarPanelDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var panel = dialog.CreatedPanel;
                    solarPanels.Add(panel);
                    panelContainer.Controls.Add(panel);
                }
            }
        }

        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            var mapForm = new MapForm();
            mapForm.AverageCoordinatesSelected += OnAverageCoordinatesSelected;
            mapForm.ShowDialog();
        }

        private void OnAverageCoordinatesSelected(PointLatLng averagePoint)
        {
            selectedAveragePoint = averagePoint;
            parseWeatherButton.Enabled = true;
            MessageBox.Show($"Средние координаты: Широта {averagePoint.Lat}, Долгота {averagePoint.Lng}", "Координаты выбраны");
        }

        private async void ParseWeatherButton_Click(object sender, EventArgs e)
        {
            if (selectedAveragePoint == null)
            {
                MessageBox.Show("Сначала выберите координаты на карте.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string filePath = "weather_forecast.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Прогноз погоды для выбранных координат:");
                    writer.WriteLine($"Координаты: Широта {selectedAveragePoint.Value.Lat}, Долгота {selectedAveragePoint.Value.Lng}");

                    string forecast = await GetWeatherForecastAsync(selectedAveragePoint.Value.Lat, selectedAveragePoint.Value.Lng);
                    writer.WriteLine(forecast);

                    MessageBox.Show($"Прогноз погоды успешно сохранён в файл: {Path.GetFullPath(filePath)}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<string> GetWeatherForecastAsync(double latitude, double longitude)
        {
            string apiKey = "917617f28fc2c155c406a5abcf99ec92";
            string url = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&appid={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject json = JObject.Parse(responseBody);
                    string forecast = "";
                    foreach (var item in json["list"])
                    {
                        string date = item["dt_txt"].ToString();
                        string temp = item["main"]["temp"].ToString();
                        string humidity = item["main"]["humidity"].ToString();
                        string windSpeed = item["wind"]["speed"].ToString();
                        string cloudiness = item["clouds"]["all"].ToString();

                        forecast += $"Дата: {date}\nТемпература: {temp}°C\nВлажность: {humidity}%\nВетер: {windSpeed} м/с\nОблачность: {cloudiness}%\n\n";
                    }

                    return forecast;
                }
                catch (Exception ex)
                {
                    return $"Ошибка получения данных: {ex.Message}";
                }
            }
        }
    }
}
