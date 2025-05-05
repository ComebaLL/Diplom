using LiveCharts.Charts;
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
        private Button openMapButton;
        private Button calculateButton;
        private List<CheckBox> panelCheckBoxes = new List<CheckBox>();
        private Button showChartButton;

        public PanelSelectionForm(List<SolarPanel> panels)
        {
            solarPanels = panels;
            Text = "Выбор солнечных панелей";
            Size = new Size(650, 500);

            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(600, 350),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelContainer);

            openMapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(10, 380),
                Width = 200
            };
            openMapButton.Click += OpenMapButton_Click;
            Controls.Add(openMapButton);

            calculateButton = new Button
            {
                Text = "Рассчитать выработку",
                Location = new Point(220, 380),
                Width = 200
            };
            calculateButton.Click += CalculateButton_Click;
            Controls.Add(calculateButton);

            LoadPanels();

            // Кнопка "Показать график"
            showChartButton = new Button
            {
                Text = "Показать график",
                Location = new Point(430, 380),
                Width = 200
            };
            showChartButton.Click += ShowChartButton_Click;
            Controls.Add(showChartButton);

        }

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
                    Tag = panel,
                    Checked = panel.IsChecked
                };

                panelCheckBoxes.Add(checkBox);
                panelControl.Controls.Add(checkBox);
                panelContainer.Controls.Add(panelControl);
            }
        }

        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            var mapForm = new MapForm();
            mapForm.ShowDialog();
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            // Обновляем флаг IsChecked
            foreach (var checkBox in panelCheckBoxes)
            {
                if (checkBox.Tag is SolarPanel panel)
                    panel.IsChecked = checkBox.Checked;
            }

            var selectedPanels = solarPanels.Where(p => p.IsChecked).ToList();
            if (selectedPanels.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну панель!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Показываем выбор периода
            using var dialog = new PeriodSelectionDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var calculator = new SolarCalculator(selectedPanels);

            switch (dialog.SelectedPeriod)
            {
                case PeriodSelectionDialog.PeriodOption.Week:
                    calculator.CalculateEnergyProduction(DateTime.Now.AddDays(-7), DateTime.Now);
                    break;

                case PeriodSelectionDialog.PeriodOption.Month:
                    if (selectedPanels.All(p => p.Type == "Статическая"))
                        calculator.CalculateStaticPanelProductionForPeriod("Месяц");
                    else if (selectedPanels.All(p => p.Type == "Динамическая"))
                        calculator.CalculateTrackerPanelProductionForPeriod("Месяц");
                    else
                    {
                        calculator.CalculateStaticPanelProductionForPeriod("Месяц");
                        calculator.CalculateTrackerPanelProductionForPeriod("Месяц");
                    }
                    break;

                case PeriodSelectionDialog.PeriodOption.Year:
                    if (selectedPanels.All(p => p.Type == "Статическая"))
                        calculator.CalculateStaticPanelProductionForPeriod("Год");
                    else if (selectedPanels.All(p => p.Type == "Динамическая"))
                        calculator.CalculateTrackerPanelProductionForPeriod("Год");
                    else
                    {
                        calculator.CalculateStaticPanelProductionForPeriod("Год");
                        calculator.CalculateTrackerPanelProductionForPeriod("Год");
                    }
                    break;
            }

            //MessageBox.Show("Расчёт завершён!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowChartButton_Click(object sender, EventArgs e)
        {
            var dialog = new PeriodSelectionDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string staticFile = null;
            string trackerFile = null;

            switch (dialog.SelectedPeriod)
            {
                case PeriodSelectionDialog.PeriodOption.Week:
                    staticFile = "energy_static.txt";
                    trackerFile = "energy_tracker.txt";
                    break;
                case PeriodSelectionDialog.PeriodOption.Month:
                    staticFile = "energy_static_month.txt";
                    trackerFile = "energy_tracker_month.txt";
                    break;
                case PeriodSelectionDialog.PeriodOption.Year:
                    staticFile = "energy_static_year.txt";
                    trackerFile = "energy_tracker_year.txt";
                    break;
            }

            bool staticExists = !string.IsNullOrEmpty(staticFile) && File.Exists(staticFile) && new FileInfo(staticFile).Length > 0;
            bool trackerExists = !string.IsNullOrEmpty(trackerFile) && File.Exists(trackerFile) && new FileInfo(trackerFile).Length > 0;

            if (!staticExists && !trackerExists)
            {
                MessageBox.Show("Нет данных для отображения графика.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var chartForm = new ChartForm(dialog.SelectedPeriod);
            chartForm.Show();
        }



    }
}
