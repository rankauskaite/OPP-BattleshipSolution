using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient.Flyweight
{
    internal class CellFlyweightFactory
    {
        private static readonly Dictionary<BoardStyle, CellFlyweight> _flyweights = new Dictionary<BoardStyle, CellFlyweight>();
        public static CellFlyweight Get(BoardStyle style)
        {
            if (_flyweights.ContainsKey(style))
                return _flyweights[style];

            var fw = Create(style);
            _flyweights[style] = fw;
            return fw;
        }

        private static CellFlyweight Create(BoardStyle style)
        {
            return style switch
            {
                BoardStyle.Retro => new CellFlyweight(
                    backgroundColor: Color.FromArgb(245, 245, 235),
                    pen: Pens.DimGray,
                    colors: new Dictionary<CellState, Color>
                    {
                        [CellState.Empty] = Color.FromArgb(210, 230, 240),
                        [CellState.Ship] = Color.FromArgb(120, 120, 130),
                        [CellState.Hit] = Color.FromArgb(230, 90, 70),
                        [CellState.Miss] = Color.FromArgb(255, 255, 250),
                        [CellState.Whole_ship_down] = Color.FromArgb(100, 40, 50)
                    }),

                BoardStyle.PowerUp => new CellFlyweight(
                    backgroundColor: Color.FromArgb(245, 250, 245),
                    pen: Pens.Black,
                    colors: new Dictionary<CellState, Color>
                    {
                        [CellState.Empty] = Color.FromArgb(215, 235, 215),
                        [CellState.Ship] = Color.FromArgb(40, 160, 80),
                        [CellState.Hit] = Color.FromArgb(230, 50, 50),
                        [CellState.Miss] = Color.White,
                        [CellState.Whole_ship_down] = Color.FromArgb(100, 20, 30)
                    }),

                BoardStyle.Colorful => new CellFlyweight(
                    backgroundColor: Color.FromArgb(240, 245, 255),
                    pen: new Pen(Color.FromArgb(70, 70, 120)),
                    colors: new Dictionary<CellState, Color>
                    {
                        [CellState.Empty] = Color.FromArgb(180, 215, 255),
                        [CellState.Ship] = Color.FromArgb(120, 70, 200),
                        [CellState.Hit] = Color.FromArgb(255, 50, 90),
                        [CellState.Miss] = Color.FromArgb(255, 250, 190),
                        [CellState.Whole_ship_down] = Color.FromArgb(100, 40, 130)
                    }),

                _ => new CellFlyweight(
                    backgroundColor: ColorTranslator.FromHtml("#f8f9fa"),
                    pen: Pens.Black,
                    colors: new Dictionary<CellState, Color>
                    {
                        [CellState.Empty] = ColorTranslator.FromHtml("#dbe9f7"),
                        [CellState.Ship] = ColorTranslator.FromHtml("#6c757d"),
                        [CellState.Hit] = ColorTranslator.FromHtml("#dc3545"),
                        [CellState.Miss] = Color.White,
                        [CellState.Whole_ship_down] = ColorTranslator.FromHtml("#781D26")
                    })
            };
        }
    }
}
