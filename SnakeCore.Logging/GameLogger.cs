using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace SnakeCore.Logging
{
    public class LokiLoggerProvider : ILoggerProvider
    {
        private readonly string _lokiEndpoint;
        private readonly HttpClient _httpClient = new HttpClient();

        public LokiLoggerProvider(string lokiEndpoint)
        {
            _lokiEndpoint = lokiEndpoint ?? throw new ArgumentNullException(nameof(lokiEndpoint));
        }

        public ILogger CreateLogger(string categoryName) => new GameLogger(categoryName, _lokiEndpoint, _httpClient);

        public void Dispose() => _httpClient.Dispose();
    }
    public class GameLogger : ILogger
    {
        private readonly string _category;
        private readonly string _endpoint;
        private readonly HttpClient _client;

        public GameLogger(string category, string endpoint, HttpClient client)
        {
            _category = category;
            _endpoint = endpoint;
            _client = client;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var logLine = new
            {
                streams = new[]
                {
                    new
                    {
                        labels = $"{{job=\"{_category}\", level=\"{logLevel}\"}}",
                        entries = new[]
                        {
                            new { ts = DateTime.UtcNow.ToString("o"), line = message }
                        }
                    }
                }
            };

            _ = _client.PostAsJsonAsync(_endpoint, logLine)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Console.WriteLine("Failed to push log: " + task.Exception);
            });
        }
    }
}
