using System;
using System.Threading.Tasks;
using BattleshipServer.Models;

namespace BattleshipServer.MessageHandling
{
    // NOTE: chain length in GameManager.BuildMessageChain() is >= 4 (actually 9).

    public sealed class RegisterMessageHandler : MessageHandlerBase
    {
        public RegisterMessageHandler(GameManager manager) : base(manager) { }

        public override async Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "register")
            {
                await Manager.Facade.RegisterPlayerAsync(Manager, player, dto);
                await Manager.TryPairPlayersAsync();
                return;
            }
            await Next(player, dto);
        }
    }

    public sealed class ReadyMessageHandler : MessageHandlerBase
    {
        public ReadyMessageHandler(GameManager manager) : base(manager) { }

        public override async Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "ready")
            {
                await Manager.Facade.MarkPlayerAsReady(Manager, player, dto);
                return;
            }
            await Next(player, dto);
        }
    }

    public sealed class CopyGameMessageHandler : MessageHandlerBase
    {
        public CopyGameMessageHandler(GameManager manager) : base(manager) { }

        public override async Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "copyGame")
            {
                await Manager.Facade.CopyGame(Manager, player);
                return;
            }
            await Next(player, dto);
        }
    }

    public sealed class UseGameCopyMessageHandler : MessageHandlerBase
    {
        public UseGameCopyMessageHandler(GameManager manager) : base(manager) { }

        public override async Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "useGameCopy")
            {
                await Manager.Facade.UseGameCopy(Manager, player);
                return;
            }
            await Next(player, dto);
        }
    }

    public sealed class ShotMessageHandler : MessageHandlerBase
    {
        public ShotMessageHandler(GameManager manager) : base(manager) { }

        public override async Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "shot")
            {
                await Manager.Facade.HandleShot(Manager, player, dto);
                return;
            }
            await Next(player, dto);
        }
    }

    public sealed class PlayBotMessageHandler : MessageHandlerBase
    {
        public PlayBotMessageHandler(GameManager manager) : base(manager) { }

        public override Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "playBot")
            {
                // This method is sync in the facade
                Manager.Facade.HandlePlayBot(Manager, player, dto, Manager.Db);
                return Task.CompletedTask;
            }
            return Next(player, dto);
        }
    }

    public sealed class PlaceShieldMessageHandler : MessageHandlerBase
    {
        public PlaceShieldMessageHandler(GameManager manager) : base(manager) { }

        public override async Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            if (dto.Type == "placeShield")
            {
                await Manager.Facade.HandlePlaceShield(Manager, player, dto);
                return;
            }
            await Next(player, dto);
        }
    }

    public sealed class BenchMessageHandler : MessageHandlerBase
    {
        public BenchMessageHandler(GameManager manager) : base(manager) { }

        public override Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            // Benchmark no-op to avoid spamming console and affecting performance results.
            if (dto.Type == "bench")
                return Task.CompletedTask;

            return Next(player, dto);
        }
    }

    public sealed class UnknownMessageHandler : MessageHandlerBase
    {
        public UnknownMessageHandler(GameManager manager) : base(manager) { }

        public override Task HandleAsync(PlayerConnection player, MessageDto dto)
        {
            Console.WriteLine($"[Manager] Unknown message type: {dto.Type}");
            return Task.CompletedTask;
        }
    }
}
