using System.Threading.Tasks;

namespace BattleshipClient.Mediator
{
    public sealed class NetworkShotSender : IShotSender
    {
        private readonly INetworkClient _net;

        public NetworkShotSender(INetworkClient net)
        {
            _net = net;
        }

        public Task SendAsync(object message) => _net.SendAsync(message);
    }
}
