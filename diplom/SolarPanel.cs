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
    public int Count { get; set; } // Кол-во панелей 
    public bool IsChecked { get; set; } // Выбрана ли панель
    public bool IsChecked2 { get; set; } // Дополнительная отметка
    public int RotationVertical { get; set; } // Угол поворота по вертикали
    public int RotationHorizontal { get; set; } // Угол поворота по горизонтали
    public double? Latitude { get; set; } // Широта
    public double? Longitude { get; set; } // Долгота

    public int Group { get; set; } // 1 или 2

    public double ReturnEnergy { get; set; } = 0.1; // по умолчанию



    public SolarPanel(
        string type,
        double power,
        double consumptionPower,
        double? angleVertical = null,
        double? angleHorizontal = null,
        int count = 1,
        int rotationVertical = 0,
        int rotationHorizontal = 0,
        bool isChecked2 = false,
        double? latitude = null,
        double? longitude = null)
    {
        Type = type;
        Power = power;
        ConsumptionPower = consumptionPower;
        AngleVertical = angleVertical;
        AngleHorizontal = angleHorizontal;
        Count = count;
        RotationVertical = rotationVertical;
        RotationHorizontal = rotationHorizontal;
        IsChecked2 = isChecked2;
        Latitude = latitude;
        Longitude = longitude;
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
        IsChecked = newData.IsChecked;
        IsChecked2 = newData.IsChecked2;
        RotationVertical = newData.RotationVertical;
        RotationHorizontal = newData.RotationHorizontal;
        Latitude = newData.Latitude;
        Longitude = newData.Longitude;
    }

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (Power <= 0 || Power > 10000) errors.Add("Power");
        if (ConsumptionPower < 0 || ConsumptionPower > 10000) errors.Add("Consumption");

        if (Type == "Статическая")
        {
            if (AngleVertical is null or < 0 or > 90) errors.Add("AngleVertical");
            if (AngleHorizontal is null or < 0 or > 90) errors.Add("AngleHorizontal");
        }
        else if (Type == "Динамическая")
        {
            if (RotationVertical < 0 || RotationVertical > 50) errors.Add("RotationVertical");
            if (RotationHorizontal < 0 || RotationHorizontal > 50) errors.Add("RotationHorizontal");
        }

        return errors;
    }
}
