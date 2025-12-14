using System.Collections.Generic;
using System.Windows.Input;
using BattleshipClient.Iterators;

namespace BattleshipClient.Commands
{
    public class GameCommandManager
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        public int TotalCommands => _undoStack.Count + _redoStack.Count;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
        }

        public void UndoAll()
        {
            while (CanUndo)
                Undo();
        }

        public void RedoAll()
        {
            while (CanRedo)
                Redo();
        }

        public void Reset()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public IIterable<ICommand> GetUndoHistory() => new StackIterable<ICommand>(_undoStack);
        public IIterable<ICommand> GetRedoHistory() => new StackIterable<ICommand>(_redoStack);

    }
}