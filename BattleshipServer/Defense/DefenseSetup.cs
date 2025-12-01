using System;
using System.Collections.Generic;
using System.Linq;
using BattleshipServer.Defense; 
using BattleshipServer;

namespace BattleshipServer.Defense
{
    public static class DefenseSetup
    {
        private static readonly Random _rng = new Random();

        public static void SetupRandomDefense(Game game)
        {
            SetupForPlayer(game, game.Player1.Id);
            SetupForPlayer(game, game.Player2.Id);
        }

        private static void SetupForPlayer(Game game, Guid playerId)
        {
            var (board, ships) = playerId == game.Player1.Id
                ? game.GetBoard1AndShips()
                : game.GetBoard2AndShips();

            var shipCells = new List<(int x, int y)>();
            foreach (var ship in ships)
            {
                for (int i = 0; i < ship.Len; i++)
                {
                    int x = ship.X + (ship.Horizontal ? i : 0);
                    int y = ship.Y + (ship.Horizontal ? 0 : i);
                    if (x >= 0 && x < 10 && y >= 0 && y < 10)
                    {
                        shipCells.Add((x, y));
                    }
                }
            }

            if (!shipCells.Any())
                return;

            // 1) Viena 3x3 SAFETINESS zona (AreaShield)
            var center1 = shipCells[_rng.Next(shipCells.Count)];
            game.AddAreaShield(
                playerId,
                center1.x - 1, center1.y - 1,
                center1.x + 1, center1.y + 1,
                DefenseMode.Safetiness);

            // 2) Viena 3x3 VISIBILITY zona (AreaShield)
            var center2 = shipCells[_rng.Next(shipCells.Count)];
            game.AddAreaShield(
                playerId,
                center2.x - 1, center2.y - 1,
                center2.x + 1, center2.y + 1,
                DefenseMode.Visibility);

            // 3) Dar 2 atskiri vieno langelio SAFETINESS skydai (CellShield)
            for (int i = 0; i < 2; i++)
            {
                var cell = shipCells[_rng.Next(shipCells.Count)];
                game.AddCellShield(playerId, cell.x, cell.y, DefenseMode.Safetiness);
            }
        }
    }
}
