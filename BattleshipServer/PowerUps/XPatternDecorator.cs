using System.Collections.Generic;
using System.Linq;

namespace BattleshipServer.PowerUps
{
    public sealed class XPatternDecorator : ShotPatternDecorator
    {
        public XPatternDecorator(IShotPattern inner) : base(inner) { }
        public override IEnumerable<Shot> GetShots(Shot origin, int w = 10, int h = 10)
        {
            var set = new HashSet<(int,int)>(Inner.GetShots(origin, w, h).Select(s => (s.X, s.Y)));
            var add = new (int dx,int dy)[]{ (-1,-1),(1,-1),(-1,1),(1,1) };
            foreach (var (dx,dy) in add)
                if (In(origin.X+dx, origin.Y+dy, w, h))
                    set.Add((origin.X+dx, origin.Y+dy));

            return set.Select(p => new Shot(p.Item1, p.Item2));
        }
    }
}
