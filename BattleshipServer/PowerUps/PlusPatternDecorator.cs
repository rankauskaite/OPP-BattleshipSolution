using System.Collections.Generic;
using System.Linq;

namespace BattleshipServer.PowerUps
{
    public sealed class PlusPatternDecorator : ShotPatternDecorator
    {
        public PlusPatternDecorator(IShotPattern inner) : base(inner) { }

        public override IEnumerable<Shot> GetShots(Shot origin, int w = 10, int h = 10)
        {
            var set = new HashSet<(int,int)>(
                Inner.GetShots(origin, w, h).Select(s => (s.X, s.Y)));

            var add = new (int dx,int dy)[]{ (0,-1),(0,1),(-1,0),(1,0) };

            foreach (var (dx,dy) in add)
            {
                int nx = origin.X + dx;
                int ny = origin.Y + dy;
                if (In(nx, ny, w, h))
                    set.Add((nx, ny));
            }

            return set.Select(p => new Shot(p.Item1, p.Item2));
        }
    }
}
