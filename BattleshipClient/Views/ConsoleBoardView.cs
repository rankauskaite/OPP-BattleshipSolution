using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;
using BattleshipClient.Iterators;


namespace BattleshipClient.Views
{
    public class ConsoleBoardView : BoardView
    {
        public ConsoleBoardView()
            : base(new ConsoleRendererAdapter(new SystemConsoleIO()))
        {
        }

        public ConsoleBoardView(IBoardRenderer renderer) : base(renderer) { }

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
