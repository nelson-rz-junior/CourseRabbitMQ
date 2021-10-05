using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CourseDataAccess.Models;
using CourseDataAccess.Data.Interfaces;

namespace CourseRazorPages.Pages.Courses
{
    public class DetailsModel : StatusBackgroundColorPageModel
    {
        private readonly ICourseRepository _courseRepository;

        public DetailsModel(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public Course Course { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Course = await _courseRepository.GetFirstOrDefault(filter: m => m.Id == id);

            if (Course == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
