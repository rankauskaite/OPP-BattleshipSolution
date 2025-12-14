using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public sealed class PowerUpSummaryHandler : ClientMessageHandlerBase
    {
        private readonly MessageService _service;

        public PowerUpSummaryHandler(MessageService service) => _service = service;

        protected override bool CanHandle(MessageDto dto) => dto.Type == "powerUpSummary";

        protected override void Process(MessageDto dto, MainForm form)
            => _service.HandlePowerUpSummary(dto, form);
    }
}
