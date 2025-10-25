using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipServer.GameFacade
{
    public class ShipService
    {
        private readonly PlayerService playerService;
        private readonly SendMessageService messageService;
        private readonly ShotService shotService;

        public ShipService(PlayerService playerService, SendMessageService messageService, ShotService shotService)
        {
            this.playerService = playerService;
            this.messageService = messageService;
            this.shotService = shotService;
        }

        public async Task HandleSunkShipsAsync(Guid shooterId, Game game, int x, int y)
        {
            bool anyLeft = false;
            bool wholeDown = false;
            List<(int x, int y)> sunkCells = null;

            var target = playerService.GetOpponent(shooterId, game);
            (int[,] targetBoard, List<Game.Ship> targetShips) = playerService.GetTargetBoardAndShips(target, game);

            foreach (var s in targetShips)
            {
                bool is_sunk = s.IsSunk(targetBoard);
                bool containsCurrentShot =
                    (s.Horizontal && (y == s.Y) && (x >= s.X) && (x < s.X + s.Len)) ||
                    (!s.Horizontal && (x == s.X) && (y >= s.Y) && (y < s.Y + s.Len));
                if (!is_sunk)
                {
                    anyLeft = true;
                }
                else
                {
                    s.setAsSunk(targetBoard);
                    if (containsCurrentShot)
                    {
                        wholeDown = true;
                        SetAsSunkCurrentShip(game, s, shooterId, target, sunkCells);
                    }
                }
            }

            if (!anyLeft)
            {
                shotService.gameOver = true;
            }
            bool hit = shotService.lastShootHit;
            await messageService.SendShotInfo(game.Player1, game.Player2, shooterId, target, x, y, hit, wholeDown);
            game.InvokeShotResolved(shooterId, x, y, hit, wholeDown, sunkCells);
        }

        private async void SetAsSunkCurrentShip(Game game, Game.Ship ship, Guid shooterId, PlayerConnection target, List<(int x, int y)> sunkCells)
        {
            sunkCells = new List<(int, int)>();
            for (int i = 0; i < ship.Len; i++)
            {
                int cx1 = ship.X + (ship.Horizontal ? i : 0);
                int cy1 = ship.Y + (ship.Horizontal ? 0 : i);
                if (cx1 < 0 || cx1 >= 10 || cy1 < 0 || cy1 >= 10) break;
                await messageService.SendWholeShipDown(game.Player1, game.Player2, shooterId, target, cx1, cy1);
                sunkCells.Add((cx1, cy1));
            }
        }

    }
}
