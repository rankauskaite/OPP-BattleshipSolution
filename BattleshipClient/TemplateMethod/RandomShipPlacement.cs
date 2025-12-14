using System;
using System.Collections.Generic;
using BattleshipClient.Models;

namespace BattleshipClient.TemplateMethod
{
    public sealed class RandomShipPlacement : ShipPlacementTemplate
    {
        private readonly Random _rnd = new Random();

        protected override (int x, int y, bool horiz) ChoosePosition(
            int size,
            int length,
            CellState[,] map,
            List<ShipDto> ships)
        {
            bool horiz = _rnd.Next(2) == 0;

            int x = _rnd.Next(0, size - (horiz ? length - 1 : 0));
            int y = _rnd.Next(0, size - (horiz ? 0 : length - 1));

            return (x, y, horiz);
        }
    }
}