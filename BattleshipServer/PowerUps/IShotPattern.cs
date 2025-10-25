namespace BattleshipServer.PowerUps
{
    public interface IShotPattern
    {
        IEnumerable<(int x,int y)> GetCells(int x, int y, int w = 10, int h = 10);
    }

    public sealed class SingleCellPattern : IShotPattern
    {
        public IEnumerable<(int x,int y)> GetCells(int x, int y, int w = 10, int h = 10)
        {
            if (x>=0 && x<w && y>=0 && y<h) yield return (x,y);
        }
    }

    public abstract class ShotPatternDecorator : IShotPattern
    {
        protected readonly IShotPattern Inner;
        protected ShotPatternDecorator(IShotPattern inner) => Inner = inner;
        public abstract IEnumerable<(int x,int y)> GetCells(int x, int y, int w = 10, int h = 10);
        protected static bool In(int x,int y,int w,int h) => x>=0 && x<w && y>=0 && y<h;
    }
}
