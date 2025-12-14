using BattleshipClient.Models;
using System.Windows.Input;

namespace BattleshipClient.Commands
{
    public class ShotCommand : ICommand
    {
        private readonly GameBoard _board;
        private readonly int _x;
        private readonly int _y;
        private readonly CellState _previousState;
        private readonly CellState _newState;

        public string ShooterName { get; }

        public ShotCommand(GameBoard board, int x, int y, CellState previous, CellState result, string shooter)
        {
            _board = board;
            _x = x;
            _y = y;
            _previousState = previous;
            _newState = result;
            ShooterName = shooter;
        }

        public void Execute()
        {
            _board.SetCell(_x, _y, _newState);
        }

        public void Undo()
        {
            _board.SetCell(_x, _y, _previousState);
        }

        public override string ToString()
        {
            char col = (char)('A' + _x);
            return $"{ShooterName}: {col}{_y + 1} ({_previousState} -> {_newState})";
        }

    }
}