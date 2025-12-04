using BattleshipServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipServer.Visitor
{
    internal interface GameMessage
    {
        public abstract Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player);
    }

    public class RegisterGameMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public RegisterGameMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitRegisterAsync(this, player);
        }
    }

    public class CopyGameMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public CopyGameMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitCopyGameAsync(this, player);
        }
    }

    public class UseGameCopyMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public UseGameCopyMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitUseGameCopyAsync(this, player);
        }
    }


    public class ReadyMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public ReadyMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitReadyAsync(this, player);
        }
    }

    public class ShotMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public ShotMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitShotAsync(this, player);
        }
    }

    public class PlayBotMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public PlayBotMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitPlayBotAsync(this, player);
        }
    }

    public class PlaceShieldMessage : GameMessage
    {
        public MessageDto Dto { get; }

        public PlaceShieldMessage(MessageDto dto)
        {
            Dto = dto;
        }

        public async Task AcceptAsync(IGameMessageVisitor visitor, PlayerConnection player)
        {
            await visitor.VisitPlaceShieldAsync(this, player);
        }
    }
}
