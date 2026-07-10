namespace Domain.Entities
{
    public class Client
    {
        public int Id { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PartNumber> PartNumbers { get; set; } = new List<PartNumber>();
    }
}