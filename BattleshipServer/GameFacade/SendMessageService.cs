using BattleshipServer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer.GameFacade
{
    public class SendMessageService
    {
        public async Task SendErrorAsync(PlayerConnection player, string message)
        {
            await player.SendAsync(new Models.MessageDto { Type = "error", Payload = JsonSerializer.SerializeToElement(new { message }) });
        }

        public async Task SendShotInfo(PlayerConnection player1, PlayerConnection player2, Guid shooterId, PlayerConnection target, int x, int y, bool hit)
        {
            var shotResult = JsonSerializer.SerializeToElement(new { x, y, result = hit ? "hit" : "miss", shooterId = shooterId.ToString(), targetId = target.Id.ToString() });
            await player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });
            await player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });
        }

        public async Task SendShotInfo(PlayerConnection player1, PlayerConnection player2, Guid shooterId, PlayerConnection target, int x, int y, bool hit, bool wholeDown)
        {
            var shotResult = JsonSerializer.SerializeToElement(new { x, y, result = hit && !wholeDown ? "hit" : hit && wholeDown ? "whole_ship_down" : "miss", shooterId = shooterId.ToString(), targetId = target.Id.ToString() });
            await player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });
            await player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = shotResult });
        }

        public async Task SendGameOverAsync(PlayerConnection player1, PlayerConnection player2, Guid winnerId)
        {
            var goPayload = JsonSerializer.SerializeToElement(new { winnerId = winnerId.ToString() });
            await player1.SendAsync(new Models.MessageDto { Type = "gameOver", Payload = goPayload });
            await player2.SendAsync(new Models.MessageDto { Type = "gameOver", Payload = goPayload });
        }

        public async Task SendWholeShipDown(PlayerConnection player1, PlayerConnection player2, Guid shooterId, PlayerConnection target, int cx1, int cy1)
        {
            var updateBoard = JsonSerializer.SerializeToElement(new
            {
                x = cx1,
                y = cy1,
                result = "whole_ship_down",
                shooterId = shooterId.ToString(),
                targetId = target.Id.ToString()
            });
            await player1.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = updateBoard });
            await player2.SendAsync(new Models.MessageDto { Type = "shotResult", Payload = updateBoard });
        }

        public async Task SendTurnMessage(PlayerConnection player1, PlayerConnection player2, Guid currentPlayerId)
        {
            var turnPayload = JsonSerializer.SerializeToElement(new { current = currentPlayerId.ToString() });
            await player1.SendAsync(new Models.MessageDto { Type = "turn", Payload = turnPayload });
            await player2.SendAsync(new Models.MessageDto { Type = "turn", Payload = turnPayload });
        }
    }
}
