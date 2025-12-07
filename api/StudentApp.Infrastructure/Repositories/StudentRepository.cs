using Microsoft.EntityFrameworkCore;
using StudentApp.Application.Interfaces;
using StudentApp.Core.Entities;
using StudentApp.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentApp.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly StudentContext _context;

        public StudentRepository(StudentContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            return await _context.Students.ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(string id)
        {
            return await _context.Students.FindAsync(id);
        }

        public async Task<string?> GetLastNomorIndukAsync(string prefix)
        {
            return await _context.Students
                .Where(s => s.NomorIndukMahasiswa.StartsWith(prefix))
                .OrderByDescending(s => s.NomorIndukMahasiswa)
                .Select(s => s.NomorIndukMahasiswa)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(Student student)
        {
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
        }
    }
}
