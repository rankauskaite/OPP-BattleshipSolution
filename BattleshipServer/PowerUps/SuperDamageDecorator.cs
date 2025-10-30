using System.Collections.Generic;
using System.Linq;

namespace BattleshipServer.PowerUps
{
    public sealed class SuperDamageDecorator : ShotEffectDecorator
    {
        public SuperDamageDecorator(IShotEffect inner) : base(inner) { }

        public override bool AfterCellHit(Shot shot, int[,] board, List<Game.Ship> ships)
        {
            bool fromInner = base.AfterCellHit(shot, board, ships);

            var s = ships.FirstOrDefault(ship =>
                (ship.Horizontal && shot.Y == ship.Y && shot.X >= ship.X && shot.X < ship.X + ship.Len) ||
                (!ship.Horizontal && shot.X == ship.X && shot.Y >= ship.Y && shot.Y < ship.Y + ship.Len));

            if (s == null) return fromInner;

            s.setAsSunk(board);
            return true;
        }
    }
}
