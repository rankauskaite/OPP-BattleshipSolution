using BattleshipServer.Data;
using BattleshipServer.Models;
using BattleshipServer.Npc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks; 
using BattleshipServer.Defense;


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
                    // ČIA įjungiame random gynybą su Area + Cell shields
                    //DefenseSetup.SetupRandomDefense(game);

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
                int x = xe.GetInt32();
                int y = ye.GetInt32();
                Game? game = manager.GetPlayersGame(player.Id);
                if (game != null)
                {
                    Dictionary<string, bool> powerUps = messageDtoService.GetPowerups(dto);
                    powerUps.TryGetValue("doubleBomb", out bool isDoubleBomb);
                    powerUps.TryGetValue("plusShape", out bool plusShape);
                    powerUps.TryGetValue("xShape", out bool xShape);
                    powerUps.TryGetValue("superDamage", out bool superDamage);
                    if (plusShape || xShape || superDamage )
                    {
                        await game.ProcessCompositeShot(player.Id, x, y, isDoubleBomb, plusShape, xShape, superDamage);
                    }
                    else
                    {
                        await game.ProcessShot(player.Id, x, y, isDoubleBomb);
                    }
                }
                (Game? game, IBotPlayerController? bot) botGame = manager.GetBotGame(player.Id);
                if(botGame.bot != null)
                {
                    await botGame.bot.MaybePlayAsync();
                }
            }
        } 

        public async Task HandlePlaceShield(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            if (dto.Payload.ValueKind != JsonValueKind.Object)
                return;

            if (!dto.Payload.TryGetProperty("x", out var xe) ||
                !dto.Payload.TryGetProperty("y", out var ye))
                return;

            int x = xe.GetInt32();
            int y = ye.GetInt32();

            string modeStr = "safetiness";
            if (dto.Payload.TryGetProperty("mode", out var me) && me.ValueKind == JsonValueKind.String)
            {
                modeStr = me.GetString() ?? "safetiness";
            }

            // PVP žaidimas, kuriame žaidžia šis player'io Id
            Game? game = manager.GetPlayersGame(player.Id);
            if (game == null)
                return;

            // Jei tai NPC žaidimas – iš viso ignoruojam (nenorim skydo prieš botą)
            var botGame = manager.GetBotGame(player.Id);
            if (botGame.bot != null)
                return;

            DefenseMode mode = modeStr == "visibility"
                ? DefenseMode.Visibility
                : DefenseMode.Safetiness;

            // Composite: pridedam vieno langelio skydą
            game.AddCellShield(player.Id, x, y, mode);

            // Optional: informacinė žinutė klientui
            var payload = JsonSerializer.SerializeToElement(new
            {
                message = $"Placed {mode} shield at {x},{y}"
            });
            await player.SendAsync(new MessageDto { Type = "info", Payload = payload });
        }


        public void HandlePlayBot(GameManager manager, PlayerConnection player, MessageDto dto, Database db)
        {
            bool isStandart = messageDtoService.GetIsStandartGame(dto);
            List<ShipDto> ships = messageDtoService.GetShipsFromDto(dto);
            botService.CreateBotGame(manager, player, isStandart, db, ships);
        }
    }
}
