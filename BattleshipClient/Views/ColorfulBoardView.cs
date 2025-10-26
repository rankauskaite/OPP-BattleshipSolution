using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;
using System.Drawing;

namespace BattleshipClient.Views
{
    public class ColorfulBoardView : BoardView
    {
        public ColorfulBoardView(IBoardRenderer renderer) : base(renderer) { }

        public override void DrawBoard(GameBoard board)
        {
            for (int y = 0; y < board.Size; y++)
            {
                for (int x = 0; x < board.Size; x++)
                {
                    var state = board.GetCell(x, y);
                    renderer.RenderCell(x, y, state, board, colorful: true);
                }
            }
        }
    }
}