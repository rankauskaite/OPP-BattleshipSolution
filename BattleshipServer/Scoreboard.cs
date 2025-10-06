using System;

namespace BattleshipServer
{
    public class Scoreboard
    {
        private static Scoreboard _instance;
        private static readonly object _lock = new();

        public int Player1Hits { get; private set; }
        public int Player2Hits { get; private set; }
        public int Player1Wins { get; private set; }
        public int Player2Wins { get; private set; }

        private Scoreboard() { }

        public static Scoreboard Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new Scoreboard();
                    return _instance;
                }
            }
        }

        public void AddHit(Guid playerId, Guid p1Id, Guid p2Id)
        {
            if (playerId == p1Id)
                Player1Hits++;
            else if (playerId == p2Id)
                Player2Hits++;

            Display();
        }

        public void AddWin(Guid playerId, Guid p1Id, Guid p2Id)
        {
            if (playerId == p1Id)
                Player1Wins++;
            else if (playerId == p2Id)
                Player2Wins++;

            Display();
        }

        public void Reset()
        {
            Player1Hits = Player2Hits = Player1Wins = Player2Wins = 0;
        }

        public void Display()
        {
            Console.WriteLine($"--- SCOREBOARD ---");
            Console.WriteLine($"P1: {Player1Hits} hits, {Player1Wins} wins");
            Console.WriteLine($"P2: {Player2Hits} hits, {Player2Wins} wins");
            Console.WriteLine($"------------------");
        }
    }
}
