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
using BattleshipServer.State;
using BattleshipServer.ChainOfResponsibility;


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
            var validate = new ValidateCoordinatesHandler();
            var retrieve = new GameRetrievalHandler();
            var process = new ShotProcessingHandler(messageDtoService);
            var bot = new BotTriggerHandler();

            validate.SetNext(retrieve);
            retrieve.SetNext(process);
            process.SetNext(bot);

            await validate.HandleAsync(manager, player, dto);
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

            bool isArea = false;
            if (dto.Payload.TryGetProperty("isArea", out var ae) &&
                (ae.ValueKind == JsonValueKind.True || ae.ValueKind == JsonValueKind.False))
            {
                isArea = ae.GetBoolean();
            }

            // PVP žaidimas, kuriame žaidžia šis player'io Id
            Game? game = manager.GetPlayersGame(player.Id);
            if (game == null)
                return;

            // Jei tai NPC žaidimas – ignoruojam
            var botGame = manager.GetBotGame(player.Id);
            if (botGame.bot != null)
                return;

            DefenseMode mode = modeStr == "visibility"
                ? DefenseMode.Visibility
                : DefenseMode.Safetiness;

            if (isArea)
            {
                int x1 = Math.Max(0, x - 1);
                int y1 = Math.Max(0, y - 1);
                int x2 = Math.Min(9, x + 1);
                int y2 = Math.Min(9, y + 1);

                // Zona visada dedama kaip tikras Composite: viduje bus 9 Leaf (CellShield).
                game.AddAreaShield(player.Id, x1, y1, x2, y2, mode);
            }
            else
            {
                game.AddCellShield(player.Id, x, y, mode);
            }



            var payload = JsonSerializer.SerializeToElement(new
            {
                message = isArea
                    ? $"Placed {mode} AREA shield around {x},{y}"
                    : $"Placed {mode} shield at {x},{y}"
            });
            await player.SendAsync(new MessageDto { Type = "info", Payload = payload });
        }

        public async Task HandleHealShip(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            var game = manager.GetPlayersGame(player.Id);
            if (game == null)
                return;

            var payload = dto.Payload;

            // Perskaitom langelių sąrašą iš žinutės
            var cells = new List<(int x, int y)>();
            foreach (var cell in payload.GetProperty("cells").EnumerateArray())
            {
                int x = cell.GetProperty("x").GetInt32();
                int y = cell.GetProperty("y").GetInt32();
                cells.Add((x, y));
            }

            if (cells.Count == 0)
                return;

            // Naudojam pirmą langelį kaip atskaitos tašką – pagal jį randam laivą
            var first = cells[0];

            var healedCells = game.HealShip(player.Id, first.x, first.y);

            if (healedCells.Count == 0)
            {
                // nieko neišgydė (laivas nenusautas, nuskendęs ar pan.) – nieko nesiunčiam
                return;
            }

            // Paruošiam atsakymo payload – kuriuos langelius reikia atnaujinti klientams
            var responsePayload = JsonSerializer.SerializeToElement(new
            {
                healedPlayerId = player.Id.ToString(),
                cells = healedCells
                    .Select(c => new { x = c.x, y = c.y })
                    .ToArray()
            });

            var response = new MessageDto
            {
                Type = "healApplied",
                Payload = responsePayload
            };

            // Išsiunčiam abiem žaidėjams – abu turi atsinaujinti lentas
            await game.Player1.SendAsync(response);
            await game.Player2.SendAsync(response);
        }

        public void HandlePlayBot(GameManager manager, PlayerConnection player, MessageDto dto, Database db)
        {
            bool isStandart = messageDtoService.GetIsStandartGame(dto);
            List<ShipDto> ships = messageDtoService.GetShipsFromDto(dto);
            botService.CreateBotGame(manager, player, isStandart, db, ships);
        }
    }
}
