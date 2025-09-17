using System;
using System.Drawing;
using System.Windows.Forms;

namespace BattleshipClient
{
    [Serializable]
    public class ShipData
    {
        public Guid Id { get; set; }
        public int Length { get; set; }
        public bool Horizontal { get; set; }
    }

    public class ShipPreviewControl : Control
    {
        public int Length { get; private set; }
        public Guid Id { get; private set; }
        public bool Horizontal { get; set; } = true;

        public ShipPreviewControl(int length)
        {
            Length = length;
            Id = Guid.NewGuid();
            this.Tag = Id;
            this.Width = length * 30;
            this.Height = 30;
            this.Margin = new Padding(6);
            this.BackColor = Color.Gray;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            for (int i = 0; i < Length; i++)
            {
                var rect = Horizontal
                    ? new Rectangle(i * 30, 0, 28, 28)
                    : new Rectangle(0, i * 30, 28, 28);
                using var b = new SolidBrush(ColorTranslator.FromHtml("#6c757d"));
                g.FillRectangle(b, rect);
                g.DrawRectangle(Pens.Black, rect);
            }
        }
    }
}
