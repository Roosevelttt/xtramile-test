using Microsoft.AspNetCore.Mvc;
using StudentApp.Api.DTOs;
using StudentApp.Application.DTOs;
using StudentApp.Application.Interfaces;
using StudentApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentRepository _repository;

        public StudentsController(IStudentRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var students = await _repository.GetAllAsync();
            var dtos = students.Select(s => new StudentDto
            {
                Id = s.Id,
                NomorIndukMahasiswa = s.NomorIndukMahasiswa,
                NamaLengkap = $"{s.FirstName} {s.LastName}".Trim(),
                Usia = CalculateAge(s.DateOfBirth)
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null) return NotFound();
            return Ok(student);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStudentRequest request)
        {
            // 1. Construct Prefix: Faculty + Jenjang + Prodi + Angkatan
            var prefix = $"{request.FacultyCode}{request.JenjangCode}{request.ProdiCode}{request.Angkatan}";
            
            // 2. Get Last ID to determine sequence
            var lastId = await _repository.GetLastNomorIndukAsync(prefix);
            
            int sequence = 1;
            if (!string.IsNullOrEmpty(lastId) && lastId.Length >= 9)
            {
                // Extract last 4 digits
                if (int.TryParse(lastId.Substring(lastId.Length - 4), out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            // 3. Generate New ID
            var newNim = $"{prefix}{sequence:D4}";

            var student = new Student
            {
                Id = Guid.NewGuid().ToString(),
                NomorIndukMahasiswa = newNim,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth
            };
            
            await _repository.AddAsync(student);
            return CreatedAtAction(nameof(GetAll), new { id = student.Id }, student);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, UpdateStudentRequest request)
        {
            var existingStudent = await _repository.GetByIdAsync(id);
            if (existingStudent == null) return NotFound();

            existingStudent.FirstName = request.FirstName;
            existingStudent.LastName = request.LastName;
            existingStudent.DateOfBirth = request.DateOfBirth;

            await _repository.UpdateAsync(existingStudent);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var students = await _repository.GetAllAsync();
            var builder = new StringBuilder();
            builder.AppendLine("NomorIndukMahasiswa,FirstName,LastName,DateOfBirth");

            foreach (var student in students)
            {
                var cells = new[]
                {
                    EscapeForCsv(student.NomorIndukMahasiswa),
                    EscapeForCsv(student.FirstName),
                    EscapeForCsv(student.LastName),
                    student.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                };
                builder.AppendLine(string.Join(',', cells));
            }

            var fileName = $"students-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", fileName);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromForm] Microsoft.AspNetCore.Http.IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "A CSV file is required." });
            }

            var existingNims = (await _repository.GetAllAsync())
                .Select(student => student.NomorIndukMahasiswa)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var imported = new List<Student>();
            var duplicateLines = new List<int>();
            var invalidLines = new List<int>();

            using var reader = new StreamReader(file.OpenReadStream());
            var lineNumber = 0;
            while (true)
            {
                var rawLine = await reader.ReadLineAsync();
                if (rawLine is null)
                {
                    break;
                }

                lineNumber++;

                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                if (lineNumber == 1 && rawLine.Contains("NomorIndukMahasiswa", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var columns = SplitCsvLine(rawLine).ToArray();
                if (columns.Length < 4)
                {
                    invalidLines.Add(lineNumber);
                    continue;
                }

                var nim = columns[0].Trim();
                var firstName = columns[1].Trim();
                var lastName = columns[2].Trim();
                var dobValue = columns[3].Trim();

                if (string.IsNullOrWhiteSpace(nim) || string.IsNullOrWhiteSpace(firstName)
                    || !DateTime.TryParse(dobValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dob))
                {
                    invalidLines.Add(lineNumber);
                    continue;
                }

                if (existingNims.Contains(nim))
                {
                    duplicateLines.Add(lineNumber);
                    continue;
                }

                imported.Add(new Student
                {
                    Id = Guid.NewGuid().ToString(),
                    NomorIndukMahasiswa = nim,
                    FirstName = firstName,
                    LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName,
                    DateOfBirth = dob
                });

                existingNims.Add(nim);
            }

            if (imported.Count > 0)
            {
                await _repository.AddManyAsync(imported);
            }

            return Ok(new
            {
                Imported = imported.Count,
                Duplicates = duplicateLines.Count,
                Invalid = invalidLines.Count
            });
        }

        private int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }

        private static string EscapeForCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sanitized = value.Replace("\"", "\"\"");
            if (sanitized.IndexOfAny(new[] { ',', '\"', '\n', '\r' }) >= 0)
            {
                return $"\"{sanitized}\"";
            }

            return sanitized;
        }

        private static IEnumerable<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];
                if (character == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        current.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (character == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(character);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }
    }
}
