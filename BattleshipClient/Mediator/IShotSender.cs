using System.Threading.Tasks;

namespace BattleshipClient.Mediator
{
    public interface IShotSender
    {
        Task SendAsync(object message);
    }
}
