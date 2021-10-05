using CourseDataAccess.Data.Interfaces;
using CourseDataAccess.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

namespace CourseConsumerProducer
{
    class Program
    {
        private const string CUSTOM_RETRY_HEADER_NAME = "number-of-retries";
        private const int MAX_NUMBER_OF_RETRIES = 3;

        private const string ORDER_QUEUE = "order.queue";
        private const string TRANSACTION_QUEUE = "transaction.queue";
        private const string RETRY_QUEUE = "retry.queue";
        private const string ERROR_QUEUE = "error.queue";

        private const string ORDER_EXCHANGE = "order.exchange";
        private const string TRANSACTION_EXCHANGE = "transaction.exchange";
        private const string RETRY_EXCHANGE = "retry.exchange";
        private const string ERROR_EXCHANGE = "error.exchange";

        private const int RETRY_DELAY = 60000;

        static void Main(string[] args)
        {
            using var connection = RabbitMQService.GetRabbitMqConnection();
            using var channel = connection.CreateModel();

            RabbitMQService.SetupInitialQueue(ORDER_EXCHANGE, ORDER_QUEUE, channel, null);

            var serviceProvider = RepositoryService.GetServiceProvider();
            var courseRepository = serviceProvider.GetService<ICourseRepository>();

            ConsumerQueue(channel, courseRepository);

            ReadKey();
        }

        static void ConsumerQueue(IModel channel, ICourseRepository courseRepository)
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, args) =>
            {
                var body = args.Body;
                var message = Encoding.UTF8.GetString(body.Span);

                WriteLine("Message from QUEUE: {0} \n", message);

                await UpdateCourseStatus(message, channel, args, courseRepository);
            };

            channel.BasicConsume(ORDER_QUEUE, true, consumer);
        }

        static async Task<Course> UpdateCourseStatus(string message, IModel channel, BasicDeliverEventArgs args, ICourseRepository courseRepository)
        {
            Guid id = Guid.Empty;

            Course result = null;

            try
            {
                var rnd = new Random();

                var course = System.Text.Json.JsonSerializer.Deserialize<Course>(message);
                if (course != null)
                {
                    id = course.Id;

                    var dbCourse = await courseRepository.Find(course.Id);
                    if (dbCourse != null)
                    {
                        dbCourse.Requeued = false;
                        dbCourse.Processed = true;
                        dbCourse.UpdatedAt = DateTime.Now;

                        if (rnd.Next(0, 2) == 1)
                        {
                            // No error
                            dbCourse.Status = Status.Approved.ToString();
                            dbCourse.TransactionId = Guid.NewGuid();
                        }
                        else
                        {
                            // Simulate error
                            dbCourse.Status = Status.Error.ToString();
                            dbCourse.TransactionId = Guid.Empty;
                        }

                        await courseRepository.Update(dbCourse);

                        // Throw exception
                        if (dbCourse.Status == Status.Error.ToString())
                        {
                            throw new Exception("Fake error");
                        }

                        WriteLine("Message APPROVED: {0} \n", message);

                        PublishTransaction(dbCourse, channel);

                        result = dbCourse;
                    }
                }
            }
            catch (Exception ex)
            {
                int retryCount = RabbitMQService.GetRetryCount(args.BasicProperties, CUSTOM_RETRY_HEADER_NAME);
                if (retryCount < MAX_NUMBER_OF_RETRIES)
                {
                    // Accept message, but create copy and throw back
                    IDictionary<string, object> headersCopy = RabbitMQService.CopyHeaders(args.BasicProperties);

                    IBasicProperties propertiesForCopy = channel.CreateBasicProperties();
                    propertiesForCopy.Headers = headersCopy;
                    propertiesForCopy.Headers[CUSTOM_RETRY_HEADER_NAME] = ++retryCount;

                    PublishRetryQueue(message, propertiesForCopy, channel);

                    WriteLine("Message {0} thrown back at queue for RETRY. New retry count: {1} \n", message, retryCount);
                }
                else
                {
                    // Must be rejected, cannot process
                    WriteLine("Message {0} has reached the max number of retries. It will be REJECTED. \n", message);

                    var error = new { Id = id, ErrorMessage = ex.Message, ErrorDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") };

                    PublishErrorQueue(System.Text.Json.JsonSerializer.Serialize(error), channel);
                }
            }

            return result;
        }

        static void PublishTransaction(Course course, IModel channel)
        {
            RabbitMQService.SetupInitialQueue(TRANSACTION_EXCHANGE, TRANSACTION_QUEUE, channel, null);

            string message = System.Text.Json.JsonSerializer.Serialize(course);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(TRANSACTION_EXCHANGE, "", null, body);
        }

        static void PublishRetryQueue(string message, IBasicProperties basicProperties, IModel channel)
        {
            var queueArgs = new Dictionary<string, object> 
            {
                { "x-dead-letter-exchange", ORDER_EXCHANGE },
                { "x-message-ttl", RETRY_DELAY }
            };

            RabbitMQService.SetupInitialQueue(RETRY_EXCHANGE, RETRY_QUEUE, channel, queueArgs);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(RETRY_EXCHANGE, "", basicProperties, body);
        }

        static void PublishErrorQueue(string message, IModel channel)
        {
            RabbitMQService.SetupInitialQueue(ERROR_EXCHANGE, ERROR_QUEUE, channel, null);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(ERROR_EXCHANGE, "", null, body);
        }
    }
}
