using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using NegativeEddy.PresencePi.Graph;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.PresencePi.Display
{
    public class PresenceUpdaterService : IHostedService
    {
        private const int PRESENCE_POLLING_INTERVAL_MS = 5000;
        private int executionCount = 0;
        private readonly ILogger<PresenceUpdaterService> _logger;
        private readonly PresenceStore _presenceStore;
        private readonly WebOptions _webOptions;
        private Task _worker = null;

        public PresenceUpdaterService(ILogger<PresenceUpdaterService> logger, IOptions<WebOptions> webOptionValue, PresenceStore presenceStore)
        {
            _logger = logger;
            _presenceStore = presenceStore;
            _webOptions = webOptionValue.Value;
        }

        CancellationTokenSource cts;

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("starting");

            cts = new CancellationTokenSource();
            _worker = Task.Run(async () => await DoWork(cts.Token), cts.Token);

            _logger.LogInformation("started");
            return Task.CompletedTask;
        }


        private async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation("worker starting");

            while (!cancellationToken.IsCancellationRequested)
            {
                int count = 0;

                if (_presenceStore.TokenAcquisition is not null &&
                    _presenceStore.User is not null)   // don't execute until auth is set up
                {

                    try
                    {
                        count = Interlocked.Increment(ref executionCount);
                        GraphServiceClient graphClient = GetGraphServiceClient(new[] { Scopes.PresenceRead });
                        var newPresence = await graphClient.Me.Presence.Request().GetAsync();
                        _presenceStore.Presence = newPresence;

                        _logger.LogTrace(
                            "worker. Count: {Count} Presence: {Presence}", count, newPresence.Availability);
                    }
                    catch (Exception ex)
                    {
                        _presenceStore.Presence = _presenceStore.PresenceError;
                        _logger.LogError(ex, "failed to get status");
                        break;
                    }
                }

                try
                {
                    await Task.Delay(PRESENCE_POLLING_INTERVAL_MS, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // do nothing if cancelled. just exit
                }

            }

            _logger.LogInformation("worker stopped");
        }

        public async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("stopping");
            try
            {
                cts.Cancel();
                await _worker;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error while stopping");
            }
            _logger.LogInformation("stopped");
        }

        private GraphServiceClient GetGraphServiceClient(string[] scopes)
        {
            return GraphServiceClientFactory.GetAuthenticatedGraphClient(async () =>
            {
                try
                {
                    string result = await _presenceStore.TokenAcquisition?.GetAccessTokenForUserAsync(scopes, user: _presenceStore.User);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get user token");
                    throw;
                }
            }, _webOptions.GraphApiUrl);
        }
    }
}
