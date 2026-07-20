namespace Domain.Entities
{
    public class QualityApprover
    {
        public int Id { get; set; }

        public int PayrollNumber { get; set; }

        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ScanDetail> ScanDetails { get; set; } = new List<ScanDetail>();
    }
}