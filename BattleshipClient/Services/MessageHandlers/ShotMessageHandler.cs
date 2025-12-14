using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public sealed class ShotMessageHandler : ClientMessageHandlerBase
    {
        private readonly MessageService _service;

        public ShotMessageHandler(MessageService service) => _service = service;

        protected override bool CanHandle(MessageDto dto)
            => dto.Type == "shotInfo" || dto.Type == "shotResult";

        protected override void Process(MessageDto dto, MainForm form)
            => _service.HandleShotMessage(dto, form);
    }
}
