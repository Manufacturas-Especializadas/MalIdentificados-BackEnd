namespace Domain.Entities
{
    public class PartNumber
    {
        public int Id { get; set; }

        public string PartNumbersCode { get; set; } = string.Empty;

        public int IdClient { get; set; }

        public int IdLine { get; set; }

        public int? DefaultStandardPack { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Client? Client { get; set; }

        public Lines? Line { get; set; }

        public ICollection<ContainerValidation> ContainerValidations { get; set; } = new List<ContainerValidation>();
    }
}