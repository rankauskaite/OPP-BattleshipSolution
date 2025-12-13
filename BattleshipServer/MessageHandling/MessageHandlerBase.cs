using System.Threading.Tasks;
using BattleshipServer.Models;

namespace BattleshipServer.MessageHandling
{
    /// <summary>
    /// Chain of Responsibility: base handler with Next link.
    /// </summary>
    public abstract class MessageHandlerBase : IMessageHandler
    {
        protected readonly GameManager Manager;
        private IMessageHandler? _next;

        protected MessageHandlerBase(GameManager manager)
        {
            Manager = manager;
        }

        public IMessageHandler SetNext(IMessageHandler next)
        {
            _next = next;
            return next;
        }

        public virtual Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            return _next?.HandleAsync(player, dto) ?? Task.CompletedTask;
        }

        protected Task Next(PlayerConnection player, MessageDto dto)
        {
            return _next?.HandleAsync(player, dto) ?? Task.CompletedTask;
        }
    }
}
