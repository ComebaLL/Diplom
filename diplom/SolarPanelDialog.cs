using System;
using System.Drawing;
using System.Windows.Forms;

public class SolarPanelDialog : Form
{
    private RadioButton staticOption, dynamicOption;
    private TextBox powerTextBox, angleVertTextBox, angleHorTextBox,
                    rotationVertTextBox, rotationHorTextBox, consumptionTextBox;
    private Label powerLabel, angleVertLabel, angleHorLabel,
                  rotationVertLabel, rotationHorLabel, consumptionLabel;
    private Button confirmButton;

    public SolarPanel CreatedPanel { get; private set; }

    public SolarPanelDialog(SolarPanel panel = null)
    {
        Text = "Добавить солнечную панель";
        Size = new Size(320, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Тип панели
        staticOption = new RadioButton { Text = "Статическая", Location = new Point(10, 10), Checked = panel?.Type != "Динамическая" };
        dynamicOption = new RadioButton { Text = "Динамическая", Location = new Point(10, 40), Checked = panel?.Type == "Динамическая" };
        staticOption.CheckedChanged += ToggleFields;
        dynamicOption.CheckedChanged += ToggleFields;

        // Мощность
        powerLabel = new Label { Text = "Мощность (Вт):", Location = new Point(10, 70) };
        powerTextBox = new TextBox { Location = new Point(150, 70), Width = 120, Text = panel?.Power.ToString() ?? "" };

        // Углы для статических панелей
        angleVertLabel = new Label { Text = "Угол (вертик.):", Location = new Point(10, 100) };
        angleVertTextBox = new TextBox { Location = new Point(150, 100), Width = 120, Text = panel?.AngleVertical?.ToString() ?? "" };

        angleHorLabel = new Label { Text = "Угол (горизонт.):", Location = new Point(10, 130) };
        angleHorTextBox = new TextBox { Location = new Point(150, 130), Width = 120, Text = panel?.AngleHorizontal?.ToString() ?? "" };

        // Повороты для динамических панелей
        rotationVertLabel = new Label { Text = "Поворот (верт.):", Location = new Point(10, 100) };
        rotationVertTextBox = new TextBox { Location = new Point(150, 100), Width = 120, Text = "0" };

        rotationHorLabel = new Label { Text = "Поворот (гориз.):", Location = new Point(10, 130) };
        rotationHorTextBox = new TextBox { Location = new Point(150, 130), Width = 120, Text = "0" };

        // Потребление
        consumptionLabel = new Label { Text = "Потребление (Вт):", Location = new Point(10, 170) };
        consumptionTextBox = new TextBox { Location = new Point(150, 170), Width = 120, Text = panel?.ConsumptionPower.ToString() ?? "" };

        // Кнопка
        confirmButton = new Button { Text = "Добавить", Location = new Point(10, 220), Width = 260 };
        confirmButton.Click += ConfirmButton_Click;

        Controls.AddRange(new Control[] {
            staticOption, dynamicOption,
            powerLabel, powerTextBox,
            angleVertLabel, angleVertTextBox,
            angleHorLabel, angleHorTextBox,
            rotationVertLabel, rotationVertTextBox,
            rotationHorLabel, rotationHorTextBox,
            consumptionLabel, consumptionTextBox,
            confirmButton
        });

        // Отобразим соответствующие поля
        ToggleFields(null, null);

        // Если редактируем — заполним повороты
        if (panel != null && panel.Type == "Динамическая")
        {
            rotationVertTextBox.Text = panel.RotationVertical.ToString();
            rotationHorTextBox.Text = panel.RotationHorizontal.ToString();
        }
    }

    private void ToggleFields(object sender, EventArgs e)
    {
        bool isStatic = staticOption.Checked;

        angleVertLabel.Visible = angleVertTextBox.Visible = isStatic;
        angleHorLabel.Visible = angleHorTextBox.Visible = isStatic;

        rotationVertLabel.Visible = rotationVertTextBox.Visible = !isStatic;
        rotationHorLabel.Visible = rotationHorTextBox.Visible = !isStatic;
    }

    private void ConfirmButton_Click(object sender, EventArgs e)
    {
        // Сбрасываем цвет фона
        powerTextBox.BackColor = Color.White;
        consumptionTextBox.BackColor = Color.White;
        angleVertTextBox.BackColor = Color.White;
        angleHorTextBox.BackColor = Color.White;
        rotationVertTextBox.BackColor = Color.White;
        rotationHorTextBox.BackColor = Color.White;

        // Парсим значения
        bool isStatic = staticOption.Checked;
        bool hasError = false;

        double.TryParse(powerTextBox.Text, out double power);
        double.TryParse(consumptionTextBox.Text, out double consumption);
        double? angleVert = null, angleHor = null;
        int rotV = 0, rotH = 0;

        if (isStatic)
        {
            if (double.TryParse(angleVertTextBox.Text, out double aV)) angleVert = aV;
            if (double.TryParse(angleHorTextBox.Text, out double aH)) angleHor = aH;
        }
        else
        {
            int.TryParse(rotationVertTextBox.Text, out rotV);
            int.TryParse(rotationHorTextBox.Text, out rotH);
        }

        var panel = new SolarPanel(
            isStatic ? "Статическая" : "Динамическая",
            power,
            consumption,
            angleVert,
            angleHor,
            1,
            rotV,
            rotH
        );

        var errors = panel.Validate();

        if (errors.Contains("Power")) { powerTextBox.BackColor = Color.MistyRose; hasError = true; }
        if (errors.Contains("Consumption")) { consumptionTextBox.BackColor = Color.MistyRose; hasError = true; }

        if (isStatic)
        {
            if (errors.Contains("AngleVertical")) { angleVertTextBox.BackColor = Color.MistyRose; hasError = true; }
            if (errors.Contains("AngleHorizontal")) { angleHorTextBox.BackColor = Color.MistyRose; hasError = true; }
        }
        else
        {
            if (errors.Contains("RotationVertical")) { rotationVertTextBox.BackColor = Color.MistyRose; hasError = true; }
            if (errors.Contains("RotationHorizontal")) { rotationHorTextBox.BackColor = Color.MistyRose; hasError = true; }
        }

        if (hasError) return;

        CreatedPanel = panel;
        DialogResult = DialogResult.OK;
        Close();
    }


}
