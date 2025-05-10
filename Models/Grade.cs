using System.Text.Json.Serialization;

namespace Financial_management_backend.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public ICollection<Student> Students { get; set; }
    }
}
