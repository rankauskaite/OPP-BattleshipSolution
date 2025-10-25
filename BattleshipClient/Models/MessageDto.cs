using System.Text.Json;

namespace BattleshipClient.Models
{
    public class MessageDto
    {
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }
}
