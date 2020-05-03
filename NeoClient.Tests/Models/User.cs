namespace NeoClient.Tests.Models
{
    public class User : EntityBase
    {
        public User() : base(label: "User") { }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}
