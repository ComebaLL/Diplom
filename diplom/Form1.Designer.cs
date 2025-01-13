using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GMap.NET;
using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;
/*
 требуемая мощьность, текущая мощность вычесляеться как кол-во понелей умноженное на их мощность(мощность может быть разная); 2 угла по вертикали\горизонталь;
сделать UI чтобы выбрать статичную или поворотную установку или обе сразу, у установки поворотной можно забить допустимый диапозон поворота, потребление электроэнергии(на 1 град по верт\горизонт)
на вывод графики (сутки, неделя, месяц, год, 3-5-10 лет) прогноз год, 3года точки отсчета по дня, 5-10 лет по месяцам точки отсчета;
вывод рекомендации, что будет использовать эффективнее поворотную, либо добавить статичные модели, срок окупаемости;
добавить в установку стоимость конструкции;
*/
/* UI, стека, парсинг погоды*/
namespace SolarPowerCalculator
{
    public class MainForm : Form
    {
        private TextBox angleTextBox;
        private TextBox powerTextBox;
        private Button calculateButton;
        private Button mapButton;

        public MainForm()
        {
            // Настройка формы
            this.Text = "Solar Power Calculator";
            this.Size = new Size(400, 200);

            // Поле для угла поворота
            Label angleLabel = new Label
            {
                Text = "Угол установки (град):",
                Location = new Point(10, 20),
                Width = 150
            };
            this.Controls.Add(angleLabel);

            angleTextBox = new TextBox
            {
                Location = new Point(170, 20),
                Width = 100
            };
            this.Controls.Add(angleTextBox);

            // Поле для мощности установки
            Label powerLabel = new Label
            {
                Text = "Мощность установки (Вт):",
                Location = new Point(10, 60),
                Width = 150
            };
            this.Controls.Add(powerLabel);

            powerTextBox = new TextBox
            {
                Location = new Point(170, 60),
                Width = 100
            };
            this.Controls.Add(powerTextBox);

            // Кнопка для выполнения расчетов
            calculateButton = new Button
            {
                Text = "Рассчитать",
                Location = new Point(10, 100),
                Width = 150
            };
            calculateButton.Click += CalculateButton_Click;
            this.Controls.Add(calculateButton);

            // Кнопка для открытия карты
            mapButton = new Button
            {
                Text = "Открыть карту",
                Location = new Point(170, 100),
                Width = 150
            };
            mapButton.Click += OpenMapButton_Click;
            this.Controls.Add(mapButton);
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(angleTextBox.Text, out double angle))
            {
                MessageBox.Show("Введите корректный угол установки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!double.TryParse(powerTextBox.Text, out double power))
            {
                MessageBox.Show("Введите корректную мощность установки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Примерные значения DNI и облачности
            double[] dniValues = { 600, 650, 700, 750, 800, 850, 900 }; // Вт/м^2
            double[] cloudinessValues = { 0.1, 0.2, 0.3, 0.1, 0.0, 0.1, 0.2 }; // Коэффициент облачности

            SolarEnergyCalculator calculator = new SolarEnergyCalculator(new PointLatLng(0, 0), power, angle);
            double[] weeklyEnergy = calculator.CalculateWeeklyEnergy(dniValues, cloudinessValues);

            SaveToTextFile(weeklyEnergy);
        }

        private void SaveToTextFile(double[] weeklyEnergy)
        {
            string filePath = "energy_output.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Выработка электроэнергии по дням недели:");
                    string[] days = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

                    for (int i = 0; i < weeklyEnergy.Length; i++)
                    {
                        writer.WriteLine($"{days[i]}: {weeklyEnergy[i]:F2} кВтч");
                    }
                }

                MessageBox.Show($"Данные сохранены в файл: {Path.GetFullPath(filePath)}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            MapForm mapForm = new MapForm();
            mapForm.Show();
        }
    }
}
