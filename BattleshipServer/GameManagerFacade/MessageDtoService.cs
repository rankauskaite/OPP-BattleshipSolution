using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer.GameManagerFacade
{
    public class MessageDtoService
    {
        public List<ShipDto> GetShipsFromDto(MessageDto dto)
        {
            var ships = new List<ShipDto>();
            if (dto.Payload.TryGetProperty("ships", out var shEl))
            {
                foreach (var el in shEl.EnumerateArray())
                {
                    ships.Add(new ShipDto
                    {
                        X = el.GetProperty("x").GetInt32(),
                        Y = el.GetProperty("y").GetInt32(),
                        Len = el.GetProperty("len").GetInt32(),
                        Dir = el.GetProperty("dir").GetString()
                    });
                }
            }
            return ships;
        }

        private bool GetBool(MessageDto dto, string attrName, bool defaultValue)
        {
            if (dto.Payload.TryGetProperty(attrName, out var val) && (val.ValueKind == JsonValueKind.True || val.ValueKind == JsonValueKind.False))
                return val.GetBoolean();
            return defaultValue;
        }

        public bool GetIsStandartGame(MessageDto dto)
        {
            return GetBool(dto, "isStandartGame", true);
        }

        public bool GetIsDoubleBomb(MessageDto dto)
        {
            return GetBool(dto, "doubleBomb", false);
        }

        public Dictionary<string, bool> GetPowerups(MessageDto dto)
        {
            Dictionary<string, bool> powerups = new Dictionary<string, bool>();
            powerups["doubleBomb"] = GetIsDoubleBomb(dto);
            // NEW: power-up flag'ai
            dto.Payload.TryGetProperty("plusShape", out var plusEl);
            dto.Payload.TryGetProperty("xShape", out var xEl);
            dto.Payload.TryGetProperty("superDamage", out var superEl);
            powerups["plusShape"] = plusEl.ValueKind == JsonValueKind.True;
            powerups["xShape"] = xEl.ValueKind == JsonValueKind.True;
            powerups["superDamage"] = superEl.ValueKind == JsonValueKind.True;
            return powerups;
        }
    }
}
