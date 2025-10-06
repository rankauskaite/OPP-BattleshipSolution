using BattleshipClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient.Services
{
    class ShipPlacementService
    {
        private int boardSize { get; } = 10;
        private int[] lens { get; } = new int[] { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        public ShipPlacementService()
        {
        }

        public ShipPlacementService(int boardSize, int[] lens)
        {
            this.boardSize = boardSize;
            this.lens = lens;
        }

        public (List<ShipDto> ships, CellState[,] map) RandomizeShips()
        {
            var rnd = new Random();
            List<ShipDto> ships = new List<ShipDto>();
            var temp = new CellState[GameBoard.Size, GameBoard.Size];

            foreach (var len in lens)
            {
                bool placed = false;
                int tries = 0;
                while (!placed && tries < 200)
                {
                    tries++;
                    bool horiz = rnd.Next(2) == 0;
                    int x = rnd.Next(0, GameBoard.Size - (horiz ? len - 1 : 0));
                    int y = rnd.Next(0, GameBoard.Size - (horiz ? 0 : len - 1));
                    bool ok = true;
                    for (int i = 0; i < len; i++)
                    {
                        int cx = x + (horiz ? i : 0);
                        int cy = y + (horiz ? 0 : i);
                        if (temp[cy, cx] != CellState.Empty) { ok = false; break; }
                    }
                    if (ok)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            int cx = x + (horiz ? i : 0);
                            int cy = y + (horiz ? 0 : i);
                            temp[cy, cx] = CellState.Ship;
                        }
                        ships.Add(new ShipDto { x = x, y = y, len = len, dir = horiz ? "H" : "V" });
                        placed = true;
                    }
                }
            }

            return (ships, temp);
        }

        public static bool CanPlaceShip(GameBoard board, int x, int y, int len, bool horiz)
        {
            if (horiz && x + len > GameBoard.Size) return false;
            if (!horiz && y + len > GameBoard.Size) return false;

            for (int i = 0; i < len; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                if (board.GetCell(cx, cy) != CellState.Empty) return false;
            }
            return true;
        }

        public static void PlaceShip(GameBoard board, int x, int y, int len, bool horiz)
        {
            for (int i = 0; i < len; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                board.SetCell(cx, cy, CellState.Ship);
            }
        }
    }
}
