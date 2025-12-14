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

            // --- bandome traktuoti kaip reconnect, jei toks žaidimas jau yra ---
            Game? existing = manager.GetGameByPlayerName(player.Name);
            if (existing != null && !existing.GetIsGameOver())
            {
                Console.WriteLine($"[Reconnect] Player {player.Name} reconnected.");

                existing.ReplacePlayerConnection(player);
                manager.RegisterExistingConnection(player, existing);

                var snapshot = manager.GetCopiedGameByPlayerName(player.Name);
                if (snapshot != null)
                {
                    existing.RestoreMemento(snapshot);
                }

                var opponentConn = existing.Player1.Name == player.Name
                    ? existing.Player2
                    : existing.Player1;

                List<ShipDto> ships = existing.GetPlayerShips(player.Id);

                bool isP1 = existing.Player1.Name == player.Name;

                int healUsed = isP1 ? existing.HealUsedP1 : existing.HealUsedP2;
                int safeShieldsUsed = isP1 ? existing.SafeShieldsUsedP1 : existing.SafeShieldsUsedP2;
                int invisibleShieldsUsed = isP1 ? existing.InvisibleShieldsUsedP1 : existing.InvisibleShieldsUsedP2;
                int plusUsed = isP1 ? existing.PlusUsedP1 : existing.PlusUsedP2;
                int xUsed = isP1 ? existing.XUsedP1 : existing.XUsedP2;
                int superUsed = isP1 ? existing.SuperUsedP1 : existing.SuperUsedP2;
                int doubleBombUsed = isP1 ? existing.DoubleBombUsedP1 : existing.DoubleBombUsedP2;

                var restorePayload = JsonSerializer.SerializeToElement(new
                {
                    yourId = player.Id.ToString(),
                    yourName = player.Name,
                    opponentId = opponentConn.Id.ToString(),
                    opponentName = opponentConn.Name,
                    current = existing.CurrentPlayerId.ToString(),
                    isGameOver = existing.GetIsGameOver(),
                    isStandard = existing.IsStandardForPlayer(player.Id),
                    boardSelf = existing.GetBoardForPlayerAsJagged(player.Id),
                    boardEnemy = existing.GetEnemyBoardViewForPlayerAsJagged(player.Id),
                    ships = ships.Select(s => new { x = s.X, y = s.Y, len = s.Len, dir = s.Dir }).ToArray(),
                    powerUps = new
                    {
                        healUsed,
                        safeShieldsUsed,
                        invisibleShieldsUsed,
                        plusUsed,
                        xUsed,
                        superUsed,
                        doubleBombUsed
                    }
                });

                await player.SendAsync(new MessageDto
                {
                    Type = "restoreState",
                    Payload = restorePayload
                });

                return;
            }

            // --- naujas žaidėjas ---
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
                    manager.StoreLatestSnapshotForGame(game);
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
            Game? game = manager.GetPlayersGame(player.Id);
            if (game != null)
            {
                Console.WriteLine($"[Manager] Copying full game state for player {player.Name}...");
                var memento = game.CreateMemento();
                manager.StoreGameCopy(player, memento);
            }

            string message = game != null ? "Game copied successfully" : "No game to save";
            await messageService.SendGameCopyInfo(player, message);
        }

        public async Task UseGameCopy(GameManager manager, PlayerConnection player)
        {
            Game? game = manager.GetPlayersGame(player.Id);
            var memento = manager.GetCopiedGameByPlayerName(player.Name);

            if (game != null && memento != null)
            {
                game.RestoreMemento(memento);

                List<ShipDto> ships = game.GetPlayerShips(player.Id);
                await messageService.SendShipInfo(player, ships);

                var infoPayload = JsonSerializer.SerializeToElement(new
                {
                    message = "Game state restored from saved snapshot."
                });
                await player.SendAsync(new MessageDto { Type = "info", Payload = infoPayload });
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

            // Pirma – įvykdom šūvį
            await validate.HandleAsync(manager, player, dto);

            // Po to – atnaujinam powerup skaitiklius ir darom autosave
            var game = manager.GetPlayersGame(player.Id);
            if (game != null)
            {
                if (dto.Payload.ValueKind == JsonValueKind.Object)
                {
                    bool isDoubleBomb = dto.Payload.TryGetProperty("doubleBomb", out var db) && db.ValueKind == JsonValueKind.True;
                    bool plusShape = dto.Payload.TryGetProperty("plusShape", out var ps) && ps.ValueKind == JsonValueKind.True;
                    bool xShape = dto.Payload.TryGetProperty("xShape", out var xs) && xs.ValueKind == JsonValueKind.True;
                    bool superDamage = dto.Payload.TryGetProperty("superDamage", out var sd) && sd.ValueKind == JsonValueKind.True;

                    if (isDoubleBomb || plusShape || xShape || superDamage)
                    {
                        game.RegisterPowerUpUse(player.Id, isDoubleBomb, plusShape, xShape, superDamage);
                    }
                }

                manager.StoreLatestSnapshotForGame(game);
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
            else if (dto.Payload.TryGetProperty("placeShield", out var pe) && pe.ValueKind == JsonValueKind.String)
            {
                // suderinam su kliento property pavadinimu
                modeStr = pe.GetString() ?? "safetiness";
            }

            bool isArea = false;
            if (dto.Payload.TryGetProperty("isArea", out var ae) &&
                (ae.ValueKind == JsonValueKind.True || ae.ValueKind == JsonValueKind.False))
            {
                isArea = ae.GetBoolean();
            }

            Game? game = manager.GetPlayersGame(player.Id);
            if (game == null)
                return;

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

            manager.StoreLatestSnapshotForGame(game);
        }

        public async Task HandleHealShip(GameManager manager, PlayerConnection player, MessageDto dto)
        {
            var game = manager.GetPlayersGame(player.Id);
            if (game == null)
                return;

            var payload = dto.Payload;

            var cells = new List<(int x, int y)>();
            foreach (var cell in payload.GetProperty("cells").EnumerateArray())
            {
                int x = cell.GetProperty("x").GetInt32();
                int y = cell.GetProperty("y").GetInt32();
                cells.Add((x, y));
            }

            if (cells.Count == 0)
                return;

            var first = cells[0];

            var healedCells = game.HealShip(player.Id, first.x, first.y);

            if (healedCells.Count == 0)
            {
                return;
            }

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

            await game.Player1.SendAsync(response);
            await game.Player2.SendAsync(response);

            manager.StoreLatestSnapshotForGame(game);
        }

        public void HandlePlayBot(GameManager manager, PlayerConnection player, MessageDto dto, Database db)
        {
            bool isStandart = messageDtoService.GetIsStandartGame(dto);
            List<ShipDto> ships = messageDtoService.GetShipsFromDto(dto);
            botService.CreateBotGame(manager, player, isStandart, db, ships);
        }
    }
}