using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private MenuStrip menuStrip;
        private Button openSelectionButton; //  Кнопка для открытия выбора панелей
        private const string FilePath = "solar_panels.json";

        public MainForm()
        {
            Text = "Solar Power Calculator";
            Size = new Size(650, 500);
            KeyPreview = true;

            //  Добавляем меню
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            var saveMenuItem = new ToolStripMenuItem("Сохранить список", null, SavePanels) { ShortcutKeys = Keys.Control | Keys.S };
            var loadMenuItem = new ToolStripMenuItem("Загрузить список", null, LoadPanels) { ShortcutKeys = Keys.Control | Keys.O };
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { saveMenuItem, loadMenuItem });
            menuStrip.Items.Add(fileMenu);
            Controls.Add(menuStrip);
            ClearOldEnergyFiles();

            //  Контейнер для панелей
            panelContainer = new FlowLayoutPanel
            {
                Location = new Point(10, 30),
                Size = new Size(600, 350),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelContainer);

            //  Кастомная кнопка "Добавить панель"
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

            //  Кнопка для открытия выбора панелей
            openSelectionButton = new Button
            {
                Text = "Выбрать панели для расчета",
                Location = new Point(10, 400),
                Width = 250,
                Height = 40
            };
            openSelectionButton.Click += OpenPanelSelection;
            Controls.Add(openSelectionButton);

            //LoadPanels(); // Загружаем панели при старте
        }


        ///  Отображение кнопки "+" для добавления панели

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


        ///  Добавление новой панели

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


        ///  Добавляет панель в список и интерфейс

        private void AddPanel(SolarPanel panel)
        {
            solarPanels.Add(panel);
            var panelControl = CreatePanelControl(panel);
            panelContainer.Controls.Add(panelControl);
            panelContainer.Controls.SetChildIndex(addPanelButton, panelContainer.Controls.Count - 1);
        }


        ///  Создаёт элемент управления для панели

        private Panel CreatePanelControl(SolarPanel panel)
        {
            var panelControl = new Panel
            {
                Size = new Size(200, 100),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = panel
            };

            string anglesInfo = panel.Type == "Динамическая"
                ? $"Повороты: V={panel.RotationVertical}, H={panel.RotationHorizontal}"
                : $"Углы: V={panel.AngleVertical}°, H={panel.AngleHorizontal}°";

            var label = new Label
            {
                Text = $"{panel.Type} | {panel.Power} Вт | Потр: {panel.ConsumptionPower} Вт\n{anglesInfo}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var countControl = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = panel.Count,
                Dock = DockStyle.Bottom,
                Width = 50
            };
            countControl.ValueChanged += (s, e) =>
            {
                panel.Count = (int)countControl.Value;
                label.Text = $"{panel.Type} | {panel.Power} Вт | Потр: {panel.ConsumptionPower} Вт\n{anglesInfo}";
            };

            var editButton = new Button
            {
                Text = "E",
                Dock = DockStyle.Left,
                Width = 30
            };
            editButton.Click += (s, e) => EditPanel(panel, panelControl, label);

            var removeButton = new Button
            {
                Text = "D",
                Dock = DockStyle.Right,
                Width = 30
            };
            removeButton.Click += (s, e) => RemovePanel(panel, panelControl);

            panelControl.Controls.Add(label);
            panelControl.Controls.Add(countControl);
            panelControl.Controls.Add(editButton);
            panelControl.Controls.Add(removeButton);

            return panelControl;
        }


        ///  Редактирование панели

        private void EditPanel(SolarPanel panel, Panel panelControl, Label label)
        {
            using (var dialog = new SolarPanelDialog(panel))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    panel.Update(dialog.CreatedPanel);
                    label.Text = panel.ToString();
                }
            }
        }


        ///  Удаление панели

        private void RemovePanel(SolarPanel panel, Panel panelControl)
        {
            solarPanels.Remove(panel);
            panelContainer.Controls.Remove(panelControl);
        }

        ///  Сохранение списка панелей в JSON

        private void SavePanels(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                saveFileDialog.Title = "Сохранить список панелей";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(solarPanels, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(saveFileDialog.FileName, json);
                        MessageBox.Show("Список панелей сохранён!", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }



        ///  Загрузка списка панелей из JSON
        private void LoadPanels(object sender = null, EventArgs e = null)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*";
                openFileDialog.Title = "Загрузить список панелей";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(openFileDialog.FileName);
                        solarPanels = JsonSerializer.Deserialize<List<SolarPanel>>(json) ?? new List<SolarPanel>();

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


        ///  Открывает окно выбора панелей для расчета
        private void OpenPanelSelection(object sender, EventArgs e)
        {
            using (var selectionForm = new PanelSelectionForm(solarPanels))
            {
                selectionForm.ShowDialog();
            }
        }

        ///  Обработчик горячих клавиш
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                SavePanels(this, EventArgs.Empty);
                return true;
            }
            if (keyData == (Keys.Control | Keys.O))
            {
                LoadPanels(this, EventArgs.Empty);
                return true;
            }
            /*
            if (keyData == (Keys.Control | Keys.N))
            {
                CreatePanelControl(SolarPanel panel);
                return true;
            }*/
                    
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ClearOldEnergyFiles()
        {
            string[] filesToClear =
            {
        "group1_output.txt",
        "group2_output.txt",
        
    };

            foreach (var file in filesToClear)
            {
                try
                {
                    if (File.Exists(file))
                        File.WriteAllText(file, string.Empty); // Очистить файл
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Не удалось очистить файл {file}: {ex.Message}");
                }
            }
        }

    }
}