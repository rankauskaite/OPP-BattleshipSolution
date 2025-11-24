using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace BattleshipClient.Iterators
{
    public sealed class RowMajorCells : IBoardCellEnumerable, IEnumerable<Point>
    {
        private readonly int _size;

        public RowMajorCells(int size) => _size = size;

        // Iterator (mūsų) – naudinga gynime parodyti
        public IBoardCellIterator GetIterator() => new RowMajorIterator(_size);

        // Patogumui – kad veiktų foreach natūraliai
        public IEnumerator<Point> GetEnumerator() => new RowMajorEnumerator(_size);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class RowMajorIterator : IBoardCellIterator
        {
            private readonly int _n;
            private int _r = 0, _c = -1;

            public RowMajorIterator(int n) { _n = n; }
            public Point Current { get; private set; }

            public bool MoveNext()
            {
                _c++;
                if (_c >= _n) { _c = 0; _r++; }
                if (_r >= _n) return false;
                Current = new Point(_c, _r);
                return true;
            }

            public void Reset() { _r = 0; _c = -1; }
        }

        private sealed class RowMajorEnumerator : IEnumerator<Point>
        {
            private readonly int _n;
            private int _r = 0, _c = -1;

            public RowMajorEnumerator(int n) { _n = n; }

            public Point Current => new Point(_c, _r);
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _c++;
                if (_c >= _n) { _c = 0; _r++; }
                return _r < _n;
            }

            public void Reset() { _r = 0; _c = -1; }
            public void Dispose() { }
        }
    }
}
