using System.Text.Json;

namespace BattleshipClient.Models
{
    public class MessageDto
    {
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }

    public class ShipDto
    {
        public int x { get; set; }
        public int y { get; set; }
        public int len { get; set; }
        public string dir { get; set; } // "H" or "V"
    }
}
