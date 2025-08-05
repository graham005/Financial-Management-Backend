using System.Text.Json.Serialization;

namespace Financial_management_backend.Models
{
    public class Grade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        [JsonIgnore]
        public ICollection<Student> Students { get; set; }
    }
}
