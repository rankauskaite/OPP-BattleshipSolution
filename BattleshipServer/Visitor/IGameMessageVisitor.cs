using BattleshipServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipServer.Visitor
{
    public interface IGameMessageVisitor
    {
        Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player);
        Task VisitReadyAsync(ReadyMessage message, PlayerConnection player);
        Task VisitCopyGameAsync(CopyGameMessage message, PlayerConnection player);
        Task VisitUseGameCopyAsync(UseGameCopyMessage message, PlayerConnection player);
        Task VisitShotAsync(ShotMessage message, PlayerConnection player);
        Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player);
        Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player);
    }

    public class GameMessageHandlerVisitor : IGameMessageVisitor
    {
        private readonly GameManager _manager;
        private readonly GameManagerFacade.GameManagerFacade gameManagerFacade = new GameManagerFacade.GameManagerFacade();
        private readonly Database _db;

        public GameMessageHandlerVisitor(GameManager manager, Database db)
        {
            _manager = manager;
            _db = db;
        }
        public async Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player)
        {
            await gameManagerFacade.RegisterPlayerAsync(_manager, player, message.Dto);
            _manager.TryPairPlayers();
        }
        public async Task VisitReadyAsync(ReadyMessage message, PlayerConnection player)
        {
            await gameManagerFacade.MarkPlayerAsReady(_manager, player, message.Dto);
        }
        public async Task VisitCopyGameAsync(CopyGameMessage message, PlayerConnection player)
        {
            await gameManagerFacade.CopyGame(_manager, player);
        }
        public async Task VisitUseGameCopyAsync(UseGameCopyMessage message, PlayerConnection player)
        {
            await gameManagerFacade.UseGameCopy(_manager, player);
        }
        public async Task VisitShotAsync(ShotMessage message, PlayerConnection player)
        {
            await gameManagerFacade.HandleShot(_manager, player, message.Dto);
        }
        public async Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player)
        {
            gameManagerFacade.HandlePlayBot(_manager, player, message.Dto, _db);
        }
        public async Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player)
        {
            await gameManagerFacade.HandlePlaceShield(_manager, player, message.Dto);
        }
    }

    public class GameMessageValidatorVisitor : IGameMessageVisitor
    {
        public Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player)
        {
            if (!message.Dto.Payload.TryGetProperty("playerName", out var nmElem) || string.IsNullOrEmpty(nmElem.GetString()))
            {
                throw new ArgumentException("Invalid register message: missing or empty playerName");
            }
            return Task.CompletedTask;
        }
        public Task VisitReadyAsync(ReadyMessage message, PlayerConnection player)
        {
            if(!message.Dto.Payload.TryGetProperty("isStandartGame", out var isStandartElem) || !isStandartElem.ValueKind.Equals(System.Text.Json.JsonValueKind.True) && !isStandartElem.ValueKind.Equals(System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid ready message: missing or invalid isStandartGame");
            }
            if(!message.Dto.Payload.TryGetProperty("ships", out var shipsElem) || shipsElem.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                throw new ArgumentException("Invalid ready message: missing or invalid ships array");
            }
            return Task.CompletedTask;
        }
        public Task VisitCopyGameAsync(CopyGameMessage message, PlayerConnection player)
        {
            return Task.CompletedTask;
        }
        public Task VisitUseGameCopyAsync(UseGameCopyMessage message, PlayerConnection player)
        {
            return Task.CompletedTask;
        }
        public Task VisitShotAsync(ShotMessage message, PlayerConnection player)
        {
            if(!message.Dto.Payload.TryGetProperty("x", out var xElem) || xElem.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                throw new ArgumentException("Invalid shot message: missing or invalid x coordinate");
            }
            if(!message.Dto.Payload.TryGetProperty("y", out var yElem) || yElem.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                throw new ArgumentException("Invalid shot message: missing or invalid y coordinate");
            }
            if (xElem.GetInt32() < 0 || xElem.GetInt32() > 10 || yElem.GetInt32() < 0 || yElem.GetInt32() > 10)
            {
                throw new ArgumentException("Invalid shot message: coordinates out of bounds");
            }
            if(!message.Dto.Payload.TryGetProperty("doubleBomb", out var doubleBom) || (doubleBom.ValueKind != System.Text.Json.JsonValueKind.True && doubleBom.ValueKind != System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid shot message: missing or invalid doubleBomb flag");
            }
            if (!message.Dto.Payload.TryGetProperty("plusShape", out var plusShape) || (plusShape.ValueKind != System.Text.Json.JsonValueKind.True && plusShape.ValueKind != System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid shot message: missing or invalid plusShape flag");
            }
            if (!message.Dto.Payload.TryGetProperty("xShape", out var xShape) || (xShape.ValueKind != System.Text.Json.JsonValueKind.True && xShape.ValueKind != System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid shot message: missing or invalid xShape flag");
            }
            if (!message.Dto.Payload.TryGetProperty("superDamage", out var superDamage) || (superDamage.ValueKind != System.Text.Json.JsonValueKind.True && superDamage.ValueKind != System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid shot message: missing or invalid superDamage flag");
            }
            return Task.CompletedTask;
        }
        public Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player)
        {
            if (!message.Dto.Payload.TryGetProperty("isStandartGame", out var isStandartElem) || !isStandartElem.ValueKind.Equals(System.Text.Json.JsonValueKind.True) && !isStandartElem.ValueKind.Equals(System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid play bot message: missing or invalid isStandartGame");
            }
            if (!message.Dto.Payload.TryGetProperty("ships", out var shipsElem) || shipsElem.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                throw new ArgumentException("Invalid play bot message: missing or invalid ships array");
            }

            return Task.CompletedTask;
        }
        public Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player)
        {
            if (!message.Dto.Payload.TryGetProperty("x", out var xElem) || xElem.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                throw new ArgumentException("Invalid place shield message: missing or invalid x coordinate");
            }
            if (!message.Dto.Payload.TryGetProperty("y", out var yElem) || yElem.ValueKind != System.Text.Json.JsonValueKind.Number)
            {
                throw new ArgumentException("Invalid place shield message: missing or invalid y coordinate");
            }
            if (xElem.GetInt32() < 0 || xElem.GetInt32() > 10 || yElem.GetInt32() < 0 || yElem.GetInt32() > 10)
            {
                throw new ArgumentException("Invalid place shield message: coordinates out of bounds");
            }

            if(!message.Dto.Payload.TryGetProperty("placeShield", out var placeShield) || string.IsNullOrEmpty(placeShield.GetString()))
            {
                throw new ArgumentException("Invalid place shield message: missing or invalid placeShield value");
            }
            if(placeShield.GetString() != "safetiness" && placeShield.GetString() != "visibility")
            {
                throw new ArgumentException("Invalid place shield message: placeShield must be either 'safetiness' or 'visibility'");
            }

            if (!message.Dto.Payload.TryGetProperty("isArea", out var isAreaElem) || (isAreaElem.ValueKind != System.Text.Json.JsonValueKind.True && isAreaElem.ValueKind != System.Text.Json.JsonValueKind.False))
            {
                throw new ArgumentException("Invalid place shield message: missing or invalid isArea flag");
            }

            return Task.CompletedTask;
        }
    }
}
