using ChatApp.Models;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;

namespace ChatApp.Workers
{
    public class MyBackgroundService : IHostedService
    {
        private CancellationTokenSource? _cts;
        private Task? _executingTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = RunAsync(_cts.Token);

            return Task.CompletedTask;
        }

        private async Task RunAsync(CancellationToken token)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(4));

            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    WeakReferenceMessenger.Default.Send(new OnUpdateDatabase());
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_cts != null)
            {
                _cts.Cancel();

                if (_executingTask != null)
                {
                    await _executingTask;
                }

                _cts.Dispose();
            }
        }
    }
}
