using System.Collections.Generic;

namespace BattleshipServer.PowerUps
{
    public interface IShotEffect
    {
        bool AfterCellHit(Shot shot, int[,] targetBoard, List<Game.Ship> targetShips);
    }

    public sealed class NoopEffect : IShotEffect
    {
        public bool AfterCellHit(Shot shot, int[,] board, List<Game.Ship> ships) => false;
    }
}
