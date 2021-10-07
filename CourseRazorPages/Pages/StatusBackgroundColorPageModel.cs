using CourseDataAccess.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CourseRazorPages.Pages
{
    public class StatusBackgroundColorPageModel : PageModel
    {
        public string GetStatusBackgroundColor(string status)
        {
            return status switch
            {
                nameof(Status.Approved) => "bg-primary text-white",
                nameof(Status.Pending) => "bg-warning text-white",
                nameof(Status.Processing) => "bg-secondary text-white",
                nameof(Status.Error) => "bg-danger text-white",
                _ => ""
            };
        }
    }
}
