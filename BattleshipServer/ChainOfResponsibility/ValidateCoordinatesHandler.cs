using BattleshipServer.Models;

namespace BattleshipServer.ChainOfResponsibility
{
    public class ValidateCoordinatesHandler : ShotHandler
    {
        protected override Task<bool> ProcessAsync(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            if (!dto.Payload.TryGetProperty("x", out _) || !dto.Payload.TryGetProperty("y", out _))
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
