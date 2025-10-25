// SuperDamageDecorator.cs (NAUJAS)
using System.Collections.Generic;
using System.Linq;

namespace BattleshipServer.PowerUps
{
    /// Pataikius į laivą – pažymi VISĄ laivą nuskendusiu.
    public sealed class SuperDamageDecorator : ShotEffectDecorator
    {
        public SuperDamageDecorator(IShotEffect inner) : base(inner) { }

        public override bool AfterCellHit(int x, int y, int[,] board, List<Game.Ship> ships)
        {
            // Pirmiausia – bazinė grandinė (jei yra).
            bool sunkByInner = base.AfterCellHit(x, y, board, ships);

            // Tada – mūsų poveikis.
            var ship = ships.FirstOrDefault(s =>
                (s.Horizontal && y == s.Y && x >= s.X && x < s.X + s.Len) ||
                (!s.Horizontal && x == s.X && y >= s.Y && y < s.Y + s.Len));

            if (ship == null) return sunkByInner;

            ship.setAsSunk(board); // pažymi visas laivo ląsteles SUNK
            return true;           // dabar tikrai nuskendęs
        }
    }
}
