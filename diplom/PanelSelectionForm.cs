using diplom;
using LiveCharts.Charts;
using System;
using System.Collections.Generic;
using System.ComponentModel; // Для BackgroundWorker
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SolarPowerCalculator
{
    public class PanelSelectionForm : Form
    {
        private FlowLayoutPanel panelContainer;
        private List<SolarPanel> solarPanels;
        private Button openMapButton;
        private Button calculateButton;
        private Button showChartButton;

        private ProgressBar progressBar;
        private Label statusLabel;
        private BackgroundWorker backgroundWorker;

        private List<SolarPanel> panelsToCalculate;
        private DateTime selectedStartDate;
        private DateTime selectedEndDate;

        public DateTime StartDate => selectedStartDate;
        public DateTime EndDate => selectedEndDate;

        //private PeriodSelectionDialog.Period SelectedPeriod;

        //private readonly SolarEnergyCalculator solarCalculator = new SolarEnergyCalculator();


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
                Text = "Экспорт в Excel",
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

            foreach (var panel in solarPanels)
            {
                var panelControl = new Panel
                {
                    Size = new Size(600, 60),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var label = new Label
                {
                    Text = panel.ToString(),
                    Location = new Point(5, 5),
                    AutoSize = true
                };
                panelControl.Controls.Add(label);

                var g1CheckBox = new CheckBox
                {
                    Text = "г1",
                    Location = new Point(320, 5),
                    AutoSize = true,
                    Tag = panel,
                    Checked = panel.IsChecked
                };
                panelControl.Controls.Add(g1CheckBox);

                var g2CheckBox = new CheckBox
                {
                    Text = "г2",
                    Location = new Point(370, 5),
                    AutoSize = true,
                    Tag = panel,
                    Checked = panel.IsChecked2
                };
                panelControl.Controls.Add(g2CheckBox);

                g1CheckBox.CheckedChanged += (s, e) =>
                {
                    if (s is CheckBox cb && cb.Tag is SolarPanel p)
                        p.IsChecked = cb.Checked;
                };

                g2CheckBox.CheckedChanged += (s, e) =>
                {
                    if (s is CheckBox cb && cb.Tag is SolarPanel p)
                        p.IsChecked2 = cb.Checked;
                };

                panelContainer.Controls.Add(panelControl);
            }
        }

        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveResultsDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var saveGroup1 = dialog.SaveGroup1;
            var saveGroup2 = dialog.SaveGroup2;

            if (!saveGroup1 && !saveGroup2)
            {
                MessageBox.Show("Выберите хотя бы одну группу для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string excelPath = "energy_report.xlsx";
                var reportWriter = new EnergyExcelWriter(excelPath);

                if (saveGroup1)
                {
                    string group1Path = "group1_output.txt";
                    if (File.Exists(group1Path))
                        reportWriter.AddSheetFromTxtFile("Group1", group1Path, GetPanelsByGroup(1));
                    else
                        MessageBox.Show("Файл group1_output.txt не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (saveGroup2)
                {
                    string group2Path = "group2_output.txt";
                    if (File.Exists(group2Path))
                        reportWriter.AddSheetFromTxtFile("Group2", group2Path, GetPanelsByGroup(2));
                    else
                        MessageBox.Show("Файл group2_output.txt не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                MessageBox.Show("Результаты успешно сохранены в Excel-файл!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private List<SolarPanel> GetPanelsByGroup(int groupNumber)
        {
            return groupNumber switch
            {
                1 => solarPanels.Where(p => p.IsChecked).ToList(),
                2 => solarPanels.Where(p => p.IsChecked2).ToList(),
                _ => new List<SolarPanel>()
            };
        }



        private void CalculateButton_Click(object sender, EventArgs e)
        {
            panelsToCalculate = solarPanels.Where(p => p.IsChecked || p.IsChecked2).ToList();
            if (panelsToCalculate.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одну панель!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var firstType = panelsToCalculate[0].Type;
            bool allSameType = panelsToCalculate.All(p => p.Type == firstType);

            if (!allSameType)
            {
                MessageBox.Show("Выбранные панели должны быть одного типа.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var periodForm = new PeriodSelectionDialog();
            if (periodForm.ShowDialog() != DialogResult.OK)
                return;

            selectedStartDate = periodForm.StartDate;
            selectedEndDate = periodForm.EndDate;

            calculateButton.Enabled = false;
            progressBar.Value = 0;
            progressBar.Visible = true;
            statusLabel.Text = "Выполняется расчет...";
            statusLabel.Visible = true;

            backgroundWorker.DoWork += (s, args) =>
            {
                int groupNumber = panelsToCalculate[0].IsChecked ? 1 : 2;
                var calculator = new SolarCalculator(panelsToCalculate, groupNumber);

                double totalProduction = 0;
                if (firstType == "Статическая")
                {
                    totalProduction = calculator.CalculateStaticPanelProductionForPeriod(selectedStartDate, selectedEndDate);
                }
                else if (firstType == "Динамическая")
                {
                    totalProduction = calculator.CalculateTrackerPanelProductionForPeriod(selectedStartDate, selectedEndDate);
                }

                args.Result = totalProduction;
            };

            backgroundWorker.RunWorkerCompleted += (s, args) =>
            {
                calculateButton.Enabled = true;
                progressBar.Visible = false;

                double result = (double)args.Result;
                statusLabel.Text = $"Расчет завершен. Общая выработка: {result:F2} кВт·ч";
            };

            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var calculator = new SolarCalculator(panelsToCalculate);
            int totalSteps = 100;

            for (int step = 0; step <= totalSteps; step++)
            {
                Thread.Sleep(10);
                backgroundWorker.ReportProgress((int)((step / (double)totalSteps) * 100));
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

        }


        private void ShowChartButton_Click(object sender, EventArgs e)
        {
            var periodForm = new PeriodSelectionDialog();
            if (periodForm.ShowDialog() != DialogResult.OK)
                return;

            DateTime startDate = periodForm.StartDate;
            DateTime endDate = periodForm.EndDate;

            var chartForm = new ChartForm(startDate, endDate);
            chartForm.Show();
        }


    }
}
