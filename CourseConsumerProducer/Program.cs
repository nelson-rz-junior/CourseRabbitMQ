using CourseDataAccess.Data.Interfaces;
using CourseDataAccess.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using System.Text.Json;
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

        static async Task UpdateCourseStatus(string message, IModel channel, BasicDeliverEventArgs args, ICourseRepository courseRepository)
        {
            try
            {
                var rnd = new Random();

                var course = JsonSerializer.Deserialize<Course>(message);
                if (course != null)
                {
                    var dbCourse = await courseRepository.Find(course.Id);
                    if (dbCourse != null)
                    {
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
                            throw new Exception("Fake error");
                        }

                        await courseRepository.Update(dbCourse);

                        RabbitMQService.PublishQueue(TRANSACTION_EXCHANGE, TRANSACTION_QUEUE, JsonSerializer.Serialize(dbCourse), channel, null, null);

                        WriteLine("Message APPROVED: {0} \n", message);
                    }
                }
            }
            catch (JsonException)
            {
                // Message must be rejected, cannot process
                RabbitMQService.PublishQueue(ERROR_EXCHANGE, ERROR_QUEUE, message, channel, null, null);

                WriteLine("Message {0} has malformed. It will be REJECTED. \n", message);
            }
            catch (SqlException sqlEx)
            {
                var course = JsonSerializer.Deserialize<Course>(message);
                if (course != null)
                {
                    int retryCount = RabbitMQService.GetRetryCount(CUSTOM_RETRY_HEADER_NAME, args.BasicProperties);
                    if (retryCount < MAX_NUMBER_OF_RETRIES)
                    {
                        // Message thrown back at queue for retry
                        var arguments = new Dictionary<string, object>
                        {
                            { "x-dead-letter-exchange", ORDER_EXCHANGE },
                            { "x-message-ttl", RETRY_DELAY }
                        };

                        IBasicProperties properties = RabbitMQService.AddCustomHeader(CUSTOM_RETRY_HEADER_NAME, ref retryCount, channel, args.BasicProperties);
                        RabbitMQService.PublishQueue(RETRY_EXCHANGE, RETRY_QUEUE, message, channel, properties, arguments);

                        WriteLine("Message {0} thrown back at queue for RETRY. New retry count: {1} \n", message, retryCount);
                    }
                    else
                    {
                        // Message must be rejected, cannot process
                        course.ErrorMessage = sqlEx.Message;
                        course.Retry = retryCount;
                        course.Processed = true;
                        course.Error = true;
                        course.Status = Status.Error.ToString();
                        course.UpdatedAt = DateTime.Now;

                        RabbitMQService.PublishQueue(ERROR_EXCHANGE, ERROR_QUEUE, JsonSerializer.Serialize(course), channel, null, null);

                        WriteLine("Message {0} has reached the max number of retries. It will be REJECTED. \n", message);
                    }
                }
                else
                {
                    RabbitMQService.PublishQueue(ERROR_EXCHANGE, ERROR_QUEUE, message, channel, null, null);
                }
            }
            catch (Exception ex)
            {
                var course = JsonSerializer.Deserialize<Course>(message);
                if (course != null)
                {
                    var dbCourse = await courseRepository.Find(course.Id);
                    if (dbCourse != null)
                    {
                        int retryCount = RabbitMQService.GetRetryCount(CUSTOM_RETRY_HEADER_NAME, args.BasicProperties);
                        if (retryCount < MAX_NUMBER_OF_RETRIES)
                        {
                            // Message thrown back at queue for retry
                            var arguments = new Dictionary<string, object>
                            {
                                { "x-dead-letter-exchange", ORDER_EXCHANGE },
                                { "x-message-ttl", RETRY_DELAY }
                            };

                            IBasicProperties properties = RabbitMQService.AddCustomHeader(CUSTOM_RETRY_HEADER_NAME, ref retryCount, channel, args.BasicProperties);

                            dbCourse.Status = Status.Processing.ToString();
                            dbCourse.Retry = retryCount;
                            dbCourse.UpdatedAt = DateTime.Now;

                            await courseRepository.Update(dbCourse);

                            RabbitMQService.PublishQueue(RETRY_EXCHANGE, RETRY_QUEUE, JsonSerializer.Serialize(dbCourse), channel, properties, arguments);

                            WriteLine("Message {0} thrown back at queue for RETRY. New retry count: {1} \n", message, retryCount);
                        }
                        else
                        {
                            // Messagte must be rejected, cannot process
                            dbCourse.ErrorMessage = ex.Message;
                            dbCourse.Retry = retryCount;
                            dbCourse.Processed = true;
                            dbCourse.Error = true;
                            dbCourse.Status = Status.Error.ToString();
                            dbCourse.UpdatedAt = DateTime.Now;

                            await courseRepository.Update(dbCourse);

                            RabbitMQService.PublishQueue(ERROR_EXCHANGE, ERROR_QUEUE, JsonSerializer.Serialize(dbCourse), channel, null, null);

                            WriteLine("Message {0} has reached the max number of retries. It will be REJECTED. \n", message);
                        }
                    }
                }
                else
                {
                    RabbitMQService.PublishQueue(ERROR_EXCHANGE, ERROR_QUEUE, message, channel, null, null);
                }
            }
        }
    }
}
