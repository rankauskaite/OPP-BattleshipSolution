namespace BattleshipServer.Domain
{
    public sealed class Player
    {
        public PlayerConnection Conn { get; }
        public string Name => Conn.Name ?? "Player";
        public System.Guid Id => Conn.Id;

        public Board Board { get; } = new Board();

        public Player(PlayerConnection connection)
        {
            Conn = connection;
        }
    }
}
