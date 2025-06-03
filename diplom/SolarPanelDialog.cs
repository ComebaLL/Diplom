using SolarPowerCalculator;
using System;
using System.Drawing;
using System.Windows.Forms;

public class SolarPanelDialog : Form
{
    private RadioButton staticOption, dynamicOption;
    private TextBox powerTextBox, angleVertTextBox, angleHorTextBox,
                    rotationVertTextBox, rotationHorTextBox, consumptionTextBox,
                    latitudeTextBox, longitudeTextBox;
    private Label powerLabel, angleVertLabel, angleHorLabel,
                  rotationVertLabel, rotationHorLabel, consumptionLabel,
                  latitudeLabel, longitudeLabel;
    private Button confirmButton, mapButton;

    private double? selectedLatitude = null;
    private double? selectedLongitude = null;

    public SolarPanel CreatedPanel { get; private set; }

    public SolarPanelDialog(SolarPanel panel = null)
    {
        Text = "Добавить солнечную панель";
        Size = new Size(340, 460);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        staticOption = new RadioButton { Text = "Статическая", Location = new Point(10, 10), Checked = panel?.Type != "Динамическая" };
        dynamicOption = new RadioButton { Text = "Динамическая", Location = new Point(10, 40), Checked = panel?.Type == "Динамическая" };
        staticOption.CheckedChanged += ToggleFields;
        dynamicOption.CheckedChanged += ToggleFields;

        powerLabel = new Label { Text = "Мощность (Вт):", Location = new Point(10, 70) };
        powerTextBox = new TextBox { Location = new Point(150, 70), Width = 150, Text = panel?.Power.ToString() ?? "" };

        angleVertLabel = new Label { Text = "Угол (вертик.):", Location = new Point(10, 100) };
        angleVertTextBox = new TextBox { Location = new Point(150, 100), Width = 150, Text = panel?.AngleVertical?.ToString() ?? "" };

        angleHorLabel = new Label { Text = "Угол (горизонт.):", Location = new Point(10, 130) };
        angleHorTextBox = new TextBox { Location = new Point(150, 130), Width = 150, Text = panel?.AngleHorizontal?.ToString() ?? "" };

        rotationVertLabel = new Label { Text = "Поворот (верт.):", Location = new Point(10, 100) };
        rotationVertTextBox = new TextBox { Location = new Point(150, 100), Width = 150, Text = panel?.RotationVertical.ToString() ?? "0" };

        rotationHorLabel = new Label { Text = "Поворот (гориз):", Location = new Point(10, 130) };
        rotationHorTextBox = new TextBox { Location = new Point(150, 130), Width = 150, Text = panel?.RotationHorizontal.ToString() ?? "0" };

        consumptionLabel = new Label { Text = "Потребление (Вт):", Location = new Point(10, 160) };
        consumptionTextBox = new TextBox { Location = new Point(150, 160), Width = 150, Text = panel?.ConsumptionPower.ToString() ?? "" };

        latitudeLabel = new Label { Text = "Широта:", Location = new Point(10, 190) };
        latitudeTextBox = new TextBox { Location = new Point(150, 190), Width = 150, Text = panel?.Latitude?.ToString() ?? "" };

        longitudeLabel = new Label { Text = "Долгота:", Location = new Point(10, 220) };
        longitudeTextBox = new TextBox { Location = new Point(150, 220), Width = 150, Text = panel?.Longitude?.ToString() ?? "" };

        mapButton = new Button { Text = "Выбрать на карте", Location = new Point(10, 250), Width = 290 };
        mapButton.Click += buttonSelectLocation_Click;

        confirmButton = new Button { Text = "Добавить", Location = new Point(10, 290), Width = 290 };
        confirmButton.Click += ConfirmButton_Click;

        Controls.AddRange(new Control[] {
            staticOption, dynamicOption,
            powerLabel, powerTextBox,
            angleVertLabel, angleVertTextBox,
            angleHorLabel, angleHorTextBox,
            rotationVertLabel, rotationVertTextBox,
            rotationHorLabel, rotationHorTextBox,
            consumptionLabel, consumptionTextBox,
            latitudeLabel, latitudeTextBox,
            longitudeLabel, longitudeTextBox,
            mapButton, confirmButton
        });

        ToggleFields(null, null);
    }

    private void ToggleFields(object sender, EventArgs e)
    {
        bool isStatic = staticOption.Checked;

        angleVertLabel.Visible = angleVertTextBox.Visible = isStatic;
        angleHorLabel.Visible = angleHorTextBox.Visible = isStatic;

        rotationVertLabel.Visible = rotationVertTextBox.Visible = !isStatic;
        rotationHorLabel.Visible = rotationHorTextBox.Visible = !isStatic;

        consumptionLabel.Visible = consumptionTextBox.Visible = !isStatic;
    }

    private void buttonSelectLocation_Click(object sender, EventArgs e)
    {
        using (var mapForm = new MapForm())
        {
            mapForm.AverageCoordinatesSelected += point =>
            {
                selectedLatitude = point.Lat;
                selectedLongitude = point.Lng;
                latitudeTextBox.Text = point.Lat.ToString("F6");
                longitudeTextBox.Text = point.Lng.ToString("F6");
            };

            mapForm.ShowDialog();
        }
    }

    private void ConfirmButton_Click(object sender, EventArgs e)
    {
        powerTextBox.BackColor = consumptionTextBox.BackColor =
        angleVertTextBox.BackColor = angleHorTextBox.BackColor =
        rotationVertTextBox.BackColor = rotationHorTextBox.BackColor =
        latitudeTextBox.BackColor = longitudeTextBox.BackColor = Color.White;

        bool isStatic = staticOption.Checked;
        bool hasError = false;

        double.TryParse(powerTextBox.Text, out double power);
        double.TryParse(consumptionTextBox.Text, out double consumption);

        double? angleVert = null, angleHor = null;
        if (isStatic)
        {
            if (double.TryParse(angleVertTextBox.Text, out double av)) angleVert = av;
            if (double.TryParse(angleHorTextBox.Text, out double ah)) angleHor = ah;
        }

        int.TryParse(rotationVertTextBox.Text, out int rotV);
        int.TryParse(rotationHorTextBox.Text, out int rotH);

        double.TryParse(latitudeTextBox.Text, out double lat);
        double.TryParse(longitudeTextBox.Text, out double lon);

        var panel = new SolarPanel(
            isStatic ? "Статическая" : "Динамическая",
            power,
            consumption,
            angleVert,
            angleHor,
            1,
            rotV,
            rotH
        )
        {
            Latitude = lat,
            Longitude = lon
        };

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

        if (errors.Contains("Latitude")) { latitudeTextBox.BackColor = Color.MistyRose; hasError = true; }
        if (errors.Contains("Longitude")) { longitudeTextBox.BackColor = Color.MistyRose; hasError = true; }

        if (hasError) return;

        CreatedPanel = panel;
        DialogResult = DialogResult.OK;
        Close();
    }
}
