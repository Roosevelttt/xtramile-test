using System;
using System.ComponentModel.DataAnnotations;

namespace StudentApp.Api.DTOs
{
    public class CreateStudentRequest
    {
        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot be longer than 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Last Name cannot be longer than 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Faculty is required")]
        public string FacultyCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jenjang is required")]
        public string JenjangCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prodi is required")]
        public string ProdiCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Angkatan is required")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Angkatan must be 2 digits (e.g., 23)")]
        public string Angkatan { get; set; } = string.Empty;
    }
}
