namespace Domain.Entities
{
    public class Line
    {
        public int Id { get; set; }

        public string LineName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PartNumber> PartNumbers { get; set; } = new List<PartNumber>()
    }
}