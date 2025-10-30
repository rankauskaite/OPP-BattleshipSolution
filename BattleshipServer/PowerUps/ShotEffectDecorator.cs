using System.Collections.Generic;

namespace BattleshipServer.PowerUps
{
    public abstract class ShotEffectDecorator : IShotEffect
    {
        protected readonly IShotEffect Inner;
        protected ShotEffectDecorator(IShotEffect inner) => Inner = inner;

        public virtual bool AfterCellHit(Shot shot, int[,] board, List<Game.Ship> ships) =>
            Inner?.AfterCellHit(shot, board, ships) ?? false;
    }
}
