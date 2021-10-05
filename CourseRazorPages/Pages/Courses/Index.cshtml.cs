using System.Collections.Generic;
using System.Threading.Tasks;
using CourseDataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System;
using CourseDataAccess.Data.Interfaces;
using System.Linq;

namespace CourseRazorPages.Pages.Courses
{
    public class IndexModel : StatusBackgroundColorPageModel
    {
        private const string ORDER_QUEUE = "order.queue";
        private const string ORDER_EXCHANGE = "order.exchange";

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

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            string boxMessage = string.Empty;
            string boxStyle = string.Empty;

            var course = await _courseRepository.GetFirstOrDefault(noTracking: false, filter: c => c.Id == id);
            if (course != null)
            {
                course.Requeued = true;
                course.Processed = false;
                course.Status = Status.Pending.ToString();
                course.TransactionId = Guid.Empty;
                course.UpdatedAt = DateTime.Now;

                await _courseRepository.Update(course);

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

                boxMessage = $"Order \"{course.Id}\" REQUEUED successfully.";
                boxStyle = "alert-primary";
            }
            else
            {
                boxMessage = $"Order \"{id}\" NOT FOUND or ALREADY REQUEUED.";
                boxStyle = "alert-danger";
            }

            TempData["BoxMessage"] = boxMessage;
            TempData["BoxStyle"] = boxStyle;

            return RedirectToPage("./Index");
        }
    }
}
