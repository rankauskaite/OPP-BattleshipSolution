using BattleshipClient.Models;
using System.Drawing;

namespace BattleshipClient.Views.Renderers
{
    public class WinFormsRenderer : IBoardRenderer
    {
        public void RenderCell(int x, int y, CellState state, GameBoard board,
                               bool retro = false, bool highlightPowerUps = false, bool colorful = false)
        {
            int cellSize = board.CellPx;
            var g = board.CreateGraphics();

            Color color = state switch
            {
                CellState.Hit => Color.Red,
                CellState.Miss => Color.LightGray,
                CellState.Whole_ship_down => Color.DarkRed,
                CellState.Ship => highlightPowerUps ? Color.LightGreen : Color.DarkBlue,
                _ => Color.LightBlue
            };

            if (retro)
            {
                color = Color.FromArgb(255, (color.R + 100) % 255, (color.G + 50) % 255, (color.B + 50) % 255);
            }

            if (colorful)
            {
                color = state switch
                {
                    CellState.Empty => Color.FromArgb(180, 215, 255),
                    CellState.Ship => Color.FromArgb(120, 70, 200),
                    CellState.Hit => Color.FromArgb(255, 50, 90),
                    CellState.Miss => Color.FromArgb(255, 250, 190),
                    CellState.Whole_ship_down => Color.FromArgb(100, 40, 130),
                    _ => Color.FromArgb(180, 215, 255),
                };
            }

            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, x * cellSize, y * cellSize, cellSize - 1, cellSize - 1);
        }
    }
}