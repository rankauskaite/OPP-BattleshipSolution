using System;
using System.Collections.Generic;
using BattleshipServer.PowerUps;

namespace BattleshipServer.Iterators
{
    public sealed class XShotTargets : IIterable<Shot>
    {
        private readonly Shot[] _shots;

        public XShotTargets(Shot origin, int w, int h)
        {
            var list = new List<Shot>(5);

            void AddIfIn(int x, int y)
            {
                if (x >= 0 && x < w && y >= 0 && y < h)
                    list.Add(new Shot(x, y));
            }

            AddIfIn(origin.X, origin.Y);
            AddIfIn(origin.X - 1, origin.Y - 1);
            AddIfIn(origin.X + 1, origin.Y - 1);
            AddIfIn(origin.X - 1, origin.Y + 1);
            AddIfIn(origin.X + 1, origin.Y + 1);

            _shots = list.ToArray();
        }

        public IIterator<Shot> GetIterator() => new ArrIt(_shots);

        private sealed class ArrIt : IIterator<Shot>
        {
            private readonly Shot[] _a;
            private int _i = -1;

            public ArrIt(Shot[] a) => _a = a;

            public Shot Current { get; private set; } = default!;

            public bool MoveNext()
            {
                _i++;
                if (_i >= _a.Length) return false;
                Current = _a[_i];
                return true;
            }

            public void Reset() { _i = -1; Current = default!; }
        }
    }
}
