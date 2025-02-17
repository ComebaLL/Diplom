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
        private List<Panel> solarPanels = new List<Panel>();
        private FlowLayoutPanel panelContainer;
        private Button addPanelButton;
        private Button mapButton;
        private PointLatLng? selectedAveragePoint;

        public MainForm()
        {
            Text = "Solar Power Calculator";
            Size = new Size(650, 400);

            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(580, 300),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelContainer);

            addPanelButton = new Button
            {
                Text = "Добавить солнечную панель",
                Location = new Point(10, 320),
                Width = 200
            };
            addPanelButton.Click += AddPanelButton_Click;
            Controls.Add(addPanelButton);

            mapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(220, 320),
                Width = 200
            };
            mapButton.Click += OpenMapButton_Click;
            Controls.Add(mapButton);
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
            MessageBox.Show($"Средние координаты: Широта {averagePoint.Lat}, Долгота {averagePoint.Lng}", "Координаты выбраны");
        }
    }
}
