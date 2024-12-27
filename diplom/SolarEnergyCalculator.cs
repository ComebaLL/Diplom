using System;
using GMap.NET;

namespace SolarPowerCalculator
{
    public class SolarEnergyCalculator
    {
        private PointLatLng _coordinates;
        private double _nominalPower;
        private double _currentAngle;

        public SolarEnergyCalculator(PointLatLng coordinates, double nominalPower, double initialAngle = 0)
        {
            _coordinates = coordinates;
            _nominalPower = nominalPower;
            _currentAngle = initialAngle;
        }

        public void SetCurrentAngle(double angle)
        {
            _currentAngle = angle;
        }

        public double GetCurrentAngle()
        {
            return _currentAngle;
        }

        public double CalculatePower(double dni, double cloudiness)
        {
            double effectiveDni = dni * (1 - cloudiness);
            double incidenceAngleRad = _currentAngle * Math.PI / 180; // Примерный угол
            double power = _nominalPower * Math.Cos(incidenceAngleRad) * effectiveDni / 1000;
            return power;
        }

        public double[] CalculateWeeklyEnergy(double[] dniValues, double[] cloudinessValues)
        {
            if (dniValues.Length != cloudinessValues.Length)
            {
                throw new ArgumentException("Массивы DNI и облачности должны быть одинаковой длины.");
            }

            double[] dailyEnergy = new double[dniValues.Length];
            for (int i = 0; i < dniValues.Length; i++)
            {
                dailyEnergy[i] = CalculatePower(dniValues[i], cloudinessValues[i]);
            }

            return dailyEnergy;
        }
    }
}
