using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public interface INpcShotStrategy
    {
        (int x, int y) ChooseTarget(BoardKnowledge known);
    }
}
