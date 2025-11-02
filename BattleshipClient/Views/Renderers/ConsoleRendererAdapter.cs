using BattleshipClient.Models;

namespace BattleshipClient.Views.Renderers
{
    public sealed class ConsoleRendererAdapter : IBoardRenderer
    {
        private readonly IAsciiConsole _console;
        private bool _headerPrinted;

        public ConsoleRendererAdapter(IAsciiConsole console)
        {
            _console = console;
        }

        public void RenderCell(int x, int y, CellState state, GameBoard board,
                               bool retro = false, bool highlightPowerUps = false, bool colorful = false)
        {
            if (!_headerPrinted)
            {
                _headerPrinted = true;
                _console.WriteText(0, 0, $"ASCII Board {board.Size}x{board.Size}");
            }

            // žemėlapis į simbolius
            char ch = state switch
            {
                CellState.Empty           => '.',
                CellState.Ship            => highlightPowerUps ? 'P' : 'O',
                CellState.Hit             => 'X',
                CellState.Miss            => '~',
                CellState.Whole_ship_down => '#',
                _                         => '?'
            };

            if (retro) ch = char.ToLower(ch);

            _console.Put(x, y + 1, ch);
        }
    }
}
