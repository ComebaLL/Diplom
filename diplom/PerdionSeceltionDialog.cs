using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PeriodSelectionDialog : Form
{
    public enum PeriodOption { Week, Month, Year }

    public PeriodOption SelectedPeriod { get; private set; }

    public PeriodSelectionDialog()
    {
        Text = "Выберите период расчета";
        Size = new Size(300, 150);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var weekButton = new Button { Text = "Неделя", Location = new Point(30, 30), Width = 75 };
        var monthButton = new Button { Text = "Месяц", Location = new Point(110, 30), Width = 75 };
        var yearButton = new Button { Text = "Год", Location = new Point(190, 30), Width = 75 };

        weekButton.Click += (s, e) => { SelectedPeriod = PeriodOption.Week; DialogResult = DialogResult.OK; };
        monthButton.Click += (s, e) => { SelectedPeriod = PeriodOption.Month; DialogResult = DialogResult.OK; };
        yearButton.Click += (s, e) => { SelectedPeriod = PeriodOption.Year; DialogResult = DialogResult.OK; };

        Controls.Add(weekButton);
        Controls.Add(monthButton);
        Controls.Add(yearButton);
    }
}
