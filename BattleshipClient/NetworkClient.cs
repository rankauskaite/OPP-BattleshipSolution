using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BattleshipClient.Models;

namespace BattleshipClient
{
    public class NetworkClient : INetworkClient   
    {
        private ClientWebSocket _ws;
        public event Action<MessageDto> OnMessageReceived;

        public async Task ConnectAsync(string wsUri)
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(wsUri), CancellationToken.None);
            _ = ReceiveLoop();
        }

        public async Task SendAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            while (_ws.State == WebSocketState.Open)
            {
                var res = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (res.MessageType == WebSocketMessageType.Close) break;
                var msg = Encoding.UTF8.GetString(buffer, 0, res.Count);
                try
                {
                    var dto = JsonSerializer.Deserialize<MessageDto>(msg, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    OnMessageReceived?.Invoke(dto);
                }
                catch { /* ignore parse errors */ }
            }
        }
    }
}
