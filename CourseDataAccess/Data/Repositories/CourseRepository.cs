using CourseDataAccess.Data.Context;
using CourseDataAccess.Data.Interfaces;
using CourseDataAccess.Models;

namespace CourseDataAccess.Data.Repositories
{
    public class CourseRepository : Repository<Course>, ICourseRepository
    {
        private readonly CourseContext _context;

        public CourseRepository(CourseContext context) : base(context)
        {
            _context = context;
        }
    }
}
