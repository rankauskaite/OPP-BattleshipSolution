using System;
using System.Collections.Generic;
using BattleshipClient.Models;

namespace BattleshipClient.TemplateMethod
{
    public sealed class EdgeShipPlacement : ShipPlacementTemplate
    {
        private readonly Random _rnd = new Random();

        protected override (int x, int y, bool horiz) ChoosePosition(
            int size,
            int length,
            CellState[,] map,
            List<ShipDto> ships)
        {
            // 0 - viršus, 1 - apačia, 2 - kairė, 3 - dešinė
            int side = _rnd.Next(4);
            bool horiz;
            int x, y;

            switch (side)
            {
                case 0:
                    horiz = true;
                    x = _rnd.Next(0, size - length + 1);
                    y = 0;
                    break;

                case 1:
                    horiz = true;
                    x = _rnd.Next(0, size - length + 1);
                    y = size - 1;
                    break;

                case 2:
                    horiz = false;
                    x = 0;
                    y = _rnd.Next(0, size - length + 1);
                    break;

                default:
                    horiz = false;
                    x = size - 1;
                    y = _rnd.Next(0, size - length + 1);
                    break;
            }

            return (x, y, horiz);
        }
    }
}