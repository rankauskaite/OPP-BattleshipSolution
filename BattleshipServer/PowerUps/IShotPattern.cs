using System.Collections.Generic;

namespace BattleshipServer.PowerUps
{
    public interface IShotPattern
    {
        IEnumerable<Shot> GetShots(Shot origin, int w = 10, int h = 10);
    }

    public sealed class SingleCellPattern : IShotPattern
    {
        public IEnumerable<Shot> GetShots(Shot origin, int w = 10, int h = 10)
        {
            if (origin.X >= 0 && origin.X < w && origin.Y >= 0 && origin.Y < h)
                yield return origin;
        }
    }

    public abstract class ShotPatternDecorator : IShotPattern
    {
        protected readonly IShotPattern Inner;
        protected ShotPatternDecorator(IShotPattern inner) => Inner = inner;

        public abstract IEnumerable<Shot> GetShots(Shot origin, int w = 10, int h = 10);

        protected static bool In(int x, int y, int w, int h) =>
            x >= 0 && x < w && y >= 0 && y < h;
    }
}
