namespace Domain.Entities
{
    public class ContainerValidation
    {
        public int Id { get; set; }

        public string ContainerNumber { get; set; } = string.Empty;

        public int? PayrollNumber { get; set; }

        public int? IdPartNumber { get; set; }

        public int RequiredQuantity { get; set; }

        public int ScannedQuantity { get; set; } = 0;

        public string? Status { get; set; }

        public PartNumber? PartNumber { get; set; }

        public ICollection<ScanDetail> ScanDetails { get; set; } = new List<ScanDetail>();
    }
}