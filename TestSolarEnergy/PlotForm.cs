using System.Windows.Forms;
using OxyPlot;
using OxyPlot.WindowsForms;

public class PlotForm : Form
{
    public PlotForm(PlotModel model)
    {
        this.Text = "График выработки";
        this.Width = 800;
        this.Height = 600;

        var plotView = new PlotView
        {
            Dock = DockStyle.Fill,
            Model = model
        };

        this.Controls.Add(plotView);
    }
}
