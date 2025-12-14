using System.Drawing;

namespace BattleshipClient.Mediator
{
    public sealed class BoardShotInput
    {
        private GameBoard? _board;
        private readonly IGameMediator _mediator;

        public BoardShotInput(GameBoard board, IGameMediator mediator)
        {
            _mediator = mediator;
            Attach(board);
        }

        public void Attach(GameBoard board)
        {
            if (_board != null)
                _board.CellClicked -= OnCellClicked;

            _board = board;
            _board.CellClicked += OnCellClicked;
        }

        private void OnCellClicked(object sender, Point p)
        {
            _ = _mediator.RequestShotAsync(p.X, p.Y);
        }
    }
}
