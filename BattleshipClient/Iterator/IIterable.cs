namespace BattleshipClient.Iterators
{
    public interface IIterable<out T>
    {
        IIterator<T> GetIterator();
    }
}
