﻿using System.Text.Json;

namespace BattleshipClient.Models
{
    public class MessageDto
    {
        public string Type { get; set; } = string.Empty;
        public JsonElement Payload { get; set; }
    }

    public class ShipDto
    {
        public int x { get; set; }
        public int y { get; set; }
        public int len { get; set; }
        public string dir { get; set; } // "H" or "V"
    }

    // --- NPC susiję DTO ---
    public class AddNpcDto
    {
        public string RoomId { get; set; } = "";
        public string Difficulty { get; set; } = "easy";
    }

    public class ShotResolvedDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Outcome { get; set; } = "Miss";
        public string By { get; set; } = "Human";
    }
}
