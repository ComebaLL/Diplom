using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace diplom
{
    public class EnergyExcelWriter
    {
        private readonly string _filePath;

        public EnergyExcelWriter(string filePath)
        {
            _filePath = filePath;
        }

        public void AddSheetFromTxtFile(string sheetName, string txtFile, List<SolarPanel> panels)
        {
            var workbook = File.Exists(_filePath) ? new XLWorkbook(_filePath) : new XLWorkbook();

            if (workbook.Worksheets.Contains(sheetName))
                workbook.Worksheets.Delete(sheetName);

            var sheet = workbook.Worksheets.Add(sheetName);

            // --- Чтение выработки из TXT файла ---
            var lines = File.ReadAllLines(txtFile);
            int row = 1;

            sheet.Cell(row, 1).Value = "Дата";
            sheet.Cell(row, 2).Value = "Выработка (кВт·ч)";
            sheet.Column(1).Width = 20;
            row++;

            foreach (var line in lines.Where(l => l.Contains(":")))
            {
                var parts = line.Split(':');
                if (DateTime.TryParse(parts[0], out var date))
                {
                    sheet.Cell(row, 1).Value = date;
                    sheet.Cell(row, 2).Value = parts[1].Trim();
                    row++;
                }
                else if (line.StartsWith("Общая выработка"))
                {
                    sheet.Cell(row++, 1).Value = line;
                }
            }

            // --- Вывод характеристик панелей ---
            int panelRow = 1;
            int col = 4;
            sheet.Cell(panelRow++, col).Value = "Характеристики панелей";

            foreach (var p in panels)
            {
                sheet.Cell(panelRow++, col).Value = $"Тип: {p.Type}";
                sheet.Cell(panelRow++, col).Value = $"Мощность: {p.Power} Вт";

                if (p.Type == "Static")
                {
                    sheet.Cell(panelRow++, col).Value = $"Азимут: {p.AngleHorizontal}";
                    sheet.Cell(panelRow++, col).Value = $"Наклон: {p.AngleVertical}";
                }
                else if (p.Type == "Dynamic")
                {
                    sheet.Cell(panelRow++, col).Value = $"Потребление: {p.ConsumptionPower} Вт";
                    sheet.Cell(panelRow++, col).Value = $"Повороты по вертикали: {p.RotationVertical}";
                    sheet.Cell(panelRow++, col).Value = $"Повороты по горизонтали: {p.RotationHorizontal}";
                }

                sheet.Cell(panelRow++, col).Value = $"Широта: {p.Latitude}";
                sheet.Cell(panelRow++, col).Value = $"Долгота: {p.Longitude}";
                panelRow++; // пустая строка между панелями
            }

            workbook.SaveAs(_filePath);
        }
    }
}
