﻿using System.Text.Json;

namespace BattleshipServer.Models
{
    public class MessageDto
    {
        public string Type { get; set; } = string.Empty;
        public JsonElement Payload { get; set; }
    }

    public class ShipDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Len { get; set; }
        public string Dir { get; set; } = string.Empty; // "H" or "V"
    }

    // --- NPC susiję DTO ---
    public class AddNpcDto
    {
        public string RoomId { get; set; } = "";
        public string Difficulty { get; set; } = "easy"; // "easy" | "hard"
    }

    public class ShotDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class ShotResolvedDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Outcome { get; set; } = "Miss"; // Miss | Hit | Sunk
        public string By { get; set; } = "Human";     // Human | NPC
    }
}
