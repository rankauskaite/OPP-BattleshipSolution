using BattleshipServer.GameManagerFacade;
using BattleshipServer.Models;

namespace BattleshipServer.ChainOfResponsibility
{
    public class ShotProcessingHandler : ShotHandler
    {
        private readonly MessageDtoService _messageDtoService;

        public ShotProcessingHandler(MessageDtoService messageDtoService)
        {
            _messageDtoService = messageDtoService;
        }

        protected override async Task<bool> ProcessAsync(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            int x = dto.Payload.GetProperty("x").GetInt32();
            int y = dto.Payload.GetProperty("y").GetInt32();
            Game? game = manager.GetPlayersGame(player.Id);
            if (game == null)
            {
                return false;
            }

            Dictionary<string, bool> powerUps = _messageDtoService.GetPowerups(dto);
            powerUps.TryGetValue("doubleBomb", out bool isDoubleBomb);
            powerUps.TryGetValue("plusShape", out bool plusShape);
            powerUps.TryGetValue("xShape", out bool xShape);
            powerUps.TryGetValue("superDamage", out bool superDamage);
            if (plusShape || xShape || superDamage)
            {
                await game.ProcessCompositeShot(player.Id, x, y, isDoubleBomb, plusShape, xShape, superDamage);
            }
            else
            {
                await game.ProcessShot(player.Id, x, y, isDoubleBomb);
            }

            return false;
        }
    }
}
