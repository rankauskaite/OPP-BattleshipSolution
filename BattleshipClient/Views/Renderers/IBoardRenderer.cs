using BattleshipClient.Models;

namespace BattleshipClient.Views.Renderers
{
    public interface IBoardRenderer
    {
        void RenderCell(int x, int y, CellState state, GameBoard board,
                        bool retro = false, bool highlightPowerUps = false, bool colorful = false);
    }
}