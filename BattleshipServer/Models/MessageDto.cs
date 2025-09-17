using System.Text.Json;

namespace BattleshipServer.Models
{
    public class MessageDto
    {
        // Pradinis value arba required
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
}
