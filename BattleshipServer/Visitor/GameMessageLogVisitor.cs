namespace BattleshipServer.Visitor
{
    public class GameMessageLogVisitor : IGameMessageVisitor
    {
        public GameMessageLogVisitor() { }

        public Task VisitCopyGameAsync(CopyGameMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Copy game message received.");
            return Task.CompletedTask;
        }

        public Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Place shield message received.");
            return Task.CompletedTask;

        }

        public Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Play bot message received.");
            return Task.CompletedTask;
        }

        public Task VisitReadyAsync(ReadyMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Ready message received.");
            return Task.CompletedTask;
        }

        public Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Register message received.");
            return Task.CompletedTask;
        }

        public Task VisitShotAsync(ShotMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Shot message received.");
            return Task.CompletedTask;
        }

        public Task VisitUseGameCopyAsync(UseGameCopyMessage message, PlayerConnection player)
        {
            Console.WriteLine("[Log] Use game copy message received.");
            return Task.CompletedTask;
        }

        public Task VisitHealShipAsync(HealShipMessage message, PlayerConnection player)
        {
            Console.WriteLine($"[LOG] healShip from {player.Name}: {message.Dto.Payload}");
            return Task.CompletedTask;
        }
    }
}
