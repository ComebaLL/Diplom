using GMap.NET.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

public class SolarPanel
{
    public string Type { get; set; } // Статическая или динамическая
    public double Power { get; set; } // Мощность панели (Вт)
    public double ConsumptionPower { get; set; } // Потребление (Вт)
    public double? AngleVertical { get; set; } // Угол наклона (вертикальный)
    public double? AngleHorizontal { get; set; } // Азимут (горизонтальный)

    public int Count { get; set; } // кол-во панелей 

    public bool IsChecked { get; set; } // проверка выбранна ли панель

    public SolarPanel(string type, double power, double consumptionPower, double? angleVertical = null, double? angleHorizontal = null, int count = 1)

    {
        Type = type;
        Power = power;
        ConsumptionPower = consumptionPower;
        AngleVertical = angleVertical;
        AngleHorizontal = angleHorizontal;
        Count = count;
    }

    public override string ToString()
    {
        return $"{Type} | {Power} Вт | Потребление: {ConsumptionPower} Вт | Кол-во: {Count}";
    }


    public void Update(SolarPanel newData)
    {
        Type = newData.Type;
        Power = newData.Power;
        AngleVertical = newData.AngleVertical;
        AngleHorizontal = newData.AngleHorizontal;
        ConsumptionPower = newData.ConsumptionPower;
        Count = newData.Count;
    }

}

