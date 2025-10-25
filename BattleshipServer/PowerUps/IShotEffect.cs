// IShotEffect.cs
using System.Collections.Generic;

namespace BattleshipServer.PowerUps
{
    public interface IShotEffect
    {
        /// Grįžta true, jei pritaikius efektą bent vienas laivas tapo nuskendęs.
        bool AfterCellHit(int x, int y, int[,] targetBoard, List<Game.Ship> targetShips);
    }

    public sealed class NoopEffect : IShotEffect
    {
        public bool AfterCellHit(int x, int y, int[,] board, List<Game.Ship> ships) => false;
    }
}
