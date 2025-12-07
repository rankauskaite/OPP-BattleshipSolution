using BattleshipServer.Models;
using BattleshipServer.Npc;

namespace BattleshipServer.ChainOfResponsibility
{
    public class BotTriggerHandler : ShotHandler
    {
        protected override async Task<bool> ProcessAsync(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            (Game? game, IBotPlayerController? bot) botGame = manager.GetBotGame(player.Id);
            if (botGame.bot != null)
            {
                await botGame.bot.MaybePlayAsync();
            }
            return true;
        }
    }
}
