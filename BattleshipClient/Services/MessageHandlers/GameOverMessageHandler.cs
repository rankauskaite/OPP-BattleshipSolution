using BattleshipClient.Models;

namespace BattleshipClient.Services.MessageHandlers
{
    public sealed class GameOverMessageHandler : ClientMessageHandlerBase
    {
        private readonly MessageService _service;

        public GameOverMessageHandler(MessageService service) => _service = service;

        protected override bool CanHandle(MessageDto dto) => dto.Type == "gameOver";

        protected override void Process(MessageDto dto, MainForm form)
            => _service.HandleGameOver(dto, form);
    }
}
