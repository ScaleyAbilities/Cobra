using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Cobra
{
    static class RabbitHelper
    {
        private static IConnection rabbitConnection;
        private static IModel rabbitChannel;

        private static string rabbitHost = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "localhost";
        public static string rabbitLogQueue = "logs";
        private static IBasicProperties rabbitProperties;

        static RabbitHelper()
        {
            // Ensure Rabbit Queue is set up
            var factory = new ConnectionFactory()
            {
                HostName = rabbitHost,
                UserName = "scaley",
                Password = "abilities",
            };

            // Try connecting to rabbit until it works
            var connected = false;
            while (!connected)
            {
                try
                {
                    rabbitConnection = factory.CreateConnection();
                    connected = true;
                }
                catch (BrokerUnreachableException)
                {
                    Console.Error.WriteLine("Unable to connect to Rabbit, retrying...");
                    Thread.Sleep(3000);
                }
            }

            rabbitChannel = rabbitConnection.CreateModel();

            rabbitChannel.QueueDeclare(
                queue: rabbitLogQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            rabbitProperties = rabbitChannel.CreateBasicProperties();
            rabbitProperties.Persistent = true;
        }

        public static void PushLogEntry(string logEntry)
        {
            rabbitChannel.BasicPublish(
                exchange: "",
                routingKey: rabbitLogQueue,
                basicProperties: rabbitProperties,
                body: Encoding.UTF8.GetBytes(logEntry)
            );
        }
    }
}
