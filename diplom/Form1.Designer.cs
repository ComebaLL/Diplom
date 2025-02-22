using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GMap.NET;

namespace SolarPowerCalculator
{
    public class MainForm : Form
    {
        private List<Panel> solarPanels = new List<Panel>(); // Список панелей
        private FlowLayoutPanel panelContainer; // Контейнер для панелей
        private Button mapButton; // Кнопка для открытия карты
        private PointLatLng? selectedAveragePoint;
        private Panel addPanelButton; // Кастомная кнопка "Добавить панель"

        public MainForm()
        {
            Text = "Solar Power Calculator";
            Size = new Size(650, 400);

            // 🔹 Контейнер для панелей
            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(580, 300),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelContainer);

            // 🔹 Кастомная кнопка "Добавить панель"
            addPanelButton = new Panel
            {
                Size = new Size(100, 100),
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            addPanelButton.Paint += AddPanelButton_Paint;
            addPanelButton.Click += AddPanelButton_Click;
            panelContainer.Controls.Add(addPanelButton); // Добавляем в контейнер

            // 🔹 Кнопка для открытия карты
            mapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(10, 320),
                Width = 200
            };
            mapButton.Click += OpenMapButton_Click;
            Controls.Add(mapButton);
        }

        /// <summary>
        /// 🔹 Обработчик рисования кнопки (Рисует `+` в центре)
        /// </summary>
        private void AddPanelButton_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Black, 4))
            {
                int midX = addPanelButton.Width / 2;
                int midY = addPanelButton.Height / 2;
                int size = 20; // Размер плюса

                // Рисуем "+"
                e.Graphics.DrawLine(pen, midX - size, midY, midX + size, midY);
                e.Graphics.DrawLine(pen, midX, midY - size, midX, midY + size);
            }
        }

        /// <summary>
        /// 🔹 Обработчик нажатия на кнопку "Добавить панель"
        /// </summary>
        private void AddPanelButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SolarPanelDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var panel = dialog.CreatedPanel;
                    solarPanels.Add(panel);

                    // 🔹 Добавляем новую панель перед кнопкой "+"
                    panelContainer.Controls.Add(panel);
                    panelContainer.Controls.SetChildIndex(addPanelButton, panelContainer.Controls.Count - 1);
                }
            }
        }

        /// <summary>
        /// 🔹 Обработчик нажатия кнопки "Открыть карту"
        /// </summary>
        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            var mapForm = new MapForm();
            mapForm.AverageCoordinatesSelected += OnAverageCoordinatesSelected;
            mapForm.ShowDialog();
        }

        /// <summary>
        /// 🔹 Метод обработки полученных координат
        /// </summary>
        private void OnAverageCoordinatesSelected(PointLatLng averagePoint)
        {
            selectedAveragePoint = averagePoint;
            MessageBox.Show($"Средние координаты: Широта {averagePoint.Lat}, Долгота {averagePoint.Lng}", "Координаты выбраны");
        }
    }
}
