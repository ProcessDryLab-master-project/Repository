using System.Threading.Channels;
using System;
using System.Text.Json;

namespace Repository.App.API
{
    class BackgroundQueue : BackgroundService
    {
        private readonly Channel<ReadOnlyMemory<byte>> _queue;
        private readonly ILogger<BackgroundQueue> _logger;

        public BackgroundQueue(Channel<ReadOnlyMemory<byte>> queue,
                                   ILogger<BackgroundQueue> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var dataStream in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    //bool validRequest = body.TryParseJson(out List<string> filters);
                    //var filter = JsonConvert.DeserializeObject<List<string>>(dataStream.Span!);
                    var person = JsonSerializer.Deserialize<List<string>> (dataStream.Span)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }
    }
}
