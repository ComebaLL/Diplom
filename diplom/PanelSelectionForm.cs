using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SolarPowerCalculator
{
    public class PanelSelectionForm : Form
    {
        private FlowLayoutPanel panelContainer;
        private List<SolarPanel> solarPanels;
        private Button openMapButton; //  Кнопка для открытия карты
        private Button calculateButton; //  Кнопка для расчета выработки
        private List<CheckBox> panelCheckBoxes = new List<CheckBox>(); //  Список чекбоксов для выделения

        public PanelSelectionForm(List<SolarPanel> panels)
        {
            solarPanels = panels;
            Text = "Выбор солнечных панелей";
            Size = new Size(650, 500);

            //  Контейнер для списка панелей
            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(600, 350),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelContainer);

            //  Кнопка "Открыть карту"
            openMapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(10, 380),
                Width = 200
            };
            openMapButton.Click += OpenMapButton_Click;
            Controls.Add(openMapButton);

            //  Кнопка "Рассчитать выработку"
            calculateButton = new Button
            {
                Text = "Рассчитать выработку",
                Location = new Point(220, 380),
                Width = 200
            };
            calculateButton.Click += CalculateButton_Click;
            Controls.Add(calculateButton);

            LoadPanels();
        }

        /// Загружаем список панелей с чекбоксами
        private void LoadPanels()
        {
            panelContainer.Controls.Clear();
            panelCheckBoxes.Clear();

            foreach (var panel in solarPanels)
            {
                var panelControl = new Panel
                {
                    Size = new Size(250, 50),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var checkBox = new CheckBox
                {
                    Text = panel.ToString(),
                    Dock = DockStyle.Fill,
                    Tag = panel
                };

                panelCheckBoxes.Add(checkBox);
                panelControl.Controls.Add(checkBox);
                panelContainer.Controls.Add(panelControl);
            }

        }

        /// Открываем карту
        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            var mapForm = new MapForm();
            mapForm.ShowDialog();
        }

        ///  Вызываем расчет выработки для выбранных пан
        private void CalculateButton_Click(object sender, EventArgs e)
        {
            var selectedPanels = panelCheckBoxes
                .Where(cb => cb.Checked)
                .Select(cb => (SolarPanel)cb.Tag)
                .ToList();

            if (selectedPanels.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну панель!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                /// Передаем список выбранных панелей в SolarCalculator
                var calculator = new SolarCalculator(selectedPanels);
                calculator.CalculateWeeklyProduction();

                MessageBox.Show("Расчёт завершён! Данные сохранены в energy_weekly.txt", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчёте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
