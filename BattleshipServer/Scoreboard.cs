using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer
{
    public class Scoreboard
    {
        private static Scoreboard _instance;
        private static readonly object _lock = new();

        public string Player1Name { get; private set; } = "P1";
        public string Player2Name { get; private set; } = "P2";
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

        public void SetPlayers(string name1, string name2)
        {
            Player1Name = name1;
            Player2Name = name2;
        }

        public async Task AddHit(Guid shooterId, Game game)
        {
            if (shooterId == game.Player1.Id)
                Player1Hits++;
            else
                Player2Hits++;

            await Broadcast(game);
        }

        public async Task AddWin(Guid winnerId, Game game)
        {
            if (winnerId == game.Player1.Id)
                Player1Wins++;
            else
                Player2Wins++;

            await Broadcast(game);
        }

        private async Task Broadcast(Game game)
        {
            var payload = JsonSerializer.SerializeToElement(new
            {
                type = "scoreUpdate",
                p1 = Player1Name,
                p2 = Player2Name,
                hits1 = Player1Hits,
                hits2 = Player2Hits,
                wins1 = Player1Wins,
                wins2 = Player2Wins
            });

            var msg = new Models.MessageDto { Type = "scoreUpdate", Payload = payload };

            await game.Player1.SendAsync(msg);
            await game.Player2.SendAsync(msg);

            Console.WriteLine($"[Scoreboard] {Player1Name} ({Player1Hits}/{Player1Wins}) vs {Player2Name} ({Player2Hits}/{Player2Wins})");
        }

        public void Reset()
        {
            Player1Hits = Player2Hits = Player1Wins = Player2Wins = 0;
        }
    }
}
