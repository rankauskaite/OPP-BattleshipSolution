using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipClient.Flyweight
{
    [MemoryDiagnoser]
    public class FlyweightBenchmark
    {
        private Bitmap _bitmap;
        private Graphics _graphics;
        private GameBoard _board;

        [GlobalSetup]
        public void Setup()
        {
            _bitmap = new Bitmap(800, 800, PixelFormat.Format32bppArgb);
            _graphics = Graphics.FromImage(_bitmap);

            _board = new GameBoard(10, BoardStyle.Classic);
        }

        [Benchmark]
        public void DrawBoard()
        {
            _board.DrawBoard(_graphics);
        }

        [Benchmark]
        public void DrawBoardOldWay()
        {
            _board.OldDrawBoard(_graphics);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _graphics.Dispose();
            _bitmap.Dispose();
        }
    }
}
