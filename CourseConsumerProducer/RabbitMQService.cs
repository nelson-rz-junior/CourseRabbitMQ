using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseConsumerProducer
{
    public static class RabbitMQService
    {
        public static IConnection GetRabbitMqConnection()
        {   
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            
            return factory.CreateConnection();
        }

        public static void SetupInitialQueue(string exchange, string queue, IModel channel, Dictionary<string, object> arguments)
        {
            channel.ExchangeDeclare(exchange, ExchangeType.Direct);
            channel.QueueDeclare(queue, true, false, false, arguments);
            channel.QueueBind(queue, exchange, "", null);
        }

        public static int GetRetryCount(string customHeaderName, IBasicProperties messageProperties)
        {
            int count = 0;

            IDictionary<string, object> headers = messageProperties.Headers;
            if (headers != null)
            {
                if (headers.ContainsKey(customHeaderName))
                {
                    string countAsString = Convert.ToString(headers[customHeaderName]);
                    count = Convert.ToInt32(countAsString);
                }
            }

            return count;
        }

        public static IBasicProperties AddCustomHeader(string customHeaderName, ref int retryCount, IModel channel, IBasicProperties originalProperties)
        {
            IDictionary<string, object> headersCopy = new Dictionary<string, object>();

            IDictionary<string, object> headers = originalProperties.Headers;
            if (headers != null)
            {
                foreach (KeyValuePair<string, object> kvp in headers)
                {
                    headersCopy[kvp.Key] = kvp.Value;
                }
            }

            IBasicProperties properties = channel.CreateBasicProperties();
            properties.Headers = headersCopy;
            properties.Headers[customHeaderName] = ++retryCount;

            return properties;
        }

        public static void PublishQueue(string exchange, string queue, string message, IModel channel, IBasicProperties properties, Dictionary<string, object> arguments)
        {
            SetupInitialQueue(exchange, queue, channel, arguments);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange, "", properties, body);
        }
    }
}
