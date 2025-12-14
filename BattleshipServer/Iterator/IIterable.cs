namespace BattleshipServer.Iterators
{
    public interface IIterable<out T>
    {
        IIterator<T> GetIterator();
    }
}
