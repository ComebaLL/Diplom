using OfficeOpenXml;
using SolarPowerCalculator;
/*
 ��������� ���������, ������� �������� ������������ ��� ���-�� ������� ���������� �� �� ��������(�������� ����� ���� ������); 2 ���� �� ���������\�����������;
������� UI ����� ������� ��������� ��� ���������� ��������� ��� ��� �����, � ��������� ���������� ����� ������ ���������� �������� ��������, ����������� ��������������(�� 1 ���� �� ����\��������)
�� ����� ������� (�����, ������, �����, ���, 3-5-10 ���) ������� ���, 3���� ����� ������� �� ���, 5-10 ��� �� ������� ����� �������;
����� ������������, ��� ����� ������������ ����������� ����������, ���� �������� ��������� ������, ���� �����������;
�������� � ��������� ��������� �����������;
*/
/* UI, �����, ������� ������*/
namespace diplom
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}