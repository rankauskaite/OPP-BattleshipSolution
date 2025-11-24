using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;
using BattleshipClient.Iterators;

namespace BattleshipClient.Views
{
    public class ClassicBoardView : BoardView
    {
        public ClassicBoardView(IBoardRenderer renderer) : base(renderer) { }

        public override void DrawBoard(GameBoard board)
        {
            foreach (var p in new RowMajorCells(board.Size))
            {
                var state = board.GetCell(p.X, p.Y);
                renderer.RenderCell(p.X, p.Y, state, board);
            }
        }
    }
}