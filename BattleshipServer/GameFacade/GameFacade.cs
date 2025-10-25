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
        private readonly ShootingService shootingService;
        private readonly ShipService shipService;
        private readonly GameManager manager;
        private readonly Database db;

        public GameFacade(GameManager _manager, Database _db) {
            messageService = new SendMessageService();
            playerService = new PlayerService();
            shootingService = new ShootingService(messageService, playerService);
            shipService = new ShipService(playerService, messageService, shootingService);
            manager = _manager;
            db = _db;
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


            shootingService.ProcessShot(game, shooterId, x, y, isDoubleBomb, manager, db);


            await shipService.HandleSunkShipsAsync(shooterId, game, x, y);

            if (shootingService.gameOver)
            {
                await messageService.SendGameOverAsync(game.Player1, game.Player2, shooterId);

                game.SetIsGameOver(true);
                manager.GameEnded(game);
                db.SaveGame(playerService.GetPLayerName(game.Player1), playerService.GetPLayerName(game.Player2), shooterId.ToString());
                Console.WriteLine($"[Game] Game over. Winner: {shooterId.ToString()}");
            }
            else
            {
                // change turn only on miss
                if (!shootingService.lastShootHit)
                {
                    var target = playerService.GetOpponent(shooterId, game);
                    game.SetCurrentPlayer(target.Id);
                }
                await messageService.SendTurnMessage(game.Player1, game.Player2, game.CurrentPlayerId);
            }
        }
    }
}
