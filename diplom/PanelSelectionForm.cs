using LiveCharts.Charts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.ComponentModel; // Для BackgroundWorker

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

        // Новые поля для прогресса
        private ProgressBar progressBar;
        private Label statusLabel;
        private BackgroundWorker backgroundWorker;

        private List<SolarPanel> panelsToCalculate;
        private PeriodSelectionDialog.PeriodOption selectedPeriod;

        public PanelSelectionForm(List<SolarPanel> panels)
        {
            solarPanels = panels;
            Text = "Выбор солнечных панелей";
            Size = new Size(650, 550);

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

            showChartButton = new Button
            {
                Text = "Показать график",
                Location = new Point(430, 380),
                Width = 200
            };
            showChartButton.Click += ShowChartButton_Click;
            Controls.Add(showChartButton);

            // Инициализация ProgressBar и Label (по умолчанию скрыты)
            progressBar = new ProgressBar
            {
                Location = new Point(10, 420),
                Size = new Size(620, 25),
                Visible = false,
                Minimum = 0,
                Maximum = 100
            };
            Controls.Add(progressBar);

            statusLabel = new Label
            {
                Location = new Point(10, 450),
                Size = new Size(620, 25),
                Text = "",
                Visible = false
            };
            Controls.Add(statusLabel);

            // Инициализация BackgroundWorker
            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            LoadPanels();
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

            panelsToCalculate = solarPanels.Where(p => p.IsChecked).ToList();
            if (panelsToCalculate.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну панель!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Показываем выбор периода
            using var dialog = new PeriodSelectionDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            selectedPeriod = dialog.SelectedPeriod;

            // Блокируем кнопку и показываем прогрессбар и статус
            calculateButton.Enabled = false;
            progressBar.Value = 0;
            progressBar.Visible = true;
            statusLabel.Text = "Выполняется расчет...";
            statusLabel.Visible = true;

            // Запускаем расчёт в фоне
            backgroundWorker.RunWorkerAsync();
        }

        // Основной расчет в фоне
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var calculator = new SolarCalculator(panelsToCalculate);

            int totalSteps = 1; // По умолчанию 1 шаг

            // Настройка количества шагов в зависимости от периода
            switch (selectedPeriod)
            {
                case PeriodSelectionDialog.PeriodOption.Week:
                    totalSteps = 100;
                    break;
                case PeriodSelectionDialog.PeriodOption.Month:
                    totalSteps = 100;
                    break;
                case PeriodSelectionDialog.PeriodOption.Year:
                    totalSteps = 100;
                    break;
            }

            for (int step = 0; step <= totalSteps; step++)
            {

                Thread.Sleep(20); // Задержка для симуляции работы

                // Отправляем прогресс
                int progressPercent = (int)((step / (double)totalSteps) * 100);
                backgroundWorker.ReportProgress(progressPercent);
            }

            // Запускаем основной расчет после прогресса 
            switch (selectedPeriod)
            {
                case PeriodSelectionDialog.PeriodOption.Week:
                    if (panelsToCalculate.All(p => p.Type == "Статическая"))
                        calculator.CalculateStaticPanelProductionForWeek();
                    else if (panelsToCalculate.All(p => p.Type == "Динамическая"))
                        calculator.CalculateTrackerPanelProductionForWeek();
                    else
                    {
                        calculator.CalculateStaticPanelProductionForWeek();
                        calculator.CalculateTrackerPanelProductionForWeek();
                    }
                    break;

                case PeriodSelectionDialog.PeriodOption.Month:
                    if (panelsToCalculate.All(p => p.Type == "Статическая"))
                        calculator.CalculateStaticPanelProductionForPeriod("Месяц");
                    else if (panelsToCalculate.All(p => p.Type == "Динамическая"))
                        calculator.CalculateTrackerPanelProductionForPeriod("Месяц");
                    else
                    {
                        calculator.CalculateStaticPanelProductionForPeriod("Месяц");
                        calculator.CalculateTrackerPanelProductionForPeriod("Месяц");
                    }
                    break;

                case PeriodSelectionDialog.PeriodOption.Year:
                    if (panelsToCalculate.All(p => p.Type == "Статическая"))
                        calculator.CalculateStaticPanelProductionForPeriod("Год");
                    else if (panelsToCalculate.All(p => p.Type == "Динамическая"))
                        calculator.CalculateTrackerPanelProductionForPeriod("Год");
                    else
                    {
                        calculator.CalculateStaticPanelProductionForPeriod("Год");
                        calculator.CalculateTrackerPanelProductionForPeriod("Год");
                    }
                    break;
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visible = false;
            statusLabel.Visible = false;
            calculateButton.Enabled = true;

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
                    staticFile = "energy_static_weekly.txt";
                    trackerFile = "energy_tracker_weekly.txt";
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
