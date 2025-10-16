using System.Collections.Generic;

namespace BattleshipServer.Models
{
    // Paprasta koordinatė
    public readonly record struct Cell(int X, int Y);

    // Būsena, kurią „žino“ NPC apie kiekvieną langelį oponento lentoje
    public enum CellMark
    {
        Unknown = 0,
        Miss    = 1,
        Hit     = 2,
        Sunk    = 3
    }

    /// <summary>
    /// Minimalus „ką žinau apie oponentą“ modelis:
    /// - Unknown: dar nešaudyta
    /// - Miss: šauta ir pramesta
    /// - Hit: pataikyta (bet laivas dar nenuskendęs)
    /// - Sunk: ląstelė priklauso nuskendusiam laivui
    ///
    /// Strategijos remiasi šiuo modeliu, kad nekartotų ėjimų ir „medžiotų“ aplink HIT,
    /// bet nebešaudytų aplink nuskendusius (SUNK).
    /// </summary>
    public sealed class BoardKnowledge
    {
        private readonly int _w, _h;
        private readonly CellMark[,] _m; // [x, y]

        public BoardKnowledge(int width, int height)
        {
            _w = width; _h = height;
            _m = new CellMark[_w, _h];
            // pagal nutylėjimą viskas Unknown (0)
        }

        public bool In(int x, int y) => x >= 0 && x < _w && y >= 0 && y < _h;

        public CellMark Get(int x, int y) => _m[x, y];

        public void Set(int x, int y, CellMark mark) => _m[x, y] = mark;

        public void MarkMiss(int x, int y) => _m[x, y] = CellMark.Miss;

        public void MarkHit(int x, int y) => _m[x, y] = CellMark.Hit;

        public void MarkSunk(int x, int y) => _m[x, y] = CellMark.Sunk;

        public void MarkShot(Cell c, bool isHit)
        {
            if (isHit) _m[c.X, c.Y] = CellMark.Hit;
            else       _m[c.X, c.Y] = CellMark.Miss;
        }

        public void MarkSunk(IEnumerable<Cell> cells)
        {
            foreach (var c in cells)
                if (In(c.X, c.Y)) _m[c.X, c.Y] = CellMark.Sunk;
        }

        /// <summary>
        /// Visi dar nešaudyti (Unknown) langeliai.
        /// </summary>
        public IEnumerable<Cell> UnshotCells()
        {
            for (int x = 0; x < _w; x++)
                for (int y = 0; y < _h; y++)
                    if (_m[x, y] == CellMark.Unknown)
                        yield return new Cell(x, y);
        }

        /// <summary>
        /// 4-krypčių „frontier“ aplink visus HIT langelius (NE aplink SUNK).
        /// Grąžina tik tuos kaimynus, į kuriuos dar nešaudyta (Unknown).
        /// </summary>
        public IEnumerable<Cell> HitFrontier4()
        {
            var dirs = new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) };
            var seen = new HashSet<Cell>();

            for (int x = 0; x < _w; x++)
            {
                for (int y = 0; y < _h; y++)
                {
                    if (_m[x, y] != CellMark.Hit) continue; // svarbu: ignoruojam Sunk

                    foreach (var d in dirs)
                    {
                        int nx = x + d.X, ny = y + d.Y;
                        if (In(nx, ny) && _m[nx, ny] == CellMark.Unknown)
                        {
                            var n = new Cell(nx, ny);
                            if (seen.Add(n)) yield return n;
                        }
                    }
                }
            }
        }
    }
}
