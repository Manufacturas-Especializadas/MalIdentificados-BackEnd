namespace Domain.Entities
{
    public class ScanDetail
    {
        public int Id { get; set; }

        public int IdValidation { get; set; }

        public string ScannedPartCode { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;

        public ContainerValidation? ContainerValidation { get; set; }
    }
}