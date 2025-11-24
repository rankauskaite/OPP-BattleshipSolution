using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace BattleshipClient.Iterators
{
    public sealed class SpiralCells : IBoardCellEnumerable, IEnumerable<Point>
    {
        private readonly int _size;
        public SpiralCells(int size) => _size = size;

        public IBoardCellIterator GetIterator() => new SpiralIterator(_size);

        public IEnumerator<Point> GetEnumerator()
        {
            var it = new SpiralIterator(_size);
            while (it.MoveNext()) yield return it.Current;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class SpiralIterator : IBoardCellIterator
        {
            private readonly int _n;
            private int top, left, bottom, right;
            private int x, y, dir; // 0:R,1:D,2:L,3:U
            private bool started, done;

            public SpiralIterator(int n)
            {
                _n = n;
                Reset();
            }

            public Point Current { get; private set; }

            public bool MoveNext()
            {
                if (done) return false;

                if (!started)
                {
                    started = true;
                    x = left; y = top; dir = 0;
                    Current = new Point(x, y);
                    return true;
                }

                switch (dir)
                {
                    case 0: // R
                        if (x < right) x++;
                        else { dir = 1; top++; if (top > bottom) { done = true; return false; } y++; }
                        break;
                    case 1: // D
                        if (y < bottom) y++;
                        else { dir = 2; right--; if (left > right) { done = true; return false; } x--; }
                        break;
                    case 2: // L
                        if (x > left) x--;
                        else { dir = 3; bottom--; if (top > bottom) { done = true; return false; } y--; }
                        break;
                    case 3: // U
                        if (y > top) y--;
                        else { dir = 0; left++; if (left > right) { done = true; return false; } x++; }
                        break;
                }

                if (done) return false;
                Current = new Point(x, y);
                return true;
            }

            public void Reset()
            {
                top = 0; left = 0; bottom = _n - 1; right = _n - 1;
                started = false; done = false;
                x = y = dir = 0;
            }
        }
    }
}
