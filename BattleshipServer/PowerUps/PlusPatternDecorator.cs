namespace BattleshipServer.PowerUps
{
    public sealed class PlusPatternDecorator : ShotPatternDecorator
    {
        public PlusPatternDecorator(IShotPattern inner) : base(inner) { }
        public override IEnumerable<(int x,int y)> GetCells(int x,int y,int w=10,int h=10)
        {
            var set = new HashSet<(int,int)>(Inner.GetCells(x,y,w,h));
            var add = new (int dx,int dy)[]{ (0,-1),(0,1),(-1,0),(1,0) };
            foreach (var (dx,dy) in add) if (In(x+dx,y+dy,w,h)) set.Add((x+dx,y+dy));
            return set;
        }
    }
}
