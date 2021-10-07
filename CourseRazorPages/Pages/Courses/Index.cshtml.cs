using System.Collections.Generic;
using System.Threading.Tasks;
using CourseDataAccess.Models;
using CourseDataAccess.Data.Interfaces;
using System.Linq;

namespace CourseRazorPages.Pages.Courses
{
    public class IndexModel : StatusBackgroundColorPageModel
    {
        private readonly ICourseRepository _courseRepository;

        public IndexModel(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public IList<Course> Courses { get; set; }

        public async Task OnGetAsync()
        {
            Courses = await _courseRepository.GetAll(orderBy: q => q.OrderByDescending(c => c.CreatedAt));
        }
    }
}
