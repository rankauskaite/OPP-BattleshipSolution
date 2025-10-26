using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;

namespace BattleshipClient.Views
{
    public class ClassicBoardView : BoardView
    {
        public ClassicBoardView(IBoardRenderer renderer) : base(renderer) { }

        public override void DrawBoard(GameBoard board)
        {
            for (int y = 0; y < board.Size; y++)
            {
                for (int x = 0; x < board.Size; x++)
                {
                    var state = board.GetCell(x, y);
                    renderer.RenderCell(x, y, state, board);
                }
            }
        }
    }
}