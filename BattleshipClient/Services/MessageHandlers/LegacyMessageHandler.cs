using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public sealed class LegacyMessageHandler : ClientMessageHandlerBase
    {
        private readonly MessageService _service;

        public LegacyMessageHandler(MessageService service) => _service = service;

        protected override bool CanHandle(MessageDto dto) => true;

        protected override void Process(MessageDto dto, MainForm form)
            => _service.LegacyHandle(dto, form);
    }
}
