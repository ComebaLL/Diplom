using System;
using System.Drawing;
using System.Windows.Forms;

public class PeriodSelectionDialog : Form
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    private DateTimePicker startPicker;
    private DateTimePicker endPicker;
    private Button okButton;
    private Button cancelButton;

    private readonly DateTime minDate = new DateTime(2025, 1, 1);
    private readonly DateTime maxDate = new DateTime(2030, 12, 31);

    public PeriodSelectionDialog()
    {
        Text = "Выберите период";
        Size = new Size(350, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        Label startLabel = new Label
        {
            Text = "Дата начала:",
            Location = new Point(20, 20),
            AutoSize = true
        };

        startPicker = new DateTimePicker
        {
            Value = new DateTime(2025, 1, 1),
            MinDate = minDate,
            MaxDate = maxDate,
            Location = new Point(120, 15),
            Width = 180
        };

        Label endLabel = new Label
        {
            Text = "Дата окончания:",
            Location = new Point(20, 60),
            AutoSize = true
        };

        endPicker = new DateTimePicker
        {
            Value = new DateTime(2025, 1, 2),
            MinDate = minDate,
            MaxDate = maxDate,
            Location = new Point(120, 55),
            Width = 180
        };

        okButton = new Button
        {
            Text = "ОК",
            Location = new Point(70, 110),
            Width = 80
        };

        cancelButton = new Button
        {
            Text = "Отмена",
            Location = new Point(180, 110),
            Width = 80
        };

        okButton.Click += OkButton_Click;
        cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(startLabel);
        Controls.Add(startPicker);
        Controls.Add(endLabel);
        Controls.Add(endPicker);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        if (endPicker.Value < startPicker.Value)
        {
            MessageBox.Show("Дата окончания не может быть раньше даты начала.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        StartDate = startPicker.Value;
        EndDate = endPicker.Value;
        DialogResult = DialogResult.OK;
        Close();
    }
}
