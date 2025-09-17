using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace BattleshipServer
{
    public class Server
    {
        private HttpListener? _listener;
        private GameManager _manager;

        public Server()
        {
            _manager = new GameManager();
        }

        public async Task StartAsync(int port = 5000)
        {
            string prefix = $"http://localhost:{port}/ws/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            Console.WriteLine($"Server listening on {prefix}");

            while (true)
            {
                var ctx = await _listener.GetContextAsync();
                if (ctx.Request.IsWebSocketRequest)
                {
                    var wsContext = await ctx.AcceptWebSocketAsync(null);
                    var webSocket = wsContext.WebSocket;
                    Console.WriteLine("New websocket connection accepted.");
                    var player = new PlayerConnection(webSocket, _manager);
                    _ = player.ProcessAsync(); // run without awaiting
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                }
            }
        }
    }
}
