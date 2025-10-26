using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer
{
    /// <summary>
    /// Globalus Singleton Scoreboard
    /// Kaupia bendrą statistiką tarp visų žaidimų
    /// </summary>
    public sealed class Scoreboard
    {
        private static readonly Lazy<Scoreboard> _lazyInstance =
            new(() => new Scoreboard());

        public static Scoreboard Instance => _lazyInstance.Value;

        // Laikome visų žaidėjų statistiką
        private readonly ConcurrentDictionary<string, PlayerStats> _playerStats = new();

        private Scoreboard() { }

        public record PlayerStats(string Name, int Hits, int Wins);

        public async Task RegisterPlayers(string name1, string name2, Game game)
        {
            _playerStats.TryAdd(name1, new PlayerStats(name1, 0, 0));
            _playerStats.TryAdd(name2, new PlayerStats(name2, 0, 0));

            await Broadcast(game);
        }

        public async Task AddHit(Guid shooterId, Game game)
        {
            string shooterName = shooterId == game.Player1.Id
                ? game.Player1.Name
                : game.Player2.Name;

            _playerStats.AddOrUpdate(shooterName,
                new PlayerStats(shooterName, 1, 0),
                (_, s) => s with { Hits = s.Hits + 1 });

            await Broadcast(game);
        }

        public async Task AddWin(Guid winnerId, Game game)
        {
            string winnerName = winnerId == game.Player1.Id
                ? game.Player1.Name
                : game.Player2.Name;

            _playerStats.AddOrUpdate(winnerName,
                new PlayerStats(winnerName, 0, 1),
                (_, s) => s with { Wins = s.Wins + 1 });

            await Broadcast(game);
        }

        private async Task Broadcast(Game game)
        {
            var p1 = game.Player1.Name;
            var p2 = game.Player2.Name;

            _playerStats.TryGetValue(p1, out var s1);
            _playerStats.TryGetValue(p2, out var s2);

            var payload = JsonSerializer.SerializeToElement(new
            {
                type = "scoreUpdate",
                p1 = p1,
                p2 = p2,
                hits1 = s1?.Hits ?? 0,
                hits2 = s2?.Hits ?? 0,
                wins1 = s1?.Wins ?? 0,
                wins2 = s2?.Wins ?? 0
            });

            var msg = new Models.MessageDto
            {
                Type = "scoreUpdate",
                Payload = payload
            };

            await game.Player1.SendAsync(msg);
            await game.Player2.SendAsync(msg);

            Console.WriteLine($"[Scoreboard] {p1} ({s1?.Hits}/{s1?.Wins}) vs {p2} ({s2?.Hits}/{s2?.Wins})");
        }

        public void ResetAll()
        {
            _playerStats.Clear();
        }
    }
}
