using BattleshipClient.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using BattleshipClient.Views;
using BattleshipClient.Views.Renderers;
using BattleshipClient.Flyweight;

namespace BattleshipClient
{
    public enum CellState { Empty, Ship, Miss, Hit, Whole_ship_down }

    // --- Pridėti lentos režimai (Bridge stiliaus „abstrakcijos“ idėja) ---
    public enum BoardStyle
    {
        Classic,
        Retro,
        PowerUp,
        Colorful,
        Console
    }

    public class GameBoard : Control
    {
        private BoardView _consoleView;
        public int Size { get; private set; } = 10;
        public CellState[,] Cells;
        public int CellPx { get; set; } = 36;
        public int LabelMargin { get; set; } = 25;

        public bool ShowSunkOutlines { get; set; } = true;
        public BoardStyle Style { get; private set; } = BoardStyle.Classic;

        public event EventHandler<Point> CellClicked;
        public event Action<ShipData, Point> ShipDropped;
        public List<ShipDto> Ships { get; set; } = new List<ShipDto>();

        public GameBoard()
        {
            InitializeBoard(BoardStyle.Classic);
        }

        public GameBoard(int size, BoardStyle style = BoardStyle.Classic)
        {
            Size = size;
            InitializeBoard(style);
        }

        private void InitializeBoard(BoardStyle style)
        {
            Style = style;
            DoubleBuffered = true;
            AllowDrop = true;
            DragEnter += GameBoard_DragEnter;
            DragDrop += GameBoard_DragDrop;
            Width = CellPx * Size + LabelMargin + 1;
            Height = CellPx * Size + LabelMargin + 1;
            Cells = new CellState[Size, Size];
        }

        public void SetStyle(BoardStyle style)
        {
            Style = style;

            if (Style == BoardStyle.Console && _consoleView == null)
                _consoleView = new ConsoleBoardView();

            Invalidate();
        }

        public void SetCell(int x, int y, CellState state)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            Cells[y, x] = state;
            Invalidate(CellRectPx(x, y));
        }

        public CellState GetCell(int x, int y)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return CellState.Empty;
            return Cells[y, x];
        }

        public void ClearBoard()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    Cells[r, c] = CellState.Empty;
            Invalidate();
        }

        private void GameBoard_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(ShipData)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void GameBoard_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(ShipData))) return;
            var ship = (ShipData)e.Data.GetData(typeof(ShipData));
            Point client = PointToClient(new Point(e.X, e.Y));
            int c = (client.X - LabelMargin) / CellPx;
            int r = (client.Y - LabelMargin) / CellPx;
            ShipDropped?.Invoke(ship, new Point(c, r));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var fw = CellFlyweightFactory.Get(Style);
            g.Clear(fw.BackgroundColor);

            using var font = new Font("Arial", 10, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // Raidės
            for (int c = 0; c < Size; c++)
            {
                string letter = ((char)('A' + c)).ToString();
                var rect = new Rectangle(LabelMargin + c * CellPx, 0, CellPx, LabelMargin);
                g.DrawString(letter, font, brush, rect, sf);
            }

            // Skaičiai
            for (int r = 0; r < Size; r++)
            {
                string number = (r + 1).ToString();
                var rect = new Rectangle(0, LabelMargin + r * CellPx, LabelMargin, CellPx);
                g.DrawString(number, font, brush, rect, sf);
            }

            // Langeliai
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    var rect = CellRectPx(c, r);
                    using (var b = new SolidBrush(fw.GetCellColor(Cells[r, c])))
                        g.FillRectangle(b, rect);

                    g.DrawRectangle(fw.Pen, rect);
                }
            }

            if (ShowSunkOutlines) DrawSunkShipPerimeter(g);

            using var pen = new Pen(Color.Black, 3);
            foreach (var ship in Ships)
            {
                int x = LabelMargin + ship.x * CellPx;
                int y = LabelMargin + ship.y * CellPx;
                int w = (ship.dir == "H" ? ship.len : 1) * CellPx;
                int h = (ship.dir == "V" ? ship.len : 1) * CellPx;
                g.DrawRectangle(pen, x, y, w, h);
            }

            // --- Papildomas piešimas į konsolę ---
            if (Style == BoardStyle.Console)
            {
                try
                {
                    if (_consoleView == null) _consoleView = new ConsoleBoardView();
                    _consoleView.DrawBoard(this);
                }
                catch { }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int c = (e.X - LabelMargin) / CellPx;
            int r = (e.Y - LabelMargin) / CellPx;
            if (c >= 0 && c < Size && r >= 0 && r < Size)
                CellClicked?.Invoke(this, new Point(c, r));
        }

        private Rectangle CellRectPx(int x, int y)
            => new Rectangle(LabelMargin + x * CellPx, LabelMargin + y * CellPx, CellPx, CellPx);

        private void DrawSunkShipPerimeter(Graphics g)
        {
            using var pen = new Pen(Color.Black, 3f) { Alignment = PenAlignment.Inset };

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (Cells[y, x] != CellState.Whole_ship_down) continue;

                    var r = CellRectPx(x, y);
                    int L = r.Left, T = r.Top, R = r.Right - 1, B = r.Bottom - 1;

                    if (y == 0 || Cells[y - 1, x] != CellState.Whole_ship_down) g.DrawLine(pen, L, T, R, T);
                    if (y == Size - 1 || Cells[y + 1, x] != CellState.Whole_ship_down) g.DrawLine(pen, L, B, R, B);
                    if (x == 0 || Cells[y, x - 1] != CellState.Whole_ship_down) g.DrawLine(pen, L, T, L, B);
                    if (x == Size - 1 || Cells[y, x + 1] != CellState.Whole_ship_down) g.DrawLine(pen, R, T, R, B);
                }
            }
        }
    }
}