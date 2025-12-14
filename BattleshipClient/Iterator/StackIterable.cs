using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleshipClient.Iterators
{
    public sealed class StackIterable<T> : IIterable<T>
    {
        private readonly Stack<T> _stack;

        public StackIterable(Stack<T> stack)
        {
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
        }

        public IIterator<T> GetIterator() => new StackIterator(_stack);

        private sealed class StackIterator : IIterator<T>
        {
            private readonly T[] _snapshotTopFirst;
            private Stack<T> _work;

            public T Current { get; private set; } = default!;

            public StackIterator(Stack<T> source)
            {
                // snapshot top-first (Stack.ToArray() grąžina nuo viršaus)
                _snapshotTopFirst = source.ToArray();
                _work = new Stack<T>(_snapshotTopFirst.Reverse()); // kad Pop() eitų top-first
            }

            public bool MoveNext()
            {
                if (_work.Count == 0) return false;
                Current = _work.Pop(); // reali STACK operacija
                return true;
            }

            public void Reset()
            {
                _work = new Stack<T>(_snapshotTopFirst.Reverse());
                Current = default!;
            }
        }
    }
}
