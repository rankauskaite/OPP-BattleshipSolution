using System;
using System.Linq;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public interface IStrategySelector
    {
        INpcShotStrategy Pick(BoardKnowledge k, INpcShotStrategy? current);
    }

    /// <summary>
    /// Paprastas taisyklėmis grįstas parinkėjas:
    /// 1) Jei yra taikinių prie pataikymų (frontier) -> "hunt-target".
    /// 2) Jei žaidimo pradžia / dar daug nežinomų -> "checkerboard".
    /// 3) Kitaip -> "human-like".
    /// 4) Jei kažkas netinka -> "random".
    /// </summary>
    public sealed class RuleBasedSelector : IStrategySelector
    {
        public INpcShotStrategy Pick(BoardKnowledge k, INpcShotStrategy? current)
        {
            // 1) Yra taikinių aplink pataikymus? Tuomet TARGET fazė.
            bool hasFrontier = k.HitFrontier4().Any();
            if (hasFrontier)
                return ShotStrategyFactory.Create("hunt-target");

            // 2) Kiek liko nešaudytų? Jei daug — checkerboard.
            var unshotCount = k.UnshotCells().Count();
            if (unshotCount > 60)
                return ShotStrategyFactory.Create("checkerboard");

            // 3) Vidurio/pabaigos žaidimas — labiau "žmogiška" paieška (linijų tąsa + heatmap)
            return ShotStrategyFactory.Create("human-like");
        }
    }
}
