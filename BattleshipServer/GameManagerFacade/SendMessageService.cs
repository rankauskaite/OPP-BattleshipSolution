using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer.GameManagerFacade
{
    public class SendMessageService
    {
        public async Task SendRegisterMessage(PlayerConnection player)
        {
            var payload = JsonSerializer.SerializeToElement(new {message = "registered"});
            await player.SendAsync(new MessageDto { Type = "register", Payload = payload });
        }

        public async Task SendGameCopyInfo(PlayerConnection player, string message)
        {
            var payload = JsonSerializer.SerializeToElement(new { message = message });
            await player.SendAsync(new MessageDto { Type = "info", Payload = payload });
        }

        public async Task SendShipInfo(PlayerConnection player)
        {
            var payload = JsonSerializer.SerializeToElement(new
            {
                message = $"No copied game found for player {player.Name}."
            });
            await player.SendAsync(new MessageDto { Type = "shipInfo", Payload = payload });
        }

        public async Task SendShipInfo(PlayerConnection player, List<ShipDto> ships)
        {
            var payload = JsonSerializer.SerializeToElement(new
            {
                message = $"Restoring game for player {player.Name} from copy...",
                ships
            });
            await player.SendAsync(new MessageDto { Type = "shipInfo", Payload = payload });
        }
    }
}
