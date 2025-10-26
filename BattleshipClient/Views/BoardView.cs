using BattleshipClient.Views.Renderers;
using BattleshipClient.Models;

namespace BattleshipClient.Views
{
    public abstract class BoardView
    {
        protected readonly IBoardRenderer renderer;

        protected BoardView(IBoardRenderer renderer)
        {
            this.renderer = renderer;
        }

        public abstract void DrawBoard(GameBoard board);
    }
}