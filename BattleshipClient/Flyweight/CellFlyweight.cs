using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient.Flyweight
{
    internal class CellFlyweight
    {
        public Color BackgroundColor { get;  }
        public Pen Pen { get; }
        private readonly Dictionary<CellState, Color> _colors;

        public CellFlyweight(Color backgroundColor, Pen pen, Dictionary<CellState, Color> colors)
        {
            BackgroundColor = backgroundColor;
            Pen = pen;
            _colors = colors;
        }
        public Color GetCellColor(CellState state) =>
        _colors.TryGetValue(state, out var c) ? c : Color.LightGray;
    }
}
