using System;
using System.Drawing;
using System.Windows.Forms;

public class SolarPanelDialog : Form
{
    private RadioButton staticOption, dynamicOption;
    private TextBox powerTextBox, angleVertTextBox, angleHorTextBox, consumptionTextBox;
    private Label powerLabel, angleVertLabel, angleHorLabel, consumptionLabel;
    private Button confirmButton;
    public SolarPanel CreatedPanel { get; private set; }

    public SolarPanelDialog(SolarPanel panel = null)
    {
        Text = "Добавить солнечную панель";
        Size = new Size(300, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // 🔹 Выбор типа панели
        staticOption = new RadioButton { Text = "Статическая", Location = new Point(10, 10), Checked = panel?.Type == "Статическая" };
        dynamicOption = new RadioButton { Text = "Динамическая", Location = new Point(10, 40), Checked = panel?.Type == "Динамическая" };

        staticOption.CheckedChanged += ToggleAngleFields;
        dynamicOption.CheckedChanged += ToggleAngleFields;

        // 🔹 Поля для ввода значений
        powerLabel = new Label { Text = "Мощность (Вт):", Location = new Point(10, 70) };
        powerTextBox = new TextBox { Location = new Point(120, 70), Width = 100, Text = panel?.Power.ToString() ?? "" };

        angleVertLabel = new Label { Text = "Угол (вертик.)", Location = new Point(10, 100), Visible = staticOption.Checked };
        angleVertTextBox = new TextBox { Location = new Point(120, 100), Width = 100, Visible = staticOption.Checked, Text = panel?.AngleVertical?.ToString() ?? "" };

        angleHorLabel = new Label { Text = "Угол (горизонт.)", Location = new Point(10, 130), Visible = staticOption.Checked };
        angleHorTextBox = new TextBox { Location = new Point(120, 130), Width = 100, Visible = staticOption.Checked, Text = panel?.AngleHorizontal?.ToString() ?? "" };

        // 🔹 Поле для потребляемой мощности
        consumptionLabel = new Label { Text = "Потребление (Вт):", Location = new Point(10, 160) };
        consumptionTextBox = new TextBox { Location = new Point(120, 160), Width = 100, Text = panel?.ConsumptionPower.ToString() ?? "" };

        // 🔹 Кнопка подтверждения
        confirmButton = new Button { Text = "Добавить", Location = new Point(10, 200), Width = 200 };
        confirmButton.Click += ConfirmButton_Click;

        Controls.AddRange(new Control[] {
            staticOption, dynamicOption,
            powerLabel, powerTextBox,
            angleVertLabel, angleVertTextBox,
            angleHorLabel, angleHorTextBox,
            consumptionLabel, consumptionTextBox,
            confirmButton
        });

        // Если передана существующая панель — обновляем поля
        if (panel != null)
        {
            ToggleAngleFields(null, null);
        }
    }

    /// <summary>
    /// 🔹 Отображение полей для углов при выборе статической панели
    /// </summary>
    private void ToggleAngleFields(object sender, EventArgs e)
    {
        bool isStatic = staticOption.Checked;
        angleVertLabel.Visible = angleVertTextBox.Visible = isStatic;
        angleHorLabel.Visible = angleHorTextBox.Visible = isStatic;
    }

    /// <summary>
    /// 🔹 Обработчик нажатия на кнопку "Добавить"
    /// </summary>
    private void ConfirmButton_Click(object sender, EventArgs e)
    {
        if (!double.TryParse(powerTextBox.Text, out double power) || power <= 0)
        {
            MessageBox.Show("Введите корректную мощность.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!double.TryParse(consumptionTextBox.Text, out double consumption) || consumption < 0)
        {
            MessageBox.Show("Введите корректную потребляемую мощность.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        double? angleVert = null, angleHor = null;

        if (staticOption.Checked)
        {
            if (!double.TryParse(angleVertTextBox.Text, out double angleV) || angleV < 0 || angleV > 90)
            {
                MessageBox.Show("Введите корректный вертикальный угол (0-90°).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!double.TryParse(angleHorTextBox.Text, out double angleH) || angleH < 0 || angleH > 90)
            {
                MessageBox.Show("Введите корректный горизонтальный угол (0-90°).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            angleVert = angleV;
            angleHor = angleH;
        }

        CreatedPanel = new SolarPanel(
            staticOption.Checked ? "Статическая" : "Динамическая",
            power,
            angleVert,
            angleHor,
            consumption
        );

        DialogResult = DialogResult.OK;
        Close();
    }
}
