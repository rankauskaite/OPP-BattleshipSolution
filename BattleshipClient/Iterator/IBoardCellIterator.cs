using System.Drawing;

namespace BattleshipClient.Iterators
{
    public interface IBoardCellIterator
    {
        bool MoveNext();
        Point Current { get; }
        void Reset();
    }
}
