namespace StudentApp.Application.DTOs
{
    public class StudentDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomorIndukMahasiswa { get; set; } = string.Empty;
        public string NamaLengkap { get; set; } = string.Empty;
        public int Usia { get; set; }
    }
}
