using System;
using System.Text.Json;
using System.Threading.Tasks;
using BattleshipServer.Models;

namespace BattleshipServer.Visitor
{
    public class GameMessageValidatorVisitor : IGameMessageVisitor
    {
        public Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player)
        {
            if (!message.Dto.Payload.TryGetProperty("playerName", out var nmElem) ||
                string.IsNullOrEmpty(nmElem.GetString()))
            {
                throw new ArgumentException("Invalid register message: missing or empty playerName");
            }
            return Task.CompletedTask;
        }

        public Task VisitReadyAsync(ReadyMessage message, PlayerConnection player)
        {
            if (!message.Dto.Payload.TryGetProperty("isStandartGame", out var isStandartElem) ||
                (!isStandartElem.ValueKind.Equals(JsonValueKind.True) &&
                 !isStandartElem.ValueKind.Equals(JsonValueKind.False)))
            {
                throw new ArgumentException("Invalid ready message: missing or invalid isStandartGame");
            }

            if (!message.Dto.Payload.TryGetProperty("ships", out var shipsElem) ||
                shipsElem.ValueKind != JsonValueKind.Array)
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
            var payload = message.Dto.Payload;

            if (!payload.TryGetProperty("x", out var xElem) || xElem.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Invalid shot message: missing or invalid x coordinate");

            if (!payload.TryGetProperty("y", out var yElem) || yElem.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Invalid shot message: missing or invalid y coordinate");

            if (xElem.GetInt32() < 0 || xElem.GetInt32() > 10 ||
                yElem.GetInt32() < 0 || yElem.GetInt32() > 10)
                throw new ArgumentException("Invalid shot message: coordinates out of bounds");

            if (!payload.TryGetProperty("doubleBomb", out var doubleBom) ||
                (doubleBom.ValueKind != JsonValueKind.True && doubleBom.ValueKind != JsonValueKind.False))
                throw new ArgumentException("Invalid shot message: missing or invalid doubleBomb flag");

            if (!payload.TryGetProperty("plusShape", out var plusShape) ||
                (plusShape.ValueKind != JsonValueKind.True && plusShape.ValueKind != JsonValueKind.False))
                throw new ArgumentException("Invalid shot message: missing or invalid plusShape flag");

            if (!payload.TryGetProperty("xShape", out var xShape) ||
                (xShape.ValueKind != JsonValueKind.True && xShape.ValueKind != JsonValueKind.False))
                throw new ArgumentException("Invalid shot message: missing or invalid xShape flag");

            if (!payload.TryGetProperty("superDamage", out var superDamage) ||
                (superDamage.ValueKind != JsonValueKind.True && superDamage.ValueKind != JsonValueKind.False))
                throw new ArgumentException("Invalid shot message: missing or invalid superDamage flag");

            return Task.CompletedTask;
        }

        public Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player)
        {
            var payload = message.Dto.Payload;

            if (!payload.TryGetProperty("isStandartGame", out var isStandartElem) ||
                (!isStandartElem.ValueKind.Equals(JsonValueKind.True) &&
                 !isStandartElem.ValueKind.Equals(JsonValueKind.False)))
            {
                throw new ArgumentException("Invalid play bot message: missing or invalid isStandartGame");
            }
            if (!payload.TryGetProperty("ships", out var shipsElem) ||
                shipsElem.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Invalid play bot message: missing or invalid ships array");
            }

            return Task.CompletedTask;
        }

        public Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player)
        {
            var payload = message.Dto.Payload;

            if (!payload.TryGetProperty("x", out var xElem) || xElem.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Invalid place shield message: missing or invalid x coordinate");

            if (!payload.TryGetProperty("y", out var yElem) || yElem.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Invalid place shield message: missing or invalid y coordinate");

            if (xElem.GetInt32() < 0 || xElem.GetInt32() > 10 ||
                yElem.GetInt32() < 0 || yElem.GetInt32() > 10)
                throw new ArgumentException("Invalid place shield message: coordinates out of bounds");

            if (!payload.TryGetProperty("placeShield", out var placeShield) ||
                string.IsNullOrEmpty(placeShield.GetString()))
                throw new ArgumentException("Invalid place shield message: missing or invalid placeShield value");

            if (placeShield.GetString() != "safetiness" && placeShield.GetString() != "visibility")
                throw new ArgumentException("Invalid place shield message: placeShield must be either 'safetiness' or 'visibility'");

            if (!payload.TryGetProperty("isArea", out var isAreaElem) ||
                (isAreaElem.ValueKind != JsonValueKind.True && isAreaElem.ValueKind != JsonValueKind.False))
                throw new ArgumentException("Invalid place shield message: missing or invalid isArea flag");

            return Task.CompletedTask;
        }

        // NAUJAS – HEAL
        public Task VisitHealShipAsync(HealShipMessage message, PlayerConnection player)
        {
            var payload = message.Dto.Payload;

            if (!payload.TryGetProperty("cells", out var cellsElem) ||
                cellsElem.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Invalid healShip message: missing or invalid 'cells' array");

            foreach (var cell in cellsElem.EnumerateArray())
            {
                if (!cell.TryGetProperty("x", out var xElem) || xElem.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException("Invalid healShip message: cell missing or invalid x");

                if (!cell.TryGetProperty("y", out var yElem) || yElem.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException("Invalid healShip message: cell missing or invalid y");

                int x = xElem.GetInt32();
                int y = yElem.GetInt32();

                if (x < 0 || x > 9 || y < 0 || y > 9)
                    throw new ArgumentException("Invalid healShip message: cell coordinates out of bounds");
            }

            return Task.CompletedTask;
        }
    }
}