using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public interface IClientMessageHandler
    {
        IClientMessageHandler SetNext(IClientMessageHandler next);
        void Handle(MessageDto dto, MainForm form);
    }
}
