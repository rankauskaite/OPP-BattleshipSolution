using BattleshipServer.Data;
using BattleshipServer.Models;
using BattleshipServer.Npc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleshipServer.GameManagerFacade
{
    public class GameManagerFacade
    {
        private readonly SendMessageService messageService;
        private readonly MessageDtoService messageDtoService;
        private readonly BotService botService;

        public GameManagerFacade()
        {
            messageService = new SendMessageService();
            messageDtoService = new MessageDtoService();
            botService = new BotService();
        }

        public async Task RegisterPlayerAsync(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            if (dto.Payload.TryGetProperty("playerName", out var nmElem))
            {
                player.Name = nmElem.GetString();
            }

            manager.AddToWaitingQueue(player);
            Console.WriteLine($"[Manager] Player registered: {player.Name} ({player.Id})");
            await messageService.SendRegisterMessage(player);
        }

        public async Task MarkPlayerAsReady(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            Game? game = manager.GetPlayersGame(player.Id);
            if (game != null)
            {
                bool isStandartGame = messageDtoService.GetIsStandartGame(dto);
                game.SetGameMode(player.Id, isStandartGame);
                List<ShipDto> ships = messageDtoService.GetShipsFromDto(dto);
                game.PlaceShips(player.Id, ships);
                Console.WriteLine($"[Manager] Player {player.Name} placed {ships.Count} ships.");
                if (game.IsReady && game.GameModesMatch)
                {
                    await game.StartGame();
                }
                else if (!game.GameModesMatch)
                {
                    Console.WriteLine("Game mode of players do not match! Try again");
                }
            }
            else
            {
                Console.WriteLine("[Manager] Ready received but player not in a game yet.");
            }
        }

        public async Task CopyGame(GameManager manager, PlayerConnection player)
        {
            var payload = JsonSerializer.SerializeToElement(new { message = "No game to save" });
            Game? game = manager.GetPlayersGame(player.Id);
            if(game != null)
            {
                Console.WriteLine($"[Manager] Copying game for player {player.Name}...");
                manager.StoreGameCopy(player.Id, game.Clone());
            }
            string message = game != null ? "Game copied successfully" : "No game to save";
            await messageService.SendGameCopyInfo(player, message);
        }

        public async Task UseGameCopy(GameManager manager, PlayerConnection player)
        {
            var gameCopy = manager.GetCopiedGame(player.Id);
            if(gameCopy != null)
            {
                List<ShipDto> ships = gameCopy.GetPlayerShips(player.Id);
                await messageService.SendShipInfo(player, ships);
            }
            else
            {
                await messageService.SendShipInfo(player);
            }
        }

        public async Task HandleShot(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            if (dto.Payload.TryGetProperty("x", out var xe) && dto.Payload.TryGetProperty("y", out var ye))
            {
                bool isDoubleBomb = messageDtoService.GetIsDoubleBomb(dto);
                int x = xe.GetInt32();
                int y = ye.GetInt32();
                // NEW: power-up flag'ai
                dto.Payload.TryGetProperty("plusShape", out var plusEl);
                dto.Payload.TryGetProperty("xShape", out var xEl);
                dto.Payload.TryGetProperty("superDamage", out var superEl);
                bool plusShape = plusEl.ValueKind == JsonValueKind.True;
                bool xShape = xEl.ValueKind == JsonValueKind.True;
                bool superDamage = superEl.ValueKind == JsonValueKind.True;

                Game? game = manager.GetPlayersGame(player.Id);
                if (game != null)
                {
                    if (plusShape || xShape || superDamage)
                    {
                        // power-up režimas
                        await g.ProcessCompositeShot(player.Id, x, y, isDoubleBomb, plusShape, xShape, superDamage);
                    }
                    else
                    {
                        // senas vieno taško (arba doubleBomb) režimas
                        await g.ProcessShot(player.Id, x, y, isDoubleBomb);
                    }
                }
                (Game? game, BotOrchestrator? bot) botGame = manager.GetBotGame(player.Id);
                if(botGame.bot != null)
                {
                    await botGame.bot.MaybePlayAsync();
                }
            }
        }

        public void HandlePlayBot(GameManager manager, PlayerConnection player, MessageDto dto, Database db)
        {
            bool isStandart = messageDtoService.GetIsStandartGame(dto);
            List<ShipDto> ships = messageDtoService.GetShipsFromDto(dto);
            botService.CreateBotGame(manager, player, isStandart, db, ships);
        }
    }
}
