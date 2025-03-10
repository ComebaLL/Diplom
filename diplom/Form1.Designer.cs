using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using GMap.NET;

namespace SolarPowerCalculator
{
    public class MainForm : Form
    {
        private List<SolarPanel> solarPanels = new List<SolarPanel>();
        private FlowLayoutPanel panelContainer;
        private Panel addPanelButton;
        private Button selectPanelButton; // 🔹 Новая кнопка для открытия PanelSelectionForm
        private MenuStrip menuStrip;
        private const string FilePath = "solar_panels.json";

        public MainForm()
        {
            Text = "Solar Power Calculator";
            Size = new Size(650, 500);

            // 🔹 Добавляем меню
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            var saveMenuItem = new ToolStripMenuItem("Сохранить список", null, SavePanels);
            var loadMenuItem = new ToolStripMenuItem("Загрузить список", null, LoadPanels);
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { saveMenuItem, loadMenuItem });
            menuStrip.Items.Add(fileMenu);
            Controls.Add(menuStrip);

            // 🔹 Контейнер для панелей
            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 30),
                Size = new Size(600, 350),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelContainer);

            // 🔹 Кастомная кнопка "Добавить панель"
            addPanelButton = new Panel
            {
                Size = new Size(100, 100),
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            addPanelButton.Paint += AddPanelButton_Paint;
            addPanelButton.Click += AddPanelButton_Click;
            panelContainer.Controls.Add(addPanelButton);

            // 🔹 Новая кнопка "Выбрать панель"
            selectPanelButton = new Button
            {
                Text = "Выбрать панель",
                Location = new Point(10, 400),
                Width = 200
            };
            selectPanelButton.Click += OpenPanelSelectionForm;
            Controls.Add(selectPanelButton);

            LoadPanels(); // Загружаем панели при старте
        }

        /// <summary>
        /// 🔹 Открывает окно выбора панели
        /// </summary>
        private void OpenPanelSelectionForm(object sender, EventArgs e)
        {
            var selectionForm = new PanelSelectionForm(solarPanels);
            selectionForm.ShowDialog();
        }

        /// <summary>
        /// 🔹 Отображение кнопки "+" для добавления панели
        /// </summary>
        private void AddPanelButton_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Black, 4))
            {
                int midX = addPanelButton.Width / 2;
                int midY = addPanelButton.Height / 2;
                int size = 20;
                e.Graphics.DrawLine(pen, midX - size, midY, midX + size, midY);
                e.Graphics.DrawLine(pen, midX, midY - size, midX, midY + size);
            }
        }

        /// <summary>
        /// 🔹 Добавление новой панели
        /// </summary>
        private void AddPanelButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SolarPanelDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    AddPanel(dialog.CreatedPanel);
                }
            }
        }

        /// <summary>
        /// 🔹 Добавляет панель в список и интерфейс
        /// </summary>
        private void AddPanel(SolarPanel panel)
        {
            solarPanels.Add(panel);
            var panelControl = CreatePanelControl(panel);
            panelContainer.Controls.Add(panelControl);
            panelContainer.Controls.SetChildIndex(addPanelButton, panelContainer.Controls.Count - 1);
        }

        /// <summary>
        /// 🔹 Создаёт элемент управления для панели
        /// </summary>
        private Panel CreatePanelControl(SolarPanel panel)
        {
            var panelControl = new Panel
            {
                Size = new Size(200, 80),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = panel
            };

            var label = new Label
            {
                Text = panel.ToString(),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var removeButton = new Button
            {
                Text = "Удалить",
                Dock = DockStyle.Bottom,
                Height = 25
            };
            removeButton.Click += (s, e) => RemovePanel(panel, panelControl);

            panelControl.Controls.Add(label);
            panelControl.Controls.Add(removeButton);

            return panelControl;
        }

        /// <summary>
        /// 🔹 Удаление панели
        /// </summary>
        private void RemovePanel(SolarPanel panel, Panel panelControl)
        {
            solarPanels.Remove(panel);
            panelContainer.Controls.Remove(panelControl);
        }

        /// <summary>
        /// 🔹 Сохранение списка панелей в JSON
        /// </summary>
        private void SavePanels(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(solarPanels, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show("Список панелей сохранён!", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 🔹 Загрузка списка панелей из JSON
        /// </summary>
        private void LoadPanels(object sender = null, EventArgs e = null)
        {
            if (!File.Exists(FilePath)) return;
            try
            {
                solarPanels = JsonSerializer.Deserialize<List<SolarPanel>>(File.ReadAllText(FilePath)) ?? new List<SolarPanel>();
                panelContainer.Controls.Clear();
                panelContainer.Controls.Add(addPanelButton);
                foreach (var panel in solarPanels)
                {
                    panelContainer.Controls.Add(CreatePanelControl(panel));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
