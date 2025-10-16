namespace BattleshipServer.Domain
{
    public readonly struct Coordinate
    {
        public int X { get; }
        public int Y { get; }

        public Coordinate(int x, int y)
        {
            X = x; Y = y;
        }

        public void Deconstruct(out int x, out int y) { x = X; y = Y; }
        public override string ToString() => $"({X},{Y})";
    }
}
