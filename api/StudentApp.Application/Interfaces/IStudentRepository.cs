using StudentApp.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentApp.Application.Interfaces
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(string id);
        Task<string?> GetLastNomorIndukAsync(string prefix);
        Task AddAsync(Student student);
        Task AddManyAsync(IEnumerable<Student> students);
        Task UpdateAsync(Student student);
        Task DeleteAsync(string id);
    }
}
