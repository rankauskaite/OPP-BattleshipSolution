using System.Collections.Generic;
using System.Windows.Input;

namespace BattleshipClient.Commands
{
    public class GameCommandManager
    {
        private readonly List<ICommand> _commands = new();
        private int _currentIndex = -1;

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _commands.Add(command);
            _currentIndex = _commands.Count - 1;
        }

        public void Undo()
        {
            if (_currentIndex >= 0)
            {
                _commands[_currentIndex].Undo();
                _currentIndex--;
            }
        }

        public void Redo()
        {
            if (_currentIndex + 1 < _commands.Count)
            {
                _currentIndex++;
                _commands[_currentIndex].Execute();
            }
        }

        public bool CanUndo => _currentIndex >= 0;
        public bool CanRedo => _currentIndex + 1 < _commands.Count;
        public int TotalCommands => _commands.Count;
        public int CurrentIndex => _currentIndex;
    }
}