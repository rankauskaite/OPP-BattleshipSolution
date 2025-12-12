using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipClient.Models;

namespace BattleshipClient
{
    /// <summary>
    /// NetworkClientProxy: Protection + Smart + Virtual proxy viename.
    /// </summary>
    public class NetworkClientProxy : INetworkClient
    {
        private readonly Func<INetworkClient> _factory;  
        private INetworkClient? _inner;                
        private readonly string _allowedHost;

        public event Action<MessageDto> OnMessageReceived;

        public NetworkClientProxy(Func<INetworkClient> factory, string allowedHost)
        {
            _factory = factory;
            _allowedHost = allowedHost;
        }

        private void EnsureClient()
        {
            if (_inner != null) return;

            _inner = _factory();

            _inner.OnMessageReceived += dto =>
            {
                File.AppendAllText("network_log.txt",
                    $"{DateTime.Now:HH:mm:ss} <- {dto.Type}{Environment.NewLine}");

                OnMessageReceived?.Invoke(dto);
            };
        }

        public async Task ConnectAsync(string wsUri)
        {
            var uri = new Uri(wsUri);
            if (!string.Equals(uri.Host, _allowedHost, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Connection host is not allowed.");

            EnsureClient(); 
            await _inner!.ConnectAsync(wsUri);
        }

        public async Task SendAsync(object payload)
        {
            EnsureClient();

            File.AppendAllText("network_log.txt",
                $"{DateTime.Now:HH:mm:ss} -> {JsonSerializer.Serialize(payload)}{Environment.NewLine}");

            await _inner!.SendAsync(payload);
        }
    }
}
