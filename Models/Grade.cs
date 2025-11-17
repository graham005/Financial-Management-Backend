using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Financial_management_backend.Models
{
    public class Grade
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        // Add these properties for ordering and promotion
        [Required]
        public int Level { get; set; } // 1=PP1, 2=PP2, 3=Grade1, ..., 8=Grade6
        
        public bool IsGraduationGrade { get; set; } = false; // Mark Grade 6 as graduation
        
        [StringLength(20)]
        public string? Category { get; set; } // "Pre-Primary", "Primary", etc.

        [JsonIgnore]
        public ICollection<Student> Students { get; set; }
    }
}
