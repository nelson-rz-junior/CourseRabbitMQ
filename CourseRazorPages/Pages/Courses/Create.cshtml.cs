using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CourseDataAccess.Models;
using System;
using RabbitMQ.Client;
using System.Text;
using CourseDataAccess.Data.Interfaces;

namespace CourseRazorPages.Pages.Courses
{
    public class CreateModel : PageModel
    {
        private const string ORDER_QUEUE = "order.queue";
        private const string ORDER_EXCHANGE = "order.exchange";

        private readonly ICourseRepository _courseRepository;

        public CreateModel(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Course Course { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            var emptyCourse = new Course();

            if (await TryUpdateModelAsync(emptyCourse, "course", c => c.CustomerFullName, c => c.ProductName, c => c.Amount))
            {
                emptyCourse.Queued = true;
                emptyCourse.Status = Status.Pending.ToString();
                emptyCourse.CreatedAt = DateTime.Now;

                await _courseRepository.Save(emptyCourse);

                PublishQueue(emptyCourse);

                return RedirectToPage("./Index");
            }

            return Page();
        }

        private void PublishQueue(Course course)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(ORDER_EXCHANGE, ExchangeType.Direct);
            channel.QueueDeclare(ORDER_QUEUE, true, false, false, null);
            channel.QueueBind(ORDER_QUEUE, ORDER_EXCHANGE, "", null);

            string message = System.Text.Json.JsonSerializer.Serialize(course);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(ORDER_EXCHANGE, "", null, body);
        }
    }
}
