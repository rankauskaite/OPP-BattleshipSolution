using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public sealed class NpcController
    {
        private readonly INpcShotStrategy _shot;

        public NpcController(INpcShotStrategy shot) => _shot = shot;

        public (int x, int y) Decide(BoardKnowledge known) => _shot.ChooseTarget(known);
    }
}
