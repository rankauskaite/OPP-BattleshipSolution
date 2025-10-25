using BattleshipServer.Builders;
using BattleshipServer.Data;
using BattleshipServer.Domain;
using BattleshipServer.Models;
using BattleshipServer.Npc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipServer.GameManagerFacade
{
    public class BotService
    {
        public async void CreateBotGame(GameManager manager, PlayerConnection player, bool isStandart, Database db, List<ShipDto> ships)
        {
            // 1) Bot žaidėjas su NoopWebSocket
            var botSocket = new NoopWebSocket();
            var bot = new PlayerConnection(botSocket, manager) { Name = "Robot" };

            // 2) Pasirenkam konkretų builder'į
            IGameSetupBuilder builder = isStandart
                ? new StandardGameBuilder()
                : new MiniGameBuilder();

            // 3) „surenkam“ žaidimą (fluent seka)
            var game = builder
                .CreateShell(player, bot, manager, db)
                .ConfigureBoard()
                .ConfigureFleets(ships, opponentRandom: true)
                .ConfigureNpc(g =>
                {
                    var selector = new RuleBasedSelector();
                    return new BotOrchestrator(g, bot.Id, selector, "checkerboard");
                })
                .Build();

            // 4) Registracija ir BotOrchestrator užkabinimas GameManager'io žemėlapyje
            var orch = builder.Orchestrator!;
            manager.AddGame(game, player.Id, orch);
            
            // 5) Start
            await game.StartGame();

            Console.WriteLine($"[Manager] Player {player.Name} started BOT game (builder).");
        }
    }
}
