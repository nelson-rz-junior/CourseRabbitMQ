using RabbitMQ.Client;
using System;
using System.Collections.Generic;

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

        public static int GetRetryCount(IBasicProperties messageProperties, string countHeader)
        {
            int count = 0;

            IDictionary<string, object> headers = messageProperties.Headers;
            if (headers != null)
            {
                if (headers.ContainsKey(countHeader))
                {
                    string countAsString = Convert.ToString(headers[countHeader]);
                    count = Convert.ToInt32(countAsString);
                }
            }

            return count;
        }

        public static IDictionary<string, object> CopyHeaders(IBasicProperties originalProperties)
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();

            IDictionary<string, object> headers = originalProperties.Headers;
            if (headers != null)
            {
                foreach (KeyValuePair<string, object> kvp in headers)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }

            return dict;
        }
    }
}
