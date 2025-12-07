using Microsoft.EntityFrameworkCore;
using StudentApp.Core.Entities;

namespace StudentApp.Infrastructure.Data
{
    public class StudentContext : DbContext
    {
        public StudentContext(DbContextOptions<StudentContext> options) : base(options) { }
        public DbSet<Student> Students { get; set; }
    }
}
