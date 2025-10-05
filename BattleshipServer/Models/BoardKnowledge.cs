using System.Collections.Generic;

namespace BattleshipServer.Models
{
    public readonly record struct Cell(int X, int Y);

    /// <summary>
    /// Minimalus „ką žinau apie oponentą“ modelis: kur jau šauta ir kur pataikyta.
    /// NPC jam remiasi, kad nekartotų ėjimų ir galėtų „medžioti“.
    /// </summary>
    public sealed class BoardKnowledge
    {
        private readonly int _w, _h;
        private readonly bool[,] _shot;
        private readonly bool[,] _hit;

        public BoardKnowledge(int width, int height)
        {
            _w = width; _h = height;
            _shot = new bool[_w, _h];
            _hit = new bool[_w, _h];
        }

        public IEnumerable<Cell> UnshotCells()
        {
            for (int x = 0; x < _w; x++)
                for (int y = 0; y < _h; y++)
                    if (!_shot[x, y]) yield return new Cell(x, y);
        }

        public void MarkShot(Cell c, bool isHit)
        {
            _shot[c.X, c.Y] = true;
            if (isHit) _hit[c.X, c.Y] = true;
        }

        public IEnumerable<Cell> HitFrontier4()
        {
            var dirs = new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) };
            var seen = new HashSet<Cell>();
            for (int x = 0; x < _w; x++)
                for (int y = 0; y < _h; y++)
                    if (_hit[x, y])
                        foreach (var d in dirs)
                        {
                            var n = new Cell(x + d.X, y + d.Y);
                            if (n.X >= 0 && n.X < _w && n.Y >= 0 && n.Y < _h && !_shot[n.X, n.Y] && seen.Add(n))
                                yield return n;
                        }
        }
    }
}
