using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public sealed class RandomShotStrategy : INpcShotStrategy
    {
        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var cells = k.UnshotCells().ToList();
            var pick = cells[Random.Shared.Next(cells.Count)];
            return (pick.X, pick.Y);
        }
    }
}
