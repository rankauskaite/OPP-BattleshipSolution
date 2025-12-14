using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public abstract class ClientMessageHandlerBase : IClientMessageHandler
    {
        private IClientMessageHandler? _next;

        public IClientMessageHandler SetNext(IClientMessageHandler next)
        {
            _next = next;
            return next;
        }

        public void Handle(MessageDto dto, MainForm form)
        {
            if (CanHandle(dto))
            {
                Process(dto, form);
                return;
            }

            _next?.Handle(dto, form);
        }

        protected abstract bool CanHandle(MessageDto dto);
        protected abstract void Process(MessageDto dto, MainForm form);
    }
}
