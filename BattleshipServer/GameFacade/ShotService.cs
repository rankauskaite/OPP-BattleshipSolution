using BattleshipServer.Data;
using BattleshipServer.Domain;
using BattleshipServer.PowerUps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BattleshipServer.Game;

namespace BattleshipServer.GameFacade
{
    public class ShotService
    {
        private readonly PlayerService playerService;
        private readonly SendMessageService messageService;
        public bool lastShootHit { get; private set; } = false;
        public bool gameOver { get; set; } = false;

        public ShotService(SendMessageService sendMessageService, PlayerService playerService)
        {
            this.playerService = playerService;
            this.messageService = sendMessageService;
        }

        public async void ProcessShot(Game game, Guid shooterId, int x, int y, bool isDoubleBomb)
        {
            var shooter = playerService.GetPlayer(shooterId, game);
            var target = playerService.GetOpponent(shooterId, game);
            if (x < 0 || x >= 10 || y < 0 || y >= 10)
            {
                await messageService.SendErrorAsync(shooter, "Invalid coords");
                return;
            }

            (int[,] targetBoard, List<Game.Ship> targetShips) = playerService.GetTargetBoardAndShips(target, game);

            bool hit = false;
            bool success = false;

            if (isDoubleBomb)
            {
                int[] doubleBombNextCoors = GetDoubleBombCoords(targetBoard, x, y);
                int x1 = doubleBombNextCoors[0];
                int y1 = doubleBombNextCoors[1];
                if (doubleBombNextCoors.Length == 2 && x1 >= 0 && y1 >= 0)
                {
                    (success, hit) = ProcessShot(x1, y1, targetBoard);
                    if (!success)
                    {
                        await messageService.SendErrorAsync(shooter, "Cell already shot");
                        return;
                    }
                    await messageService.SendShotInfo(game.Player1, game.Player2, shooterId, target, x1, y1, hit);

                    bool anyLeftAfterFirst = false;
                    foreach (var s in targetShips)
                    {
                        if (!s.IsSunk(targetBoard))
                        {
                            anyLeftAfterFirst = true;
                            break;
                        }
                    }
                    if (!anyLeftAfterFirst)
                    {
                        game.SetIsGameOver(true);
                        await messageService.SendGameOverAsync(game.Player1, game.Player2, shooterId);
                        game.SaveGameToDB(shooterId);
                        return;
                    }
                }
            }

            (success, hit) = ProcessShot(x, y, targetBoard);
            this.lastShootHit = hit;
            if (!success)
            {
                await messageService.SendErrorAsync(shooter, "Cell already shot");
                return;
            }
        }

        public async void ProcessCompositeShot(Game game, Guid shooterId, int x0, int y0, bool isDoubleBomb, bool plusShape, bool xShape, bool superDamage)
        {
            if (!plusShape && !xShape && !superDamage)
            {
                ProcessShot(game, shooterId, x0, y0, isDoubleBomb);
                return;
            }
            var shooter = playerService.GetPlayer(shooterId, game);
            var target = playerService.GetOpponent(shooterId, game);
            (int[,] board, List<Game.Ship> ships) = playerService.GetTargetBoardAndShips(target, game);

            IShotPattern patt = new SingleCellPattern();
            if (plusShape) patt = new PlusPatternDecorator(patt);
            if (xShape) patt = new XPatternDecorator(patt);
            var cells = patt.GetCells(x0, y0, 10, 10).Distinct().ToList();

            IShotEffect effect = superDamage ? new SuperDamageEffect() : new NoopEffect();

            lastShootHit = false;
            var sunkThisTurn = new HashSet<Game.Ship>(); // kad nekartotume „whole_ship_down“

            foreach (var (x, y) in cells)
            {
                var (success, hit) = ProcessShot(x, y, board);
                if (!success) continue;
                // 1) VISADA pirma nusiunčiam "hit"/"miss"
                await messageService.SendShotInfo(game.Player1, game.Player2, shooterId, target, x, y, hit);
                
                if (hit)
                {
                    lastShootHit = true;
                    // 2) taikom efektą (superDamage pažymės laivą nuskendusiu)
                    _ = effect.AfterCellHit(x, y, board, ships);

                    // 3) surandam laivą, kurį palietėm
                    var hitShip = ships.FirstOrDefault(s =>
                        (s.Horizontal && y == s.Y && x >= s.X && x < s.X + s.Len) ||
                        (!s.Horizontal && x == s.X && y >= s.Y && y < s.Y + s.Len));

                    // 4) jei dabar laivas nuskendęs -> išsiunčiam whole_ship_down visoms jo ląstelėms (kartą)
                    if (hitShip != null && !sunkThisTurn.Contains(hitShip) && hitShip.IsSunk(board))
                    {
                        sunkThisTurn.Add(hitShip);
                        for (int i = 0; i < hitShip.Len; i++)
                        {
                            int cx = hitShip.X + (hitShip.Horizontal ? i : 0);
                            int cy = hitShip.Y + (hitShip.Horizontal ? 0 : i);

                            await messageService.SendWholeShipDown(game.Player1, game.Player2, shooterId, target, cx, cy);
                        }
                    }
                }

                // 5) ar liko laivų?
                bool anyLeft = ships.Any(s => !s.IsSunk(board));
                if (!anyLeft)
                {
                    game.SetIsGameOver(true);
                    await messageService.SendGameOverAsync(game.Player1, game.Player2, shooterId);
                    game.SaveGameToDB(shooterId);
                    return;
                }
            }

            if (!lastShootHit)
            {
                game.SetCurrentPlayer(target.Id);
            }

            await messageService.SendTurnMessage(game.Player1, game.Player2, game.CurrentPlayerId);
        }

        private int[] GetDoubleBombCoords(int[,] targetBoard, int x, int y)
        {
            int[] res = new int[4];
            List<int[]> possible_moves = new List<int[]>();

            if (y > 0 && (targetBoard[y - 1, x] == 0 || targetBoard[y - 1, x] == 1))
            {
                // second bomb drop is above current shot
                possible_moves.Add([x, y - 1,]);
            }

            if (y < targetBoard.GetLength(0) - 1 && (targetBoard[y + 1, x] == 0 || targetBoard[y + 1, x] == 1))
            {
                // second bobm drop is below current shot
                possible_moves.Add([x, y + 1]);
            }

            if (x > 0 && (targetBoard[y, x - 1] == 0 || targetBoard[y, x - 1] == 1))
            {
                // second bomb drop is to the left of the current shot
                possible_moves.Add([x - 1, y]);
            }

            if (x < targetBoard.GetLength(1) - 1)
            {
                // second bomb drop is to the right of the current shot
                possible_moves.Add([x + 1, y]);
            }

            if (possible_moves.Count == 0)
            {
                return [-1, -1];
            }
            if (possible_moves.Count == 1)
            {
                return possible_moves[0];
            }
            Random rnd = new Random();
            int idx = rnd.Next(0, possible_moves.Count);
            return possible_moves[idx];
        }

        private (bool, bool) ProcessShot(int x, int y, int[,] targetBoard)
        {
            bool success = true;
            bool hit = false;
            if (targetBoard[y, x] == 1)
            {
                targetBoard[y, x] = 3; // hit
                hit = true;
            }
            else if (targetBoard[y, x] == 0)
            {
                targetBoard[y, x] = 2; // miss
                hit = false;
            }
            else
            {
                // already shot here
                success = false;
            }

            return (success, hit);
        }
    }
}
