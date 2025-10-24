using System.Text.Json;

namespace BattleshipClient.Models
{
    public class ShipDto
    {
        public int x { get; set; }
        public int y { get; set; }
        public int len { get; set; }
        public string dir { get; set; } // "H" or "V"
    }
}
