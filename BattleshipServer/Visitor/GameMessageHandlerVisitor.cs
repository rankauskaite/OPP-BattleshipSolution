using System.Threading.Tasks;
using BattleshipServer.Data;
using BattleshipServer.GameManagerFacade;
using BattleshipServer.Models;

namespace BattleshipServer.Visitor
{
    public class GameMessageHandlerVisitor : IGameMessageVisitor
    {
        private readonly GameManager _manager;
        private readonly GameManagerFacade.GameManagerFacade gameManagerFacade = new GameManagerFacade.GameManagerFacade();
        private readonly Database _db;

        public GameMessageHandlerVisitor(GameManager manager, Database db)
        {
            _manager = manager;
            _db = db;
        }

        public async Task VisitRegisterAsync(RegisterGameMessage message, PlayerConnection player)
        {
            await gameManagerFacade.RegisterPlayerAsync(_manager, player, message.Dto);
            _manager.TryPairPlayers();
        }

        public async Task VisitReadyAsync(ReadyMessage message, PlayerConnection player)
        {
            await gameManagerFacade.MarkPlayerAsReady(_manager, player, message.Dto);
        }

        public async Task VisitCopyGameAsync(CopyGameMessage message, PlayerConnection player)
        {
            await gameManagerFacade.CopyGame(_manager, player);
        }

        public async Task VisitUseGameCopyAsync(UseGameCopyMessage message, PlayerConnection player)
        {
            await gameManagerFacade.UseGameCopy(_manager, player);
        }

        public async Task VisitShotAsync(ShotMessage message, PlayerConnection player)
        {
            await gameManagerFacade.HandleShot(_manager, player, message.Dto);
        }

        public async Task VisitPlayBotAsync(PlayBotMessage message, PlayerConnection player)
        {
            gameManagerFacade.HandlePlayBot(_manager, player, message.Dto, _db);
        }

        public async Task VisitPlaceShieldAsync(PlaceShieldMessage message, PlayerConnection player)
        {
            await gameManagerFacade.HandlePlaceShield(_manager, player, message.Dto);
        }

        public async Task VisitHealShipAsync(HealShipMessage message, PlayerConnection player)
        {
            await gameManagerFacade.HandleHealShip(_manager, player, message.Dto);
        }
    }
}