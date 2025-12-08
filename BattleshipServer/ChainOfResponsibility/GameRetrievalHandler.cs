using BattleshipServer.Models;

namespace BattleshipServer.ChainOfResponsibility
{
    public class GameRetrievalHandler : ShotHandler
    {
        protected override Task<bool> ProcessAsync(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            Game? game = manager.GetPlayersGame(player.Id);
            if (game == null){
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
