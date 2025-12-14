using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public sealed class TurnMessageHandler : ClientMessageHandlerBase
    {
        private readonly MessageService _service;

        public TurnMessageHandler(MessageService service) => _service = service;

        protected override bool CanHandle(MessageDto dto) => dto.Type == "turn";

        protected override void Process(MessageDto dto, MainForm form)
            => _service.HandleTurn(dto, form);
    }
}
