using BattleshipServer.Data;
using BattleshipServer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer.GameFacade
{
    public class GameFacade
    {
        private readonly SendMessageService messageService;
        private readonly PlayerService playerService;
        private readonly ShotService shotService;
        private readonly ShipService shipService;

        public GameFacade() {
            messageService = new SendMessageService();
            playerService = new PlayerService();
            shotService = new ShotService(messageService, playerService);
            shipService = new ShipService(playerService, messageService, shotService);
        }

        public async Task HandleShot(Game game, Guid shooterId, int x, int y, bool isDoubleBomb)
        {
            if (shooterId != game.CurrentPlayerId)
            {
                await messageService.SendErrorAsync(playerService.GetPlayer(shooterId, game), "Not your turn");
                return;
            }

            if (game.GetIsGameOver())
            {
                await messageService.SendErrorAsync(playerService.GetPlayer(shooterId, game), "Game already finished");
                return;
            }


            shotService.ProcessShot(game, shooterId, x, y, isDoubleBomb);


            await shipService.HandleSunkShipsAsync(shooterId, game, x, y);

            if (shotService.gameOver)
            {
                await messageService.SendGameOverAsync(game.Player1, game.Player2, shooterId);

                game.SetIsGameOver(true);
                game.SaveGameToDB(shooterId);
                Console.WriteLine($"[Game] Game over. Winner: {shooterId.ToString()}");
            }
            else
            {
                // change turn only on miss
                if (!shotService.lastShootHit)
                {
                    var target = playerService.GetOpponent(shooterId, game);
                    game.SetCurrentPlayer(target.Id);
                }
                await messageService.SendTurnMessage(game.Player1, game.Player2, game.CurrentPlayerId);
            }
        }
    }
}
