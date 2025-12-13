using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BattleshipServer.Models;

namespace BattleshipServer
{
    public class PlayerConnection
    {
        public Guid Id { get; } = Guid.NewGuid();
        public WebSocket Socket { get; }
        public string Name { get; set; } = string.Empty;

        private readonly GameManager _manager;

        public PlayerConnection(WebSocket socket, GameManager manager)
        {
            Socket = socket;
            _manager = manager;
        }

        public async Task ProcessAsync()
        {
            var buffer = new byte[8192];
            try
            {
                while (Socket.State == WebSocketState.Open)
                {
                    using var ms = new System.IO.MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    var msg = Encoding.UTF8.GetString(ms.ToArray());
                    var dto = JsonSerializer.Deserialize<MessageDto>(msg, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dto != null)
                    {
                        await _manager.HandleMessageAsync(this, dto);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerConnection] Error: {ex.Message}");
            }
        }


        public async Task SendAsync(MessageDto dto)
        {
            try
            {
                var json = JsonSerializer.Serialize(dto);
                var bytes = Encoding.UTF8.GetBytes(json);
                await Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendAsync] {ex.Message}");
            }
        }
    }
}
