using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipServer.Visitor
{
    public interface IGameMessageVisitor
    {
        Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player);
        Task VisitReadyAsync(ReadyMessage message, PlayerConnection player);
        Task VisitCopyGameAsync(CopyGameMessage message, PlayerConnection player);
        Task VisitUseGameCopyAsync(UseGameCopyMessage message, PlayerConnection player);
        Task VisitShotAsync(ShotMessage message, PlayerConnection player);
        Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player);
        Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player);
    }
}
