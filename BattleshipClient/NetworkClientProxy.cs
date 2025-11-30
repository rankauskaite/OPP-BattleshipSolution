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
        private readonly Func<INetworkClient> _factory;  // kaip kursim realų klientą
        private INetworkClient? _inner;                  // RealSubject (lazy)
        private readonly string _allowedHost;

        // event'ą turim atskirai – klientas subscribinasi į Proxy
        public event Action<MessageDto> OnMessageReceived;

        public NetworkClientProxy(Func<INetworkClient> factory, string allowedHost)
        {
            _factory = factory;
            _allowedHost = allowedHost;
        }

        // Virtual proxy – realų objektą kuriam tik tada, kai JAU reikia
        private void EnsureClient()
        {
            if (_inner != null) return;

            _inner = _factory();

            // forwardinam event'us ir pridedam logging (Smart proxy)
            _inner.OnMessageReceived += dto =>
            {
                File.AppendAllText("network_log.txt",
                    $"{DateTime.Now:HH:mm:ss} <- {dto.Type}{Environment.NewLine}");

                OnMessageReceived?.Invoke(dto);
            };
        }

        public async Task ConnectAsync(string wsUri)
        {
            // Protection proxy – leidžiam jungtis tik prie tam tikro host
            var uri = new Uri(wsUri);
            if (!string.Equals(uri.Host, _allowedHost, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Connection host is not allowed.");

            EnsureClient(); // Virtual proxy – sukuriam realų klientą tik dabar
            await _inner!.ConnectAsync(wsUri);
        }

        public async Task SendAsync(object payload)
        {
            EnsureClient();

            // Smart proxy – papildomas funkcionalumas, pvz. logging
            File.AppendAllText("network_log.txt",
                $"{DateTime.Now:HH:mm:ss} -> {JsonSerializer.Serialize(payload)}{Environment.NewLine}");

            await _inner!.SendAsync(payload);
        }
    }
}
