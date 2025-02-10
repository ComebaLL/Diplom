namespace SolarPowerCalculator
{
    public class SolarPanelDialog : Form
    {
        private RadioButton staticOption;
        private RadioButton dynamicOption;
        private TextBox powerTextBox;
        private TextBox angleTextBox1;
        private TextBox angleTextBox2;
        private Button confirmButton;
        public Panel CreatedPanel { get; private set; }

        public SolarPanelDialog()
        {
            this.Text = "���������� ��������� ������";
            this.Size = new Size(300, 300);

            // ����� ���� ������
            staticOption = new RadioButton
            {
                Text = "���������",
                Location = new Point(10, 10),
                Checked = true
            };
            this.Controls.Add(staticOption);

            dynamicOption = new RadioButton
            {
                Text = "����������",
                Location = new Point(10, 40)
            };
            this.Controls.Add(dynamicOption);

            // ���� ��� ��������
            Label powerLabel = new Label
            {
                Text = "�������� (��):",
                Location = new Point(10, 80),
                Width = 100
            };
            this.Controls.Add(powerLabel);

            powerTextBox = new TextBox
            {
                Location = new Point(120, 80),
                Width = 100
            };
            this.Controls.Add(powerTextBox);

            // ���� ��� �����
            Label angleLabel1 = new Label
            {
                Text = "���� 1 (����):",
                Location = new Point(10, 120),
                Width = 100
            };
            this.Controls.Add(angleLabel1);

            angleTextBox1 = new TextBox
            {
                Location = new Point(120, 120),
                Width = 100
            };
            this.Controls.Add(angleTextBox1);

            Label angleLabel2 = new Label
            {
                Text = "���� 2 (����):",
                Location = new Point(10, 160),
                Width = 100,
                Visible = false
            };
            this.Controls.Add(angleLabel2);

            angleTextBox2 = new TextBox
            {
                Location = new Point(120, 160),
                Width = 100,
                Visible = false
            };
            this.Controls.Add(angleTextBox2);

            // ������� ������������ ���� ������
            dynamicOption.CheckedChanged += (s, e) =>
            {
                angleLabel2.Visible = angleTextBox2.Visible = dynamicOption.Checked;
            };

            // ������ �������������
            confirmButton = new Button
            {
                Text = "��������",
                Location = new Point(10, 200),
                Width = 100
            };
            confirmButton.Click += ConfirmButton_Click;
            this.Controls.Add(confirmButton);
        }

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(powerTextBox.Text, out double power) || power <= 0)
            {
                MessageBox.Show("������� ���������� ��������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!double.TryParse(angleTextBox1.Text, out double angle1) || angle1 < 0 || angle1 > 90)
            {
                MessageBox.Show("������� ���������� ���� 1.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double? angle2 = null;
            if (dynamicOption.Checked)
            {
                if (!double.TryParse(angleTextBox2.Text, out double angle2Value) || angle2Value < 0 || angle2Value > 90)
                {
                    MessageBox.Show("������� ���������� ���� 2.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                angle2 = angle2Value;
            }

            // ������ ������ ��� ����������� �� ������� �����
            CreatedPanel = new Panel
            {
                Size = new Size(200, 50),
                BorderStyle = BorderStyle.FixedSingle
            };

            string panelInfo = $"��������: {power} ��, ���� 1: {angle1}�";
            if (angle2.HasValue)
            {
                panelInfo += $", ���� 2: {angle2}�";
            }

            Label panelLabel = new Label
            {
                Text = panelInfo,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            CreatedPanel.Controls.Add(panelLabel);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
