using System;
using System.Collections.Generic;
using BattleshipClient.Models;

namespace BattleshipClient.Services
{
    public sealed class SpreadSafeShipPlacement : ShipPlacementTemplate
    {
        private readonly Random rnd = new Random();

        protected override (int x, int y, bool horiz) ChoosePosition(
            int size, int length, CellState[,] map, List<ShipDto> ships)
        {
            bool horiz = rnd.Next(2) == 0;

            int x = rnd.Next(0, size - (horiz ? length - 1 : 0));
            int y = rnd.Next(0, size - (horiz ? 0 : length - 1));

            return (x, y, horiz);
        }

        protected override bool CanPlace(int length, int x, int y, bool horiz, CellState[,] map)
        {
            if (!base.CanPlace(length, x, y, horiz, map))
                return false;

            int size = map.GetLength(0);

            // buffer zona
            for (int i = -1; i <= length; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int cx = x + (horiz ? i : j);
                    int cy = y + (horiz ? j : i);

                    if (cx < 0 || cy < 0 || cx >= size || cy >= size)
                        continue;

                    if (map[cy, cx] == CellState.Ship)
                        return false;
                }
            }

            return true;
        }
    }
}