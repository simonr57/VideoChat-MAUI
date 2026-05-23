using ChatApp.Utilities;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.External
{
    public class ConnectionHubService : IDisposable
    {
        private bool _disposed = false;
        private string _ownusername = string.Empty;
        private HubConnection? _hubConnection;

        public ConnectionHubService()
        {
            var localUser = LocalDbExtensions.RetrievePreferences("_user");
            if (!string.IsNullOrEmpty(localUser))
            {
                _ownusername = localUser;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(
                    Configuration.BackendURL + "ConnectionHub",
                    options =>
                    {
                        options.Headers["Authorization"] =
                            $"Bearer {LocalDbExtensions.RetrieveSecureString("_jwttoken")}";
                    }
                )
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += HubConnection_Closed;
            _hubConnection.Reconnected += HubConnection_Reconnected;

            Task.Run(async () => await StartConnection());
        }

        private async Task StartConnection()
        {
            try
            {
                if (
                    Connectivity.Current.NetworkAccess == NetworkAccess.Internet
                    && _hubConnection?.State == HubConnectionState.Disconnected
                )
                {
                    await _hubConnection.StartAsync();
                    await _hubConnection.InvokeAsync("AddToGroup", _ownusername);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
            }
        }

        private async Task HubConnection_Closed(Exception? ex)
        {
            await StartConnection();
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            await _hubConnection.InvokeAsync("AddToGroup", _ownusername);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_hubConnection != null)
                    {
                        _hubConnection.Closed -= HubConnection_Closed;
                        _hubConnection.Reconnected -= HubConnection_Reconnected;
                        _hubConnection.DisposeAsync().GetAwaiter().GetResult();
                    }
                }
                _disposed = true;
            }
        }
    }
}
