using System;

namespace StudentApp.Api.DTOs
{
    public class CreateStudentRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string FacultyCode { get; set; } = string.Empty;
        public string JenjangCode { get; set; } = string.Empty;
        public string ProdiCode { get; set; } = string.Empty;
        public string Angkatan { get; set; } = string.Empty;
    }
}
