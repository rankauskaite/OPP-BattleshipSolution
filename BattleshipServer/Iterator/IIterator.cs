namespace BattleshipServer.Iterators
{
    public interface IIterator<out T>
    {
        bool MoveNext();
        T Current { get; }
        void Reset();
    }
}
