using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BattleshipServer
{
    /// <summary>
    /// WebSocket "tūta", kuri nieko nesiunčia ir nieko negauna.
    /// Leidžia turėti PlayerConnection botui, kad Game galėtų kviesti SendAsync be null tikrinimų.
    /// </summary>
    public sealed class NoopWebSocket : WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;

        public override WebSocketCloseStatus? CloseStatus => WebSocketCloseStatus.NormalClosure;
        public override string CloseStatusDescription => "Noop";
        public override WebSocketState State => _state;
        public override string SubProtocol => string.Empty;

        public override void Abort() => _state = WebSocketState.Aborted;
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        { _state = WebSocketState.Closed; return Task.CompletedTask; }
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        { _state = WebSocketState.CloseSent; return Task.CompletedTask; }

        public override void Dispose() { _state = WebSocketState.Closed; }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            // Jokio realaus gavimo – grąžinam tuščią rezultatą (uždaryta)
            _state = WebSocketState.CloseReceived;
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            // Tyla :) — nieko nesiunčiam
            return Task.CompletedTask;
        }
    }
}
