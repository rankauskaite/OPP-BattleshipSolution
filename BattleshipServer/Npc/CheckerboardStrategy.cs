using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public sealed class CheckerboardStrategy : INpcShotStrategy
    {
        public (int x, int y) ChooseTarget(BoardKnowledge k)
        {
            var cand = k.UnshotCells().Where(c => ((c.X + c.Y) & 1) == 0).ToList();
            if (cand.Count == 0) cand = k.UnshotCells().ToList();
            var pick = cand[Random.Shared.Next(cand.Count)];
            return (pick.X, pick.Y);
        }
    }
}
