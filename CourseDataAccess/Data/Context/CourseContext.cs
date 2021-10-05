using CourseDataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseDataAccess.Data.Context
{
    public class CourseContext : DbContext
    {
        public CourseContext(DbContextOptions<CourseContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
    }
}
