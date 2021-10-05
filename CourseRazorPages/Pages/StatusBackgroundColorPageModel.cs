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
                nameof(Status.Approved) => "bg-primary",
                nameof(Status.Pending) => "bg-warning",
                nameof(Status.Error) => "bg-danger",
                _ => ""
            };
        }
    }
}
