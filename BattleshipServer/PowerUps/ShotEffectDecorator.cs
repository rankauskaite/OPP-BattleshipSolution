// ShotEffectDecorator.cs (NAUJAS)
using System.Collections.Generic;

namespace BattleshipServer.PowerUps
{
    public abstract class ShotEffectDecorator : IShotEffect
    {
        protected readonly IShotEffect Inner;
        protected ShotEffectDecorator(IShotEffect inner) => Inner = inner;

        public virtual bool AfterCellHit(int x, int y, int[,] board, List<Game.Ship> ships)
        {
            // Pagal nutylėjimą – deleguojam „žemyn“, tada galim pridėti savo veiksmą.
            return Inner?.AfterCellHit(x, y, board, ships) ?? false;
        }
    }
}
