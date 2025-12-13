using System.Threading.Tasks;
using BattleshipServer.Models;

namespace BattleshipServer.MessageHandling
{
    /// <summary>
    /// Chain of Responsibility: Handler interface.
    /// </summary>
    public interface IMessageHandler
    {
        IMessageHandler SetNext(IMessageHandler next);
        Task HandleAsync(PlayerConnection player, MessageDto dto);
    }
}
