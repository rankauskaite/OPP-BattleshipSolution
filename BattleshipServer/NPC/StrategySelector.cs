using System;
using System.Linq;
using System.Collections.Generic;
using BattleshipServer.Models;

namespace BattleshipServer.Npc
{
    public interface IStrategySelector
    {
        // Parenka, kuri strategija turi galioti pagal žaidimo būseną.
        INpcShotStrategy Pick(BoardKnowledge k, INpcShotStrategy? current);

        // Gražina pradinę strategiją pagal raktą (pvz., "checkerboard", "hunt-target"...).
        INpcShotStrategy Resolve(string key);
    }

    /// <summary>
    /// Visiškai autonominis parinkėjas:
    /// 1) Jei yra taikinių prie pataikymų (frontier) -> "hunt-target".
    /// 2) Jei žaidimo pradžia / dar daug nežinomų -> "checkerboard".
    /// 3) Kitaip -> "human-like".
    /// 4) Jei kažkas netinka -> "random".
    /// </summary>
    public sealed class RuleBasedSelector : IStrategySelector
    {
        // Vienkartiniai instancai (nebesisukam per fabriką).
        private readonly INpcShotStrategy _random       = new RandomShotStrategy();
        private readonly INpcShotStrategy _checkerboard = new CheckerboardStrategy();
        private readonly INpcShotStrategy _huntTarget   = new HuntTargetStrategy();
        private readonly INpcShotStrategy _humanLike    = new HumanLikeFrontierHeatStrategy();

        private readonly Dictionary<string, INpcShotStrategy> _byKey;

        public RuleBasedSelector()
        {
            _byKey = new(StringComparer.OrdinalIgnoreCase)
            {
                ["random"]       = _random,
                ["checkerboard"] = _checkerboard,
                ["hunt-target"]  = _huntTarget,
                ["human-like"]   = _humanLike,
            };
        }

        public INpcShotStrategy Resolve(string key)
            => _byKey.TryGetValue(key, out var s) ? s : _random;

        public INpcShotStrategy Pick(BoardKnowledge k, INpcShotStrategy? current)
        {
            // 1) Yra taikinių aplink pataikymus? Tuomet TARGET fazė.
            bool hasFrontier = k.HitFrontier4().Any();
            if (hasFrontier)
                return _huntTarget;

            // 2) Kiek liko nešaudytų? Jei daug — checkerboard.
            int unshotCount = k.UnshotCells().Count();
            if (unshotCount > 60)
                return _checkerboard;

            // 3) Vidurio/pabaigos žaidimas — labiau "žmogiška" paieška (linijų tąsa + mini heatmap).
            return _humanLike;
        }
    }
}
