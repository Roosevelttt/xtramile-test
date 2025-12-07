using Microsoft.AspNetCore.Mvc;
using StudentApp.Api.DTOs;
using StudentApp.Application.DTOs;
using StudentApp.Application.Interfaces;
using StudentApp.Core.Entities;
using System;
using System.Linq;
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

        private int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
