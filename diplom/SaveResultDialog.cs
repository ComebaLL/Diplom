using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace diplom
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public class SaveResultsDialog : Form
    {
        private CheckBox checkBoxGroup1;
        private CheckBox checkBoxGroup2;
        private Button buttonOK;
        private Button buttonCancel;

        public bool SaveGroup1 => checkBoxGroup1.Checked;
        public bool SaveGroup2 => checkBoxGroup2.Checked;

        public SaveResultsDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.checkBoxGroup1 = new CheckBox();
            this.checkBoxGroup2 = new CheckBox();
            this.buttonOK = new Button();
            this.buttonCancel = new Button();

            // 
            // checkBoxGroup1
            // 
            this.checkBoxGroup1.AutoSize = true;
            this.checkBoxGroup1.Location = new Point(20, 20);
            this.checkBoxGroup1.Text = "Сохранить результаты группы 1";

            // 
            // checkBoxGroup2
            // 
            this.checkBoxGroup2.AutoSize = true;
            this.checkBoxGroup2.Location = new Point(20, 50);
            this.checkBoxGroup2.Text = "Сохранить результаты группы 2";

            // 
            // buttonOK
            // 
            this.buttonOK.Location = new Point(30, 90);
            this.buttonOK.Size = new Size(80, 30);
            this.buttonOK.Text = "OK";
            this.buttonOK.DialogResult = DialogResult.OK;

            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new Point(130, 90);
            this.buttonCancel.Size = new Size(80, 30);
            this.buttonCancel.Text = "Отмена";
            this.buttonCancel.DialogResult = DialogResult.Cancel;

            // 
            // SaveResultsDialog
            // 
            this.ClientSize = new Size(250, 140);
            this.Controls.Add(this.checkBoxGroup1);
            this.Controls.Add(this.checkBoxGroup2);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Сохранить результаты";
            this.AcceptButton = this.buttonOK;
            this.CancelButton = this.buttonCancel;
        }
    }

}
