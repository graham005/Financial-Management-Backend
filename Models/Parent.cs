namespace Financial_management_backend.Models
{
    public class Parent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
    }
}
