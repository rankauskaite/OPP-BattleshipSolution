using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;
using System.Drawing;
using BattleshipClient.Iterators;

namespace BattleshipClient.Views
{
    public class ColorfulBoardView : BoardView
    {
        public ColorfulBoardView(IBoardRenderer renderer) : base(renderer) { }

        public override void DrawBoard(GameBoard board)
        {
            var it = new RowMajorCells(board.Size).GetIterator();
            while (it.MoveNext())
            {
                var p = it.Current;
                var state = board.GetCell(p.X, p.Y);
                renderer.RenderCell(p.X, p.Y, state, board);
            }
        }
    }
}