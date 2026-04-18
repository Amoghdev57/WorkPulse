namespace WorkPulse.Models
{
    public class SalaryEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal HRA { get; set; }
        public decimal Bonus { get; set; }
        public decimal PFDeduction { get; set; }
        public decimal TaxDeduction { get; set; }
        public decimal NetPay { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
        public decimal GrossPay => BasicSalary + HRA + Bonus;
        public decimal TotalDeductions => PFDeduction + TaxDeduction;
    }
}