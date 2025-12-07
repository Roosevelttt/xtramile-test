using System;

namespace StudentApp.Core.Entities
{
    public class Student
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string NomorIndukMahasiswa { get; set; } = string.Empty;
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
}
