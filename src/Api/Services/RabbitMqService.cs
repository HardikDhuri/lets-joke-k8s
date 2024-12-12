using System;
using System.Text;
using RabbitMQ.Client;

namespace HardikDhuri.TaskManager.Api.Services;

public class RabbitMqService
    {
        private readonly IConfiguration _configuration;

        public RabbitMqService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async void SendMessage(string message)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMQ is not configured."),
                UserName = _configuration["RabbitMq:Username"]!,
                Password = _configuration["RabbitMq:Password"]!
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var queueName = _configuration["RabbitMq:QueueName"] ?? throw new InvalidOperationException("Rabbit Mq is not configured.");
            await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);

            Console.WriteLine($" [x] Sent: {message}");
        }
    }
