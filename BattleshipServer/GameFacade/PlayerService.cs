using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BattleshipServer.Game;

namespace BattleshipServer.GameFacade
{
    public class PlayerService
    {
        public PlayerConnection GetPlayer(Guid id, Game game) => id == game.Player1.Id ? game.Player1 : game.Player2;
        public PlayerConnection GetOpponent(Guid id, Game game) => id == game.Player1.Id ? game.Player2 : game.Player1;
        public (int[,], List<Ship>) GetTargetBoardAndShips(PlayerConnection target, Game game) => target == game.Player1 ? game.GetBoard1AndShips() : game.GetBoard2AndShips();
        public string GetPLayerName(PlayerConnection player) => player.Name ?? player.Id.ToString();

    }
}
