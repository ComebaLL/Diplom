using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SolarPanel
{
    public string Type { get; set; } // Статическая / Динамическая
    public double Power { get; set; } // Мощность (Вт)
    public double? AngleVertical { get; set; } // Угол по вертикали (только для статичных)
    public double? AngleHorizontal { get; set; } // Угол по горизонтали (только для статичных)
     
    public double? ConsumptionPower { get; set; } // потребляемая мощность

    public SolarPanel(string type, double power, double? angleVertical = null, double? angleHorizontal = null, double? consumptionPower = null)
    {
        Type = type;
        Power = power;
        AngleVertical = angleVertical;
        AngleHorizontal = angleHorizontal;
        ConsumptionPower = consumptionPower;
    }

    public override string ToString()
    {
        string baseInfo = Type == "Статическая"
            ? $"{Type} | {Power} Вт | Вертик.: {AngleVertical}° | Горизонт.: {AngleHorizontal}°"
            : $"{Type} | {Power} Вт";

        return $"{baseInfo} | Потребление: {ConsumptionPower} Вт";
    }

    public void Update(SolarPanel newData)
    {
        Power = newData.Power;
        AngleVertical = newData.AngleVertical;
        AngleHorizontal = newData.AngleHorizontal;
        ConsumptionPower = newData.ConsumptionPower;
    }
}

