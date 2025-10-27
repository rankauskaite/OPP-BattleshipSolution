using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;
using System.Drawing;

namespace BattleshipClient.Views
{
    public class PowerUpBoardView : BoardView
    {
        public PowerUpBoardView(IBoardRenderer renderer) : base(renderer) { }

        public override void DrawBoard(GameBoard board)
        {
            for (int y = 0; y < board.Size; y++)
            {
                for (int x = 0; x < board.Size; x++)
                {
                    var state = board.GetCell(x, y);
                    renderer.RenderCell(x, y, state, board, highlightPowerUps: true);
                }
            }
        }
    }
}