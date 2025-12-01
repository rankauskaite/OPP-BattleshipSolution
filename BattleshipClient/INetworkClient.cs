using System;
using System.Threading.Tasks;
using BattleshipClient.Models;

namespace BattleshipClient
{
    public interface INetworkClient
    {
        event Action<MessageDto> OnMessageReceived;

        Task ConnectAsync(string wsUri);
        Task SendAsync(object payload);
    }
}
