namespace BattleshipClient.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}