using System.Net.Http.Json;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HardikDhuri.TaskManager.Worker;

public class Worker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Initializes RabbitMQ Connection and Channel
    /// </summary>
    private async Task InitializeRabbitMQAsync()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("RabbitMQ Host is not configured."),
                UserName = _configuration["RabbitMq:Username"] ?? throw new InvalidOperationException("RabbitMQ Username is not configured."),
                Password = _configuration["RabbitMq:Password"] ?? throw new InvalidOperationException("RabbitMQ Password is not configured.")
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            var queueName = _configuration["RabbitMq:QueueName"] ?? throw new InvalidOperationException("RabbitMQ QueueName is not configured.");
            await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            Console.WriteLine($" [*] Connected to RabbitMQ and listening on queue: {queueName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [!] Error initializing RabbitMQ: {ex.Message}");
            throw; // Re-throw the exception for further handling
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection is null)
            await InitializeRabbitMQAsync();

        // Ensure RabbitMQ is initialized before starting the consumer
        if (_channel == null || _connection == null)
        {
            Console.WriteLine(" [!] RabbitMQ connection or channel is not initialized.");
            return;
        }

        var queueName = _configuration["RabbitMq:QueueName"];
        if (string.IsNullOrEmpty(queueName))
        {
            Console.WriteLine(" [!] RabbitMQ queue name is not configured.");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var taskId = Encoding.UTF8.GetString(body);

            Console.WriteLine($" [x] Received Task ID: {taskId}");

            try
            {
                // Fetch joke from Joke API
                var jokeResult = await FetchJokeAsync();

                // Send joke result back to the API
                await SendResultToApi(taskId, jokeResult);

                // Acknowledge the message
                await _channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [!] Error processing message: {ex.Message}");
            }
        };

        await _channel.BasicConsumeAsync(queue: queueName!, autoAck: false, consumer: consumer);
        Console.WriteLine(" [*] Waiting for messages...");
        await Task.CompletedTask;
    }

    private async Task<string> FetchJokeAsync()
    {
        try
        {
            var jokeApiUrl = "https://official-joke-api.appspot.com/random_joke";
            var response = await _httpClient.GetFromJsonAsync<JokeResult>(jokeApiUrl);

            return response != null
                ? $"{response.Setup} - {response.Punchline}"
                : "No joke found.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [!] Failed to fetch joke: {ex.Message}");
            return "Error fetching joke.";
        }
    }

    private async Task SendResultToApi(string taskId, string result)
    {
        try
        {
            var apiUrl = $"{_configuration["Api:BaseUrl"]}/api/task/{taskId}";
            var content = new StringContent($"{{ \"result\": \"{result}\" }}", Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($" [✓] Task {taskId} updated successfully.");
            }
            else
            {
                Console.WriteLine($" [!] Failed to update task {taskId}: {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [!] Error sending result to API: {ex.Message}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Safely dispose of RabbitMQ resources
            if (_channel != null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }

            base.Dispose();
            Console.WriteLine(" [*] RabbitMQ resources disposed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [!] Error during disposal: {ex.Message}");
        }
    }

    // Add the JokeResult class for JSON deserialization
    public class JokeResult
    {
        public string Setup { get; set; } = string.Empty;
        public string Punchline { get; set; } = string.Empty;
    }
}
